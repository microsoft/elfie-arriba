// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Structures;

namespace Arriba.Model.Aggregations
{
    public class BaseAggregator : IAggregator
    {
        public virtual object CreateContext()
        {
            return null;
        }

        public virtual bool RequireMerge
        {
            get { return false; }
        }

        /// <summary>
        ///  Get the DefaultValue to return if there aren't any items to aggregate [the ShortSet is empty]
        /// </summary>
        public virtual object DefaultValue
        {
            get { return null; }
        }

        public virtual object Aggregate(object context, ShortSet matches, IUntypedColumn[] columns)
        {
            if (columns == null || columns.Length < 1) throw new ArgumentException(StringExtensions.Format("At least one column must be passed to {0}.", this.GetType().Name));
            if (matches == null) throw new ArgumentNullException("matches");
            if (matches.IsEmpty()) return DefaultValue;

            // Enumerate set once and get values once, avoiding any per-item method calls
            ushort[] items = matches.Values;
            Array values = columns[0].GetValues(items);
            Type itemType = columns[0].ColumnType;

            // Cast to a specific type at the array level to avoid per item casting
            if (itemType.Equals(typeof(long)))
            {
                return AggregateLong((long[])values);
            }
            else if (itemType.Equals(typeof(int)))
            {
                return AggregateInt((int[])values);
            }
            else if (itemType.Equals(typeof(short)))
            {
                return AggregateShort((short[])values);
            }
            else if (itemType.Equals(typeof(byte)))
            {
                return AggregateByte((byte[])values);
            }
            else if (itemType.Equals(typeof(ulong)))
            {
                return AggregateULong((ulong[])values);
            }
            else if (itemType.Equals(typeof(uint)))
            {
                return AggregateUInt((uint[])values);
            }
            else if (itemType.Equals(typeof(ushort)))
            {
                return AggregateUShort((ushort[])values);
            }
            else if (itemType.Equals(typeof(double)))
            {
                return AggregateDouble((double[])values);
            }
            else if (itemType.Equals(typeof(float)))
            {
                return AggregateFloat((float[])values);
            }
            else if (itemType.Equals(typeof(DateTime)))
            {
                return AggregateDateTime((DateTime[])values);
            }
            else if (itemType.Equals(typeof(TimeSpan)))
            {
                return AggregateTimeSpan((TimeSpan[])values);
            }
            else if (itemType.Equals(typeof(Guid)))
            {
                return AggregateGuid((Guid[])values);
            }
            else if (itemType.Equals(typeof(ByteBlock)))
            {
                return AggregateByteBlock((ByteBlock[])values);
            }
            else
            {
                throw new NotImplementedException(StringExtensions.Format("{0} is unable to aggregate type {1}.", this.GetType().Name, values.GetValue(0).GetType().Name));
            }
        }

        public virtual object Merge(object context, object[] values)
        {
            throw new NotImplementedException();
        }

        #region Aggregate Virtual Methods by type
        protected virtual object AggregateLong(long[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateInt(int[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateShort(short[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateByte(byte[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateULong(ulong[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateUInt(uint[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateUShort(ushort[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateDouble(double[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateFloat(float[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateDateTime(DateTime[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateTimeSpan(TimeSpan[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateGuid(Guid[] values)
        {
            throw new NotImplementedException();
        }

        protected virtual object AggregateByteBlock(ByteBlock[] values)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
