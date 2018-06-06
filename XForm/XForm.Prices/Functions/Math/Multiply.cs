// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions.Math
{
    internal class MultiplyBuilder : IFunctionBuilder
    {
        public string Name => "Multiply";
        public string Usage => "Multiply({Number}, {Number})";
        public Type ReturnType => typeof(float);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn left = context.Parser.NextColumn(source, context, typeof(float));
            IXColumn right = context.Parser.NextColumn(source, context, typeof(float));
            if (left.ColumnDetails.Type != right.ColumnDetails.Type) throw new ArgumentException("Multiply requires two columns of the same type. Cast first if needed.");

            // ISSUE: How do I make math generic?

            // TODO: If left or right is a constant, make a SimpleTransformFunction

            return new Multiply("Multiply", left, right);
        }
    }

    public class Multiply : IXColumn
    {
        private IXColumn _left;
        private IXColumn _right;
        private float[] _buffer;
        private bool[] _nullBuffer;

        public ColumnDetails ColumnDetails { get; private set; }
        public Type IndicesType => null;

        public Multiply(string name, IXColumn left, IXColumn right)
        {
            _left = left;
            _right = right;
            this.ColumnDetails = new ColumnDetails(name, left.ColumnDetails.Type);
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> leftGetter = _left.CurrentGetter();
            Func<XArray> rightGetter = _right.CurrentGetter();

            return () => Transform(leftGetter(), rightGetter());
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> leftGetter = _left.SeekGetter();
            Func<ArraySelector, XArray> rightGetter = _right.SeekGetter();
            if (leftGetter == null || rightGetter == null) return null;

            return (selector) => Transform(leftGetter(selector), rightGetter(selector));
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        private XArray Transform(XArray left, XArray right)
        {
            float[] leftArray = (float[])left.Array;
            float[] rightArray = (float[])right.Array;

            int count = left.Count;
            Allocator.AllocateToSize(ref _buffer, count);

            if (left.HasNulls || right.HasNulls)
            {
                Allocator.AllocateToSize(ref _nullBuffer, count);

                for (int i = 0; i < count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);

                    _buffer[i] = leftArray[leftIndex] * rightArray[rightIndex];
                    _nullBuffer[i] = (left.NullRows != null && left.NullRows[leftIndex]) || (right.NullRows != null && right.NullRows[rightIndex]);
                }

                return XArray.All(_buffer, left.Count, _nullBuffer);
            }
            else if(left.Selector.Indices != null || right.Selector.Indices != null)
            {
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[left.Index(i)] * rightArray[right.Index(i)];
                }
            }
            else if(left.Selector.IsSingleValue)
            {
                float single = leftArray[left.Index(0)];
                int offset = right.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = single * rightArray[i + offset];
                }
            }
            else if(right.Selector.IsSingleValue)
            {
                float single = rightArray[right.Index(0)];
                int offset = left.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + offset] * single;
                }
            }
            else if(left.Selector.StartIndexInclusive != 0 || right.Selector.StartIndexInclusive != 0)
            {
                int offsetLeft = left.Selector.StartIndexInclusive;
                int offsetRight = right.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i + offsetLeft] * rightArray[i + offsetRight];
                }
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = leftArray[i] * rightArray[i];
                }
            }

            return XArray.All(_buffer, count);
        }
    }
}
