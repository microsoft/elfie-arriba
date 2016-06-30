// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Arriba.Extensions;
using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  SortedColumn wraps an underlying column and tracks the sort order of 
    ///  the underlying IDs, enabling fast Where clauses for comparison 
    ///  operators and IndexOf. SortedColumn adds two bytes per value to keep
    ///  the order.
    /// </summary>
    /// <typeparam name="T">Type of underlying values indexed</typeparam>
    public class SortedColumn<T> : BaseColumnWrapper<T>, IColumn<T> where T : IComparable<T>
    {
        protected ushort[] SortedIDs;
        protected ushort SortedIDCount;

        public SortedColumn(IColumn<T> column) : this(column, 0)
        {
        }

        public SortedColumn(IColumn<T> column, ushort initialCapacity) : base(column)
        {
            this.SortedIDs = ArrayExtensions.CreateRecommendedArray<ushort>(initialCapacity);
            this.SortedIDCount = 0;
        }

        #region IColumn<T>
        public override T this[ushort lid]
        {
            get { return this.Column[lid]; }
            set
            {
                // Re-sort SortedIDs to include the new value
                UpdateSortedIDs(value, lid);

                // Set the new value
                this.Column[lid] = value;
            }
        }

        protected void UpdateSortedIDs(T value, ushort lid)
        {
            // Find the index where the current value must be removed
            int currentSortedIndex = FindSortedIndexWithID(this.Column[lid], lid);
            Debug.Assert(currentSortedIndex >= 0, "SortedIDs couldn't update because value which must be set wasn't found.");

            // Find the index where the new value should be inserted
            int newValueIndex = FindInsertionPosition(value, lid);

            // Shift SortedIDs to maintain sorting
            if (newValueIndex > currentSortedIndex)
            {
                // The insertion position will be one left of the suggested one, since we'll shift everything else to the left
                newValueIndex--;

                // Existing value moving later - Shift items left between the old and new position
                Array.Copy(this.SortedIDs, currentSortedIndex + 1, this.SortedIDs, currentSortedIndex, newValueIndex - currentSortedIndex);
            }
            else if (newValueIndex < currentSortedIndex)
            {
                // Existing value moving earlier - Shift items right between the old and new position
                Array.Copy(this.SortedIDs, newValueIndex, this.SortedIDs, newValueIndex + 1, currentSortedIndex - newValueIndex);
            }

            // Insert the new SortedID
            this.SortedIDs[newValueIndex] = lid;
        }

        public override void SetSize(ushort size)
        {
            ushort currentSize = this.Column.Count;

            if (size > currentSize)
            {
                // Ensure the column has enough space to store the new items, use Grow to avoid shrinking the column if it was preallocated with a higher capacity
                ArrayExtensions.Grow(ref this.SortedIDs, size, ushort.MaxValue);

                int countToInsert = size - currentSize;

                // Find the index where IDs with the new value should be inserted
                int insertionIndex = FindInsertionPosition(this.DefaultValue, (ushort)(size - 1));

                // If this isn't at the end, shift items to make room for the new sorted IDs
                if (insertionIndex < currentSize)
                {
                    Array.Copy(this.SortedIDs, insertionIndex, this.SortedIDs, insertionIndex + countToInsert, currentSize - insertionIndex);
                }

                // Insert SortedIDs for new items at the correct place
                for (int i = 0; i < countToInsert; ++i)
                {
                    this.SortedIDs[insertionIndex + i] = (ushort)(currentSize + i);
                }

                // Set the number of items now in the SortedID set - after insertions so searches have correct bounds
                this.SortedIDCount = size;
            }
            else if (size < currentSize)
            {
                // For each removed item...
                for (int i = currentSize - 1; i >= size; --i)
                {
                    // Find the index where the ID to remove currently is
                    int insertionIndex = FindInsertionPosition(this[(ushort)i], (ushort)i);
                    Debug.Assert(this.SortedIDs[insertionIndex] == i, StringExtensions.Format("Unable to remove LID {0:n0}; search for value reported it at index {1:n0}, which has LID {2:n0} instead.", i, insertionIndex, this.SortedIDs[insertionIndex]));

                    // If this isn't at the end, shift items left to remove this ID
                    if (insertionIndex + 1 < currentSize)
                    {
                        Array.Copy(this.SortedIDs, insertionIndex + 1, this.SortedIDs, insertionIndex, currentSize - (insertionIndex + 1));
                    }

                    // Clear the last value
                    this.SortedIDs[i] = 0;

                    // Decrement size, count so the next searches have correct bounds
                    currentSize--;
                    this.SortedIDCount--;
                }

                // If shrinking, shrink after moves
                ArrayExtensions.Resize(ref this.SortedIDs, size, ushort.MaxValue);
            }

            // Resize the underlying column - don't do until after SortedIDs fixed so we know where to remove from when shrinking.
            this.Column.SetSize(size);
        }

        public override void TryWhere(Operator op, T value, ShortSet result, ExecutionDetails details)
        {
            RangeToScan range = new RangeToScan();
            bool rangeOk = true;

            // For StartsWith, for ByteBlocks only, implement using IsPrefixOf
            if (op == Operator.StartsWith)
            {
                if (value is ByteBlock)
                {
                    IComparable<T> prefixComparer = (IComparable<T>)((ByteBlock)(object)value).GetExtendedIComparable(ByteBlock.Comparison.IsPrefixOf);     // trust me C#... I'm a professional...

                    int first = FindFirstWhere(prefixComparer);
                    int last = FindLastWhere(prefixComparer);
                    if (!RangeToScan.TryBuild(Operator.Equals, first, last, this.Column.Count, ref range))
                    {
                        rangeOk = false;
                        if (details != null)
                        {
                            details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
                        }
                    }
                }
                else
                {
                    rangeOk = false;
                    if (details != null)
                    {
                        details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
                    }
                }
            }
            else
            {
                int first = FindFirstWhere(value);
                int last = FindLastWhere(value);
                // Determine the range to scan to compute the result
                if (!RangeToScan.TryBuild(op, first, last, this.Column.Count, ref range))
                {
                    rangeOk = false;
                    if (details != null)
                    {
                        details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
                    }
                }
            }

            // Build the result set and return it
            if (rangeOk == true) range.AddMatches(this.SortedIDs, result);
        }

        public override bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            sortedIndexes = this.SortedIDs;
            sortedIndexesCount = this.SortedIDCount;
            return true;
        }

        public override bool TryGetIndexOf(T value, out ushort index)
        {
            int i = FindFirstWhere(value);

            if (i < 0)
                index = ushort.MaxValue;
            else
                index = this.SortedIDs[i];

            return true;
        }

        public override void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            base.VerifyConsistency(level, details);

            // Verify SortedIDCount agrees with ItemCount
            if (this.SortedIDCount != this.Count)
            {
                if (details != null)
                {
                    details.AddError(ExecutionDetails.ColumnDoesNotHaveEnoughValues, this.Name, this.SortedIDCount, this.Count);
                }
            }

            // Verify that all IDs are in SortedIDs, all values are ordered, and no unexpected values are found
            ushort lastID = 0;
            IComparable lastValue = null;

            ShortSet idsInList = new ShortSet(this.Count);
            for (int i = 0; i < this.Count; ++i)
            {
                ushort id = this.SortedIDs[i];

                if (id >= this.Count)
                {
                    if (details != null)
                    {
                        details.AddError(ExecutionDetails.SortedIdOutOfRange, this.Name, id, this.Count);
                    }
                }
                else if (idsInList.Contains(id))
                {
                    if (details != null)
                    {
                        details.AddError(ExecutionDetails.SortedIdAppearsMoreThanOnce, this.Name, id);
                    }
                }
                else
                {
                    idsInList.Add(id);

                    IComparable value = (IComparable)this[id];
                    if (lastValue != null)
                    {
                        int compareResult = lastValue.CompareTo(value);
                        if (compareResult > 0)
                        {
                            if (details != null)
                            {
                                details.AddError(ExecutionDetails.SortedValuesNotInOrder, this.Name, lastID, lastValue, id, value);
                            }
                        }
                    }

                    lastValue = value;
                    lastID = id;
                }
            }

            idsInList.Not();
            if (idsInList.Count() > 0)
            {
                if (details != null)
                {
                    details.AddError(ExecutionDetails.SortedColumnMissingIDs, this.Name, String.Join(", ", idsInList.Values));
                }
            }
        }
        #endregion

        #region Debuggability
        public T[] ConvertToSortedArray()
        {
            return (T[])this.InnerColumn.GetValues(this.SortedIDs);
        }
        #endregion

        #region IBinarySerializable
        public override void ReadBinary(ISerializationContext context)
        {
            this.Column.ReadBinary(context);
            this.SortedIDs = BinaryBlockSerializer.ReadArray<ushort>(context);
            this.SortedIDCount = this.Column.Count;
        }

        public override void WriteBinary(ISerializationContext context)
        {
            this.Column.WriteBinary(context);
            BinaryBlockSerializer.WriteArray(context, this.SortedIDs, 0, this.Column.Count);
        }
        #endregion

        #region Sorted Index Binary Search

        /// <summary>
        ///  Binary Search through SortedIDs to find the first *index* in SortedIDs
        ///  pointing to an ID whose value matches the filter. This is used to
        ///  find the range of items with a given value quickly.
        /// </summary>
        /// <param name="item">item which implements appropriate comparison function like desiredValue.CompareTo. Returns negative if value too big, positive if too small, 0 if matching filter.</param>
        /// <returns>First index in SortedIDs referring to an item matching filter, one's complement (negative) of insertion point if not found</returns>
        public int FindFirstWhere<U>(U item) where U : IComparable<T>
        {
            int min = 0;
            int max = this.SortedIDCount - 1;
            int cmp;

            // If there are no items, we would insert at position 0
            if (max == -1) return -1;

            // Find the *first* matching value in our set
            while (min < max)
            {
                int mid = (min + max) / 2;
                T midValue = this.Column[this.SortedIDs[(ushort)mid]];

                cmp = item.CompareTo(midValue);
                if (cmp > 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid;
                }
            }

            // Check the value we settled on
            cmp = item.CompareTo(this.Column[this.SortedIDs[(ushort)min]]);
            if (cmp == 0)
            {
                // If the value matches, this is the first value
                return min;
            }
            else if (cmp > 0)
            {
                // If it is still too small, we would insert after it
                return ~(min + 1);
            }
            else
            {
                // If it is too big, we would insert at it
                return ~min;
            }
        }

        /// <summary>
        ///  Binary Search through SortedIDs to find the last *index* in SortedIDs
        ///  pointing to an ID whose value matches the filter. This is used to
        ///  find the range of items with a given value quickly.
        /// </summary>
        /// <param name="item">item which implements appropriate comparison function like desiredValue.CompareTo. Returns negative if value too big, positive if too small, 0 if matching filter.</param>
        /// <returns>Last index in SortedIDs referring to an item matching filter, one's complement (negative) of insertion point if not found</returns>
        public int FindLastWhere<U>(U item) where U : IComparable<T>
        {
            int min = 0;
            int max = this.SortedIDCount - 1;
            int cmp;

            // If there are no items, we would insert at position 0
            if (max == -1) return -1;

            // Find the *last* matching value in our set
            while (min < max)
            {
                int mid = (min + max + 1) / 2;
                T midValue = this.Column[this.SortedIDs[(ushort)mid]];

                cmp = item.CompareTo(midValue);
                if (cmp < 0)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid;
                }
            }

            // Check the value we settled on
            cmp = item.CompareTo(this.Column[this.SortedIDs[(ushort)min]]);
            if (cmp == 0)
            {
                // If the value matches, this is the last value
                return min;
            }
            else if (cmp > 0)
            {
                // If it is still too small, we would insert after it
                return ~(min + 1);
            }
            else
            {
                // If it is too big, we would insert at it
                return ~min;
            }
        }

        /// <summary>
        ///  Find where 'value' should be inserted within SortedIDs;
        ///  they must remain sorted by value and then by LID for
        ///  searches for values and LIDs to work properly.
        /// </summary>
        /// <param name="value">Value to find position for</param>
        /// <param name="lid">LID of the item to insert</param>
        /// <returns>Index where value should be inserted</returns>
        protected int FindInsertionPosition(T value, ushort lid)
        {
            int newValueIndex = FindSortedIndexWithID(value, lid);

            if (newValueIndex < 0)
            {
                // If 'value' wasn't found, the returned value is the complement of the insertion position
                newValueIndex = ~newValueIndex;
            }

            return newValueIndex;
        }

        /// <summary>
        ///  Binary Search through SortedIDs to find the index where the item with the given LID
        ///  and value is. The value is needed to tell which way to search.
        /// </summary>
        /// <param name="value">Value to search for</param>
        /// <param name="lid">The LID to search for</param>
        /// <returns>Last index in SortedIDs containing that LID, which must point to the given value, or the complement of where it should be inserted</returns>
        private int FindSortedIndexWithID(T value, ushort lid)
        {
            int min = 0;
            int max = this.SortedIDCount - 1;
            int mid = 0;
            int cmp = 0;

            while (min <= max)
            {
                mid = (min + max) / 2;
                ushort midId = this.SortedIDs[(ushort)mid];
                T midValue = this.Column[midId];

                cmp = midValue.CompareTo(value);
                if (cmp == 0)
                {
                    cmp = midId.CompareTo(lid);
                }

                if (cmp == 0)
                {
                    return mid;
                }
                else if (cmp > 0)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            // Check the last value we examined
            if (cmp < 0)
            {
                // If it is still too small, we would insert after it
                return ~(mid + 1);
            }
            else
            {
                // If it is too big, we would insert at it
                return ~mid;
            }
        }
        #endregion
    }
}
