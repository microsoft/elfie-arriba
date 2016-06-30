// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Structures;

namespace Arriba.Model.Aggregations
{
    /// <summary>
    ///  MinAggregator computes the MIN of a query for the column passed.
    /// </summary>
    public class MinAggregator : BaseAggregator
    {
        public override string ToString()
        {
            return "MIN";
        }

        public override object Merge(object context, object[] values)
        {
            if (values == null) throw new ArgumentNullException("values");

            IComparable result = null;

            for (int i = 0; i < values.Length; ++i)
            {
                IComparable value = (IComparable)values[i];

                if (value != null)
                {
                    if (result == null || value.CompareTo(result) < 0) result = value;
                }
            }

            return result;
        }

        #region Aggregate Methods by type
        protected override object AggregateLong(long[] values)
        {
            long result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateInt(int[] values)
        {
            int result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateShort(short[] values)
        {
            short result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateByte(byte[] values)
        {
            byte result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateULong(ulong[] values)
        {
            ulong result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateUInt(uint[] values)
        {
            uint result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateUShort(ushort[] values)
        {
            ushort result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateDouble(double[] values)
        {
            double result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateFloat(float[] values)
        {
            float result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateDateTime(DateTime[] values)
        {
            DateTime result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateTimeSpan(TimeSpan[] values)
        {
            TimeSpan result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i] < result) result = values[i];
            }

            return result;
        }

        protected override object AggregateGuid(Guid[] values)
        {
            Guid result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i].CompareTo(result) < 0) result = values[i];
            }

            return result;
        }

        protected override object AggregateByteBlock(ByteBlock[] values)
        {
            ByteBlock result = values[0];

            for (int i = 1; i < values.Length; ++i)
            {
                if (values[i].CompareTo(result) < 0) result = values[i];
            }

            return result;
        }
        #endregion

    }
}
