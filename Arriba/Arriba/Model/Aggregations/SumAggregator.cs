// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Structures;

namespace Arriba.Model.Aggregations
{
    /// <summary>
    ///  SumAggregator computes the SUM of a query for the column passed.
    /// </summary>
    public class SumAggregator : BaseAggregator
    {
        public override string ToString()
        {
            return "SUM";
        }

        public override object DefaultValue
        {
            get
            {
                return null;
            }
        }

        public override object Merge(object context, object[] values)
        {
            if (values == null) throw new ArgumentNullException("values");

            int i = 0;

            // Skip until the first non-null value
            for (; i < values.Length; ++i)
            {
                if (values[i] != null) break;
            }

            // If no non-null values, return null
            if (i == values.Length) return null;

            // Otherwise, sum up by type
            if (values[i] is long)
            {
                long result = 0;

                for (; i < values.Length; ++i)
                {
                    if (values[i] != null)
                    {
                        result += (long)values[i];
                    }
                }

                return result;
            }
            else if (values[i] is double)
            {
                double result = 0;

                for (; i < values.Length; ++i)
                {
                    if (values[i] != null)
                    {
                        result += (double)values[i];
                    }
                }

                return result;
            }
            else if (values[i] is ulong)
            {
                ulong result = 0;

                for (; i < values.Length; ++i)
                {
                    if (values[i] != null)
                    {
                        result += (ulong)values[i];
                    }
                }

                return result;
            }
            else if (values[i] is TimeSpan)
            {
                TimeSpan result = TimeSpan.Zero;

                for (; i < values.Length; ++i)
                {
                    if (values[i] != null)
                    {
                        result += (TimeSpan)values[i];
                    }
                }

                return result;
            }
            else
            {
                throw new ArgumentException(StringExtensions.Format("SumAggregator is unable to merge types other than long and double. A {0} was passed.", values[i].GetType().Name));
            }
        }

        #region Aggregate Methods by type
        protected override object AggregateLong(long[] values)
        {
            long result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += values[i];
            }

            return result;
        }

        protected override object AggregateInt(int[] values)
        {
            long result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += (long)values[i];
            }

            return result;
        }

        protected override object AggregateShort(short[] values)
        {
            long result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += (long)values[i];
            }

            return result;
        }

        protected override object AggregateByte(byte[] values)
        {
            long result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += (long)values[i];
            }

            return result;
        }

        protected override object AggregateULong(ulong[] values)
        {
            ulong result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += values[i];
            }

            return result;
        }

        protected override object AggregateUInt(uint[] values)
        {
            long result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += (long)values[i];
            }

            return result;
        }

        protected override object AggregateUShort(ushort[] values)
        {
            long result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += (long)values[i];
            }

            return result;
        }

        protected override object AggregateDouble(double[] values)
        {
            double result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += values[i];
            }

            return result;
        }

        protected override object AggregateFloat(float[] values)
        {
            double result = 0;

            for (int i = 0; i < values.Length; ++i)
            {
                result += values[i];
            }

            return result;
        }


        protected override object AggregateTimeSpan(TimeSpan[] values)
        {
            TimeSpan result = TimeSpan.Zero;

            for (int i = 0; i < values.Length; ++i)
            {
                result += values[i];
            }

            return result;
        }
        #endregion
    }
}
