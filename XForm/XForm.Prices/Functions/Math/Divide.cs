// Copydenominator (c) Microsoft. All denominators reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions.Math
{
    internal class DivideBuilder : IFunctionBuilder
    {
        public string Name => "Divide";
        public string Usage => "Divide({Numerator}, {Denominator})";
        public Type ReturnType => typeof(float);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn numerator = context.Parser.NextColumn(source, context, typeof(float));
            IXColumn denominator = context.Parser.NextColumn(source, context, typeof(float));
            if (numerator.ColumnDetails.Type != denominator.ColumnDetails.Type) throw new ArgumentException("Divide requires two columns of the same type. Cast first if needed.");

            // ISSUE: How do I make math generic?

            // TODO: If numerator or denominator is a constant, make a SimpleTransformFunction

            return new Divide("Divide", numerator, denominator);
        }
    }

    public class Divide : IXColumn
    {
        private IXColumn _numerator;
        private IXColumn _denominator;
        private float[] _buffer;
        private bool[] _nullBuffer;

        public ColumnDetails ColumnDetails { get; private set; }
        public Type IndicesType => null;

        public Divide(string name, IXColumn numerator, IXColumn denominator)
        {
            _numerator = numerator;
            _denominator = denominator;
            this.ColumnDetails = new ColumnDetails(name, numerator.ColumnDetails.Type);
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> numeratorGetter = _numerator.CurrentGetter();
            Func<XArray> denominatorGetter = _denominator.CurrentGetter();

            return () => Transform(numeratorGetter(), denominatorGetter());
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> numeratorGetter = _numerator.SeekGetter();
            Func<ArraySelector, XArray> denominatorGetter = _denominator.SeekGetter();
            if (numeratorGetter == null || denominatorGetter == null) return null;

            return (selector) => Transform(numeratorGetter(selector), denominatorGetter(selector));
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

        private static float DivideInternal(float numerator, float denominator)
        {
            return numerator / denominator;
        }

        private XArray Transform(XArray numerator, XArray denominator)
        {
            float[] numeratorArray = (float[])numerator.Array;
            float[] denominatorArray = (float[])denominator.Array;

            int count = numerator.Count;
            Allocator.AllocateToSize(ref _buffer, count);

            if (numerator.HasNulls || denominator.HasNulls)
            {
                Allocator.AllocateToSize(ref _nullBuffer, count);

                for (int i = 0; i < count; ++i)
                {
                    int numeratorIndex = numerator.Index(i);
                    int denominatorIndex = denominator.Index(i);

                    _buffer[i] = DivideInternal(numeratorArray[numeratorIndex], denominatorArray[denominatorIndex]);
                    _nullBuffer[i] = (numerator.NullRows != null && numerator.NullRows[numeratorIndex]) || (denominator.NullRows != null && denominator.NullRows[denominatorIndex]);
                }

                return XArray.All(_buffer, numerator.Count, _nullBuffer);
            }
            else if (numerator.Selector.Indices != null || denominator.Selector.Indices != null)
            {
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = DivideInternal(numeratorArray[numerator.Index(i)], denominatorArray[denominator.Index(i)]);
                }
            }
            else if (numerator.Selector.IsSingleValue)
            {
                float single = numeratorArray[numerator.Index(0)];
                int offset = denominator.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = DivideInternal(single, denominatorArray[i + offset]);
                }
            }
            else if (denominator.Selector.IsSingleValue)
            {
                float single = denominatorArray[denominator.Index(0)];
                int offset = numerator.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = DivideInternal(numeratorArray[i + offset], single);
                }
            }
            else if (numerator.Selector.StartIndexInclusive != 0 || denominator.Selector.StartIndexInclusive != 0)
            {
                int offsetLeft = numerator.Selector.StartIndexInclusive;
                int offsetRight = denominator.Selector.StartIndexInclusive;
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = DivideInternal(numeratorArray[i + offsetLeft], denominatorArray[i + offsetRight]);
                }
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    _buffer[i] = DivideInternal(numeratorArray[i], denominatorArray[i]);
                }
            }

            return XArray.All(_buffer, count);
        }
    }
}
