// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Query;

namespace XForm.Columns
{
    /// <summary>
    ///  ConstantColumn implements IXColumn for a single constant value.
    /// </summary>
    public class ConstantColumn : IXColumn
    {
        public bool IsNull { get; private set; }
        public bool WasUnwrappedLiteral { get; private set; }
        public ColumnDetails ColumnDetails { get; private set; }

        private Array _array;
        private XArray _xArray;
        private IXTable Source { get; set; }

        public ConstantColumn(IXTable source, object value, Type type, bool wasUnwrappedLiteral = false)
        {
            Source = source;

            Allocator.AllocateToSize(ref _array, 1, type);
            _array.SetValue(value, 0);

            IsNull = (value == null || (value.Equals("null") && wasUnwrappedLiteral));
            _xArray = (IsNull ? XArray.Null(_array, 1) : XArray.Single(_array, 1));

            WasUnwrappedLiteral = wasUnwrappedLiteral;
            ColumnDetails = new ColumnDetails(string.Empty, type);
        }

        private XArray Get(int count)
        {
            if (IsNull) return XArray.Null(_array, count);
            return XArray.Single(_array, count);
        }

        public object Value
        {
            get { return _xArray.Array.GetValue(0); }
            set { _xArray.Array.SetValue(value, 0); }
        }

        public Func<XArray> CurrentGetter()
        {
            return () => Get(Source.CurrentRowCount);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return (selector) => Get(selector.Count);
        }

        public Func<XArray> ValuesGetter()
        {
            return () => _xArray;
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

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        public override string ToString()
        {
            return XqlScanner.Escape(Value.ToString(), TokenType.Value, (IsNull || ColumnDetails.Type != typeof(String8)));
        }
    }
}
