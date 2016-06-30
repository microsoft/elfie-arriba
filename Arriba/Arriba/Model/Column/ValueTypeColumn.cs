// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Arriba.Extensions;
using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  ValueTypeColumn is a basic array of value types, useful for storing values which are
    ///  primarily unique. Column consumes the raw size per value (four bytes per int)
    ///  only.
    /// </summary>
    /// <typeparam name="T">Type of underlying values indexed</typeparam>
    public class ValueTypeColumn<T> : IColumn<T> where T : struct, IComparable<T>
    {
        private ushort _itemCount;
        private T _default;
        private T[] _values;

        public ValueTypeColumn(T defaultValue) : this(defaultValue, 0)
        {
        }

        public ValueTypeColumn(T defaultValue, ushort initialCapacity)
        {
            _itemCount = 0;
            _default = defaultValue;
            _values = ArrayExtensions.CreateRecommendedArray<T>(initialCapacity);
        }

        #region IColumn<T>
        public T DefaultValue
        {
            get { return _default; }
        }

        public T this[ushort lid]
        {
            get { return _values[lid]; }
            set
            {
                _values[lid] = value;
                if (lid >= _itemCount) _itemCount = (ushort)(lid + 1);
            }
        }

        public string Name { get; set; }

        public ushort Count
        {
            get { return _itemCount; }
        }

        public Array GetValues(IList<ushort> lids)
        {
            if (lids == null)
            {
                throw new ArgumentNullException("lids");
            }

            int count = lids.Count;

            T[] result = new T[count];
            for (int i = 0; i < count; ++i)
            {
                result[i] = this[lids[i]];
            }

            return result;
        }

        public void SetSize(ushort size)
        {
            if (size >= _itemCount)
            {
                // Ensure the column has enough space to store the new items, use Grow to avoid shrinking the column if it was preallocated with a higher capacity
                ArrayExtensions.Grow(ref _values, size, ushort.MaxValue);

                // If column grew, set new items to default
                for (int i = _itemCount; i < size; ++i)
                {
                    _values[i] = _default;
                }
            }
            else
            {
                ArrayExtensions.Resize(ref _values, size, ushort.MaxValue);

                // If column shrunk, remove deleted values
                for (int i = size; i < Math.Min(_itemCount, _values.Length); ++i)
                {
                    _values[i] = _default;
                }
            }

            // Track the new number of items set
            _itemCount = size;
        }

        public bool TryEvaluate(ushort lid, Operator op, T value, out bool result)
        {
            T itemValue = this[lid];
            return itemValue.TryEvaluate(op, value, out result);
        }

        public void TryWhere(Operator op, T value, ShortSet result, ExecutionDetails details)
        {
            // Base Column can't identify matches for any operator in bulk efficiently.
            if (details != null)
            {
                details.AddError(ExecutionDetails.ColumnDoesNotSupportOperator, op, this.Name);
            }
        }

        public bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int SortedIndexesCount)
        {
            // Base column doesn't contain sorting information
            sortedIndexes = null;
            SortedIndexesCount = 0;
            return false;
        }

        public bool TryGetIndexOf(T value, out ushort index)
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
            if (_itemCount > _values.Length)
            {
                if (details != null)
                {
                    details.AddError(ExecutionDetails.ColumnDoesNotHaveEnoughValues, this.Name, _itemCount, _values.Length);
                }
            }
        }
        #endregion

        #region IBinarySerializable
        public void ReadBinary(ISerializationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            _itemCount = context.Reader.ReadUInt16();
            _default = BinaryBlockSerializer.Read<T>(context);
            _values = BinaryBlockSerializer.ReadArray<T>(context);
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            context.Writer.Write(_itemCount);
            BinaryBlockSerializer.Write(context, _default);
            BinaryBlockSerializer.WriteArray(context, _values, 0, _itemCount);
        }
        #endregion
    }
}
