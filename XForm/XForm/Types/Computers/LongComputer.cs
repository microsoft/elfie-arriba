using System;
using XForm.Data;

namespace XForm.Types.Computers
{
    public class LongComputer : IXArrayComputer
    {
        private long[] _buffer;
        private bool[] _isNull;

        public XArray Subtract(XArray left, XArray right)
        {
            long[] leftArray = (long[])left.Array;
            long[] rightArray = (long[])right.Array;

            int count = left.Count;
            if(right.Count != count) throw new InvalidOperationException("Computations must get the same number of rows from each argument.");

            bool areAnyNull = false;

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, count);
            Allocator.AllocateToSize(ref _isNull, count);

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.HasNulls || right.HasNulls)
            {
                for (int i = 0; i < count; ++i)
                {
                    int index1 = left.Index(i);
                    int index2 = right.Index(i);

                    bool rowIsNull = (left.HasNulls && left.NullRows[index1]) || (right.HasNulls && right.NullRows[index2]);
                    areAnyNull |= rowIsNull;

                    _isNull[i] = rowIsNull;
                    _buffer[i] = leftArray[index1] - rightArray[index2];
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                for (int i = 0; i < left.Count; ++i)
                {
                    _buffer[i] = leftArray[left.Index(i)] - rightArray[right.Index(i)];
                }
            }
            else if (!right.Selector.IsSingleValue && !left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                int rightStart = right.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + leftStart] - rightArray[i + rightStart];
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                long rightValue = rightArray[right.Selector.StartIndexInclusive];

                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + leftStart] - rightValue;
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                long leftValue = leftArray[left.Selector.StartIndexInclusive];
                int rightStart = right.Selector.StartIndexInclusive;

                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftValue - rightArray[i + rightStart];
                }
            }
            else
            {
                _buffer[0] = leftArray[left.Selector.StartIndexInclusive] - rightArray[right.Selector.StartIndexInclusive];
                return XArray.Single(_buffer, count);
            }

            return XArray.All(_buffer, count, (areAnyNull ? _isNull : null));
        }

        public XArray Add(XArray left, XArray right)
        {
            long[] leftArray = (long[])left.Array;
            long[] rightArray = (long[])right.Array;

            int count = left.Count;
            if (right.Count != count) throw new InvalidOperationException("Computations must get the same number of rows from each argument.");

            bool areAnyNull = false;

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, count);
            Allocator.AllocateToSize(ref _isNull, count);

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.HasNulls || right.HasNulls)
            {
                for (int i = 0; i < count; ++i)
                {
                    int index1 = left.Index(i);
                    int index2 = right.Index(i);

                    bool rowIsNull = (left.HasNulls && left.NullRows[index1]) || (right.HasNulls && right.NullRows[index2]);
                    areAnyNull |= rowIsNull;

                    _isNull[i] = rowIsNull;
                    _buffer[i] = leftArray[index1] + rightArray[index2];
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                for (int i = 0; i < left.Count; ++i)
                {
                    _buffer[i] = leftArray[left.Index(i)] + rightArray[right.Index(i)];
                }
            }
            else if (!right.Selector.IsSingleValue && !left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                int rightStart = right.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + leftStart] + rightArray[i + rightStart];
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                long rightValue = rightArray[right.Selector.StartIndexInclusive];

                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + leftStart] + rightValue;
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                long leftValue = leftArray[left.Selector.StartIndexInclusive];
                int rightStart = right.Selector.StartIndexInclusive;

                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftValue + rightArray[i + rightStart];
                }
            }
            else
            {
                _buffer[0] = leftArray[left.Selector.StartIndexInclusive] + rightArray[right.Selector.StartIndexInclusive];
                return XArray.Single(_buffer, count);
            }

            return XArray.All(_buffer, count, (areAnyNull ? _isNull : null));
        }

        public XArray Multiply(XArray left, XArray right)
        {
            long[] leftArray = (long[])left.Array;
            long[] rightArray = (long[])right.Array;

            int count = left.Count;
            if (right.Count != count) throw new InvalidOperationException("Computations must get the same number of rows from each argument.");

            bool areAnyNull = false;

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, count);
            Allocator.AllocateToSize(ref _isNull, count);

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.HasNulls || right.HasNulls)
            {
                for (int i = 0; i < count; ++i)
                {
                    int index1 = left.Index(i);
                    int index2 = right.Index(i);

                    bool rowIsNull = (left.HasNulls && left.NullRows[index1]) || (right.HasNulls && right.NullRows[index2]);
                    areAnyNull |= rowIsNull;

                    _isNull[i] = rowIsNull;
                    _buffer[i] = leftArray[index1] * rightArray[index2];
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                for (int i = 0; i < left.Count; ++i)
                {
                    _buffer[i] = leftArray[left.Index(i)] * rightArray[right.Index(i)];
                }
            }
            else if (!right.Selector.IsSingleValue && !left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                int rightStart = right.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + leftStart] * rightArray[i + rightStart];
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                long rightValue = rightArray[right.Selector.StartIndexInclusive];

                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + leftStart] * rightValue;
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                long leftValue = leftArray[left.Selector.StartIndexInclusive];
                int rightStart = right.Selector.StartIndexInclusive;

                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftValue * rightArray[i + rightStart];
                }
            }
            else
            {
                _buffer[0] = leftArray[left.Selector.StartIndexInclusive] * rightArray[right.Selector.StartIndexInclusive];
                return XArray.Single(_buffer, count);
            }

            return XArray.All(_buffer, count, (areAnyNull ? _isNull : null));
        }

        public XArray Divide(XArray left, XArray right)
        {
            long[] leftArray = (long[])left.Array;
            long[] rightArray = (long[])right.Array;

            int count = left.Count;
            if (right.Count != count) throw new InvalidOperationException("Computations must get the same number of rows from each argument.");

            bool areAnyNull = false;

            // Allocate for results
            Allocator.AllocateToSize(ref _buffer, count);
            Allocator.AllocateToSize(ref _isNull, count);

            // Check how the XArrays are configured and run the fastest loop possible for the configuration.
            if (left.HasNulls || right.HasNulls)
            {
                for (int i = 0; i < count; ++i)
                {
                    int index1 = left.Index(i);
                    int index2 = right.Index(i);

                    bool rowIsNull = (left.HasNulls && left.NullRows[index1]) || (right.HasNulls && right.NullRows[index2]);

                    DivideSafe(leftArray[index1], rightArray[index2], out _buffer[i], out _isNull[i], ref areAnyNull);
                    _isNull[i] |= rowIsNull;
                    areAnyNull |= rowIsNull;
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                for (int i = 0; i < left.Count; ++i)
                {
                    DivideSafe(leftArray[left.Index(i)], rightArray[right.Index(i)], out _buffer[i], out _isNull[i], ref areAnyNull);
                }
            }
            else if (!right.Selector.IsSingleValue && !left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                int rightStart = right.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    DivideSafe(leftArray[i + leftStart], rightArray[i + rightStart], out _buffer[i], out _isNull[i], ref areAnyNull);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                int leftStart = left.Selector.StartIndexInclusive;
                long rightValue = rightArray[right.Selector.StartIndexInclusive];

                for (int i = 0; i < count; ++i)
                {
                    DivideSafe(leftArray[i + leftStart], rightValue, out _buffer[i], out _isNull[i], ref areAnyNull);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                long leftValue = leftArray[left.Selector.StartIndexInclusive];
                int rightStart = right.Selector.StartIndexInclusive;

                for (int i = 0; i < count; ++i)
                {
                    DivideSafe(leftValue, rightArray[i + rightStart], out _buffer[i], out _isNull[i], ref areAnyNull);
                }
            }
            else
            {
                DivideSafe(leftArray[left.Selector.StartIndexInclusive], rightArray[right.Selector.StartIndexInclusive], out _buffer[0], out _isNull[0], ref areAnyNull);
                return (areAnyNull ? XArray.Null(_buffer, count) : XArray.Single(_buffer, count));
            }

            return XArray.All(_buffer, count, (areAnyNull ? _isNull : null));
        }

        private static void DivideSafe(long numerator, long denominator, out long result, out bool isNull, ref bool areAnyNull)
        {
            isNull = (denominator == 0);
            result = (isNull ? 0 : numerator / denominator);
            areAnyNull |= isNull;
        }
    }
}
