// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Functions
{
    /// <summary>
    ///  Constant implements IXColumn for a single constant value.
    /// </summary>
    public class Constant : IXColumn
    {
        public bool IsNull { get; private set; }
        public bool WasUnwrappedLiteral { get; private set; }
        public ColumnDetails ColumnDetails { get; private set; }

        private Array _valueArray;
        private IXTable Source { get; set; }

        public Constant(IXTable source, object value, Type type, bool wasUnwrappedLiteral = false)
        {
            Source = source;

            _valueArray = null;
            Allocator.AllocateToSize(ref _valueArray, 1, type);
            _valueArray.SetValue(value, 0);

            IsNull = (value == null || value.Equals("null"));
            WasUnwrappedLiteral = wasUnwrappedLiteral;
            ColumnDetails = new ColumnDetails(string.Empty, type);
        }

        private XArray Get(ArraySelector selector)
        {
            if (IsNull) return XArray.Null(_valueArray, selector.Count);
            return XArray.Single(_valueArray, selector.Count);
        }

        public object Value
        {
            get { return _valueArray.GetValue(0); }
            set { _valueArray.SetValue(value, 0); }
        }

        public Func<XArray> CurrentGetter()
        {
            return () => Get(Source.CurrentSelector);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return (selector) => Get(selector);
        }

        public Func<XArray> ValuesGetter()
        {
            return () => Get(ArraySelector.Single(1));
        }

        public Type IndicesType => null;

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }

        public override string ToString()
        {
            return XqlScanner.Escape(Value.ToString(), TokenType.Value, WasUnwrappedLiteral);
        }
    }
}
