// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Extensions;
using Arriba.Model;
using Arriba.Model.Expressions;
using Arriba.Serialization;

namespace Arriba.Structures
{
    /// <summary>
    ///  ByteBlockColumn contains a set of variable length values as blocks of bytes.
    ///  Small values are packed together to reduce object overhead.
    /// </summary>
    public class ByteBlockColumn : IColumn<ByteBlock>, IBinarySerializable
    {
        private const int StoreSeparatelyMinimumLengthBytes = 4096;
        private const double CompactWasteThreshold = 0.20;

        private ByteBlock _default;
        private ushort _itemCount;
        private BlockPosition[] _index;
        private ushort _batchCount;
        private BlockBatch[] _batches;
        private ushort _appendToBatchIndex;

        public ByteBlockColumn(ByteBlock defaultValue)
        {
            _default = defaultValue;
            _itemCount = 0;
            _index = new BlockPosition[ArrayExtensions.MinimumSize];
            _batches = new BlockBatch[1];
            _appendToBatchIndex = 0;
            AddBatch(ArrayExtensions.MinimumSize);
        }

        #region IColumn
        public ByteBlock DefaultValue
        {
            get { return _default; }
        }

        public string Name { get; set; }

        public ushort Count
        {
            get { return _itemCount; }
        }

        public Array GetValues(IList<ushort> lids)
        {
            if (lids == null) throw new ArgumentNullException("lids");

            int count = lids.Count;

            ByteBlock[] result = new ByteBlock[count];
            for (int i = 0; i < count; ++i)
            {
                result[i] = this[lids[i]];
            }

            return result;
        }

        public void SetSize(ushort size)
        {
            if (size < _itemCount)
            {
                HashSet<ushort> batchesToShrink = new HashSet<ushort>();

                // If we're shrinking, find and remove values of removed items
                for (int i = _itemCount - 1; i >= size; --i)
                {
                    BlockPosition p = _index[i];
                    batchesToShrink.Add(p.BatchIndex);
                    RemoveValue(p);
                }

                // Remove index entries
                Array.Clear(_index, size, _itemCount - size);

                // Shrink the index array (if enough savings)
                ArrayExtensions.Resize(ref _index, size, ushort.MaxValue);

                // Reset size to exclude removed items (*before* compression, so it doesn't re-create them)
                _itemCount = size;

                // Consider compression of batches with removed values
                foreach (ushort batchIndex in batchesToShrink)
                {
                    CompactIfNeeded(batchIndex, 0);
                }
            }
            else
            {
                // If column grew, new values are automatically a zero-length value.
                ArrayExtensions.Grow(ref _index, size, ushort.MaxValue);

                ushort oldCount = _itemCount;
                _itemCount = size;

                // If column grew, set new items to default
                for (ushort i = oldCount; i < size; ++i)
                {
                    this[i] = _default;
                }
            }
        }

        public bool TryEvaluate(ushort lid, Operator op, ByteBlock value, out bool result)
        {
            ByteBlock itemValue = this[lid];
            return itemValue.TryEvaluate(op, value, out result);
        }

        public void TryWhere(Operator op, ByteBlock value, ShortSet result, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");

            // Base Column can't identify matches for any operator in bulk efficiently.
            details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
        }

        public bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            // ByteBlock column doesn't contain sorting information
            sortedIndexes = null;
            sortedIndexesCount = 0;
            return false;
        }

        public bool TryGetIndexOf(ByteBlock value, out ushort index)
        {
            // Base column doesn't contain sorting information
            index = ushort.MaxValue;
            return false;
        }

        public IColumn InnerColumn
        {
            get { return null; }
        }

        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");

            // Verify there are enough index items
            if (_itemCount > _index.Length)
            {
                details.AddError(ExecutionDetails.ColumnDoesNotHaveEnoughValues, this.Name, _itemCount, _index.Length);
            }

            for (int i = 0; i < _itemCount; ++i)
            {
                BlockPosition p = _index[i];
                if (p.BatchIndex >= _batchCount)
                {
                    // Verify every index item points to a valid batch
                    details.AddError(ExecutionDetails.ByteBlockColumnBatchOutOfRange, this.Name, i, p.BatchIndex, _batchCount);
                }
                else
                {
                    // Verify every empty item is represented correctly and every non-oversize item points to a valid array range
                    BlockBatch batch = _batches[p.BatchIndex];
                    if (p.Length == 0)
                    {
                        if (p.Position != 0) details.AddError(ExecutionDetails.ByteBlockEmptyValueMisrecorded, this.Name, i, p.Position);
                    }
                    else if (p.Length == ushort.MaxValue)
                    {
                        if (p.Position != 0) details.AddError(ExecutionDetails.ByteBlockHugeValueMisrecorded, this.Name, i, p.Position);
                    }
                    else
                    {
                        if (p.Position >= batch.Array.Length || p.Position + p.Length > batch.Array.Length)
                        {
                            details.AddError(ExecutionDetails.ByteBlockColumnPositionOutOfRange, this.Name, i, p.Position, p.Length, batch.Array.Length);
                        }
                    }
                }
            }

            // Verify all out-of-range items are clear
            for (int i = _itemCount; i < _index.Length; ++i)
            {
                BlockPosition p = _index[i];
                if (p.BatchIndex != 0 || p.Position != 0 || p.Length != 0)
                {
                    details.AddError(ExecutionDetails.ByteBlockColumnUnclearedIndexEntry, this.Name, i, p);
                }
            }
        }
        #endregion

        #region Value Placement
        public ByteBlock this[ushort lid]
        {
            get
            {
                BlockPosition indexEntry = _index[lid];
                ByteBlock block = new ByteBlock(_batches[indexEntry.BatchIndex].Array, indexEntry.Position, indexEntry.Length);

                // If value is huge, get actual length
                if (block.Length == ushort.MaxValue && block.Index == 0) block.Length = block.Array.Length;

                return block;
            }
            set
            {
                BlockPosition p = new BlockPosition();
                int oldBatchIndex = -1;

                if (lid < _itemCount)
                {
                    // Find the current position and index
                    p = _index[lid];
                    oldBatchIndex = p.BatchIndex;

                    // If we're trying to set the same value, stop (don't clear it and wipe it out)
                    if (value.Array == _batches[p.BatchIndex].Array && value.Index == p.Position && value.Length == p.Length) return;

                    // Remove the current value (clear space and count as waste)
                    RemoveValue(p);

                    // Clear index entry
                    _index[lid].BatchIndex = 0;
                    _index[lid].Position = 0;
                    _index[lid].Length = 0;
                }
                else
                {
                    _itemCount = (ushort)(lid + 1);
                }

                // If the value is non-empty, write a new value
                if (value.Length > 0)
                {
                    // Can we write the new value in place?
                    if (!TryWriteInPlace(ref p, value))
                    {
                        // Can we write it alone?
                        if (!TryWriteAlone(ref p, value))
                        {
                            // Can we append to the current append batch?
                            if (!TryWriteAppend(ref p, value))
                            {
                                // Otherwise, write to a new array
                                AddBatch(Math.Max(value.Length, ArrayExtensions.MinimumSize));
                                _appendToBatchIndex = (ushort)(_batchCount - 1);
                                TryWriteAppend(ref p, value);
                            }
                        }
                    }

                    // Update the index position
                    _index[lid] = p;
                }

                // Consider compacting the old position
                if (oldBatchIndex != -1) CompactIfNeeded(oldBatchIndex, 0);
            }
        }

        private void RemoveValue(BlockPosition p)
        {
            // If value stored separately, don't clear - we replace it and GC will collect the old one.
            if (p.Length == ushort.MaxValue) return;

            // Clear the previous space (security)
            Array.Clear(_batches[p.BatchIndex].Array, p.Position, p.Length);

            // Add waste for the previous value
            _batches[p.BatchIndex].WasteSpace += p.Length;
        }

        private void AddBatch(int size)
        {
            ArrayExtensions.Resize(ref _batches, _batchCount + 1, ushort.MaxValue);
            _batches[_batchCount].Array = new byte[size];
            _batchCount++;
        }

        private bool CompactIfNeeded(int batchIndex, int additionalSizeNeeded)
        {
            BlockBatch batch = _batches[batchIndex];

            // If there's not enough waste, nothing to clean up
            double wastePercentage = ((double)batch.WasteSpace / batch.Array.Length);
            if (wastePercentage < CompactWasteThreshold) return false;

            // Otherwise, create a new array
            int neededSize = batch.UsedSpace - batch.WasteSpace + additionalSizeNeeded;
            int newSize = ArrayExtensions.RecommendedSize(batch.Array.Length, neededSize, ushort.MaxValue);
            byte[] oldArray = batch.Array;
            byte[] newArray = new byte[newSize];

            // Next, copy every non-empty item in this batch to the new array contiguously
            ushort nextWritePosition = 0;
            for (int i = 0; i < _itemCount; ++i)
            {
                BlockPosition p = _index[i];
                if (p.BatchIndex != batchIndex) continue;
                if (p.Length == 0) continue;

                Array.Copy(oldArray, p.Position, newArray, nextWritePosition, p.Length);
                p.Position = nextWritePosition;

                nextWritePosition += p.Length;
                _index[i] = p;
            }

            // Update and rewrite the batch
            batch.Array = newArray;
            batch.UsedSpace = nextWritePosition;
            batch.WasteSpace = 0;
            _batches[batchIndex] = batch;

            return true;
        }

        private bool TryWriteInPlace(ref BlockPosition position, ByteBlock value)
        {
            BlockBatch containingBatch = _batches[position.BatchIndex];

            // If this item is new, there is no place to write it
            if (position.Length == 0) return false;

            // If the old value was written alone, replace it with a new exact size array
            if (position.Length == ushort.MaxValue)
            {
                containingBatch.Array = new byte[value.Length];
                containingBatch.UsedSpace = (ushort)value.Length;
                value.CopyTo(containingBatch.Array);
            }
            else
            {
                // If the new value is too big, we can't write it in place
                if (position.Length < value.Length) return false;

                // Reduce the waste for reusing the space (we added it all as waste before placement)
                containingBatch.WasteSpace -= (ushort)value.Length;

                // Write the array to the existing position and record the new length
                value.CopyTo(_batches[position.BatchIndex].Array, position.Position);
                position.Length = (ushort)value.Length;
            }

            // Update the batch
            _batches[position.BatchIndex] = containingBatch;

            return true;
        }

        private bool TryWriteAppend(ref BlockPosition position, ByteBlock value)
        {
            BlockBatch appendBatch = _batches[_appendToBatchIndex];

            // If the new value is too big, can we expand?
            if (appendBatch.Array.Length - appendBatch.UsedSpace < value.Length)
            {
                // Compact, if reasonable, or expand if not
                if (CompactIfNeeded(_appendToBatchIndex, value.Length))
                {
                    appendBatch = _batches[_appendToBatchIndex];
                }
                else
                {
                    ArrayExtensions.Resize(ref appendBatch.Array, appendBatch.UsedSpace + value.Length, ushort.MaxValue);
                    _batches[_appendToBatchIndex] = appendBatch;
                }
            }

            // If the value is still too big, stop
            if (appendBatch.Array.Length - appendBatch.UsedSpace < value.Length) return false;

            // Build the new position
            BlockPosition newPosition;
            newPosition.BatchIndex = _appendToBatchIndex;
            newPosition.Position = (ushort)appendBatch.UsedSpace;
            newPosition.Length = (ushort)value.Length;

            // Track the new used space
            appendBatch.UsedSpace += (ushort)value.Length;
            _batches[_appendToBatchIndex] = appendBatch;

            // Write the value to the new position
            value.CopyTo(appendBatch.Array, newPosition.Position);

            // Update the position and return
            position = newPosition;
            return true;
        }

        private bool TryWriteAlone(ref BlockPosition position, ByteBlock value)
        {
            // If this value is too small to write alone, stop
            if (value.Length < StoreSeparatelyMinimumLengthBytes) return false;

            // Build the new position
            BlockPosition newPosition;
            newPosition.BatchIndex = _batchCount;
            newPosition.Position = 0;
            newPosition.Length = ushort.MaxValue;

            // Write the value to the new array
            byte[] newBatchArray = new byte[value.Length];
            value.CopyTo(newBatchArray);

            // Create a new Batch for this item
            ArrayExtensions.Resize(ref _batches, _batchCount + 1, ushort.MaxValue);
            _batches[_batchCount].Array = newBatchArray;
            _batchCount++;

            // Update the position and return
            position = newPosition;
            return true;
        }
        #endregion

        #region Internal Structures
        private struct BlockPosition : IBinarySerializable
        {
            public ushort BatchIndex;
            public ushort Position;
            public ushort Length;

            public void ReadBinary(ISerializationContext context)
            {
                if (context == null) throw new ArgumentNullException("context");

                this.BatchIndex = context.Reader.ReadUInt16();
                this.Position = context.Reader.ReadUInt16();
                this.Length = context.Reader.ReadUInt16();
            }

            public void WriteBinary(ISerializationContext context)
            {
                if (context == null) throw new ArgumentNullException("context");

                context.Writer.Write(this.BatchIndex);
                context.Writer.Write(this.Position);
                context.Writer.Write(this.Length);
            }

            public override string ToString()
            {
                return StringExtensions.Format("({0}, {1}, {2})", this.BatchIndex, this.Position, this.Length);
            }
        }

        private struct BlockBatch : IBinarySerializable
        {
            public byte[] Array;
            public ushort UsedSpace;
            public ushort WasteSpace;

            public void ReadBinary(ISerializationContext context)
            {
                this.Array = BinaryBlockSerializer.ReadArray<byte>(context);
                this.UsedSpace = context.Reader.ReadUInt16();
                this.WasteSpace = context.Reader.ReadUInt16();
            }

            public void WriteBinary(ISerializationContext context)
            {
                BinaryBlockSerializer.WriteArray(context, this.Array);
                context.Writer.Write(this.UsedSpace);
                context.Writer.Write(this.WasteSpace);
            }
        }
        #endregion

        #region Debuggability
        public string[] ConvertToArray()
        {
            string[] values = new string[_itemCount];

            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = this[(ushort)i].ToString();
            }

            return values;
        }
        #endregion

        #region IBinarySerializable
        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            // NOTE: DefaultValue is not preserved here [compat], but Partition serialization re-creates it from ColumnDetails.

            _itemCount = context.Reader.ReadUInt16();
            _batchCount = context.Reader.ReadUInt16();
            _appendToBatchIndex = context.Reader.ReadUInt16();

            _index = BinaryBlockSerializer.ReadSerializableArray<BlockPosition>(context);
            _batches = BinaryBlockSerializer.ReadSerializableArray<BlockBatch>(context);
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            // NOTE: DefaultValue is not preserved here [compat], but Partition serialization re-creates it from ColumnDetails.

            context.Writer.Write(_itemCount);
            context.Writer.Write(_batchCount);
            context.Writer.Write(_appendToBatchIndex);

            BinaryBlockSerializer.WriteSerializableArray(context, _index, 0, _itemCount);
            BinaryBlockSerializer.WriteSerializableArray(context, _batches, 0, _batchCount);
        }
        #endregion
    }
}
