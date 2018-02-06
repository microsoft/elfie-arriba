// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Query;

namespace XForm.Columns
{
    /// <summary>
    ///  SingleValueColumn implements IXColumn for a single value that is not known before Next()
    /// </summary>
    public class SingleValueColumn : IXColumn
    {
        public ColumnDetails ColumnDetails { get; private set; }

        private bool _isNull;
        private Array _array;
        private IXTable _source;

        public SingleValueColumn(IXTable source, string columnName, Type type)
        {
            _source = source;

            Allocator.AllocateToSize(ref _array, 1, type);
            ColumnDetails = new ColumnDetails(columnName, type);
        }

        public void Set(object value)
        {
            _array.SetValue(value, 0);
            _isNull = (value == null || value.Equals("null"));
        }

        private XArray Get()
        {
            return (_isNull ? XArray.Null(_array, _source.CurrentRowCount) : XArray.Single(_array, _source.CurrentRowCount));
        }

        public Func<XArray> CurrentGetter()
        {
            return () => Get();
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return null;
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
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
            return XqlScanner.Escape((_array.GetValue(0) ?? "null").ToString(), TokenType.Value, (_isNull || ColumnDetails.Type != typeof(String8)));
        }
    }
}
