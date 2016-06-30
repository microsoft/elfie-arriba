// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    /// IUntypedColumn is an opaque interface for an UntypedColumn which is the top level wrapper of any column
    /// </summary>
    public interface IUntypedColumn : IColumn<object>
    {
        /// <summary>
        /// Gets the type of the underlying column
        /// </summary>
        Type ColumnType { get; }
    }

    /// <summary>
    ///  UntypedColumn wraps an underlying typed column and exposes it as an
    ///  IUntypedColumn which the table understands. It handles type
    ///  conversions for values passed.
    /// </summary>
    /// <typeparam name="T">Type of underlying values indexed</typeparam>
    public class UntypedColumn<T> : IUntypedColumn, ICommittable where T : IEquatable<T>
    {
        private IColumn<T> _column;

        public UntypedColumn(IColumn<T> column)
        {
            _column = column;
        }

        private bool TryConvert(object o, out T result)
        {
            if (o is T)
            {
                result = (T)o;
                return true;
            }

            if (o is ValueTypeReference<T>)
            {
                result = (o as ValueTypeReference<T>).Value;
                return true;
            }

            if (o == null)
            {
                result = _column.DefaultValue;
                return true;
            }

            Value v = Value.Create(o);
            return v.TryConvert<T>(out result);
        }

        #region IColumn<object>
        public object DefaultValue
        {
            get { return _column.DefaultValue; }
        }

        public object this[ushort lid]
        {
            get { return _column[lid]; }
            set
            {
                // Convert the new value to the column type, if needed
                T t;
                if (!TryConvert(value, out t))
                {
                    t = _column.DefaultValue;
                }

                // Set the new value, if it has changed
                // DEBUG: Comment condition to check consistency when rewriting identical values.
                T currentValue = _column[lid];

                // NOTE: .Equals of generic type parameters will do a reference equals (and therefore cause boxing of value types) 
                // unless constrained with IEquatable<T> as is done above.  Don't remove that constraint.
                if (!currentValue.Equals(t))
                {
                    _column[lid] = t;
                }
            }
        }

        public Array GetValues(IList<ushort> lids)
        {
            return _column.GetValues(lids);
        }

        public string Name
        {
            get { return _column.Name; }
            set { _column.Name = value; }
        }

        public ushort Count
        {
            get { return _column.Count; }
        }

        public void SetSize(ushort size)
        {
            _column.SetSize(size);
        }

        public bool TryEvaluate(ushort lid, Operator op, object value, out bool result)
        {
            T t;
            if (!TryConvert(value, out t))
            {
                result = false;
                return false;
            }

            return _column.TryEvaluate(lid, op, t, out result);
        }

        public void TryWhere(Operator op, object value, ShortSet result, ExecutionDetails details)
        {
            T t;
            if (!TryConvert(value, out t))
            {
                if (details != null)
                {
                    details.AddError(ExecutionDetails.UnableToConvertType, value, this.Name, typeof(T).Name);
                }
            }
            else
            {
                _column.TryWhere(op, t, result, details);
            }
        }

        public bool TryGetSortedIndexes(out IList<ushort> sortedIndexes, out int sortedIndexesCount)
        {
            return _column.TryGetSortedIndexes(out sortedIndexes, out sortedIndexesCount);
        }

        public bool TryGetIndexOf(object value, out ushort index)
        {
            T t;
            if (!TryConvert(value, out t))
            {
                index = ushort.MaxValue;
                return false;
            }

            return _column.TryGetIndexOf(t, out index);
        }

        public IColumn InnerColumn
        {
            get { return _column; }
        }

        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            _column.VerifyConsistency(level, details);
        }
        #endregion

        #region IBinarySerializable
        public void ReadBinary(ISerializationContext context)
        {
            _column.ReadBinary(context);
        }

        public void WriteBinary(ISerializationContext context)
        {
            _column.WriteBinary(context);
        }
        #endregion

        public void Commit()
        {
            if (_column is ICommittable)
            {
                (_column as ICommittable).Commit();
            }
        }

        public Type ColumnType
        {
            get
            {
                return typeof(T);
            }
        }
    }
}
