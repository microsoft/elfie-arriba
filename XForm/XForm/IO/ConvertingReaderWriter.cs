// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Types;

namespace XForm.IO
{
    /// <summary>
    ///  ConvertingWriter converts the column to another type and writes as that type.
    /// </summary>
    public class ConvertingWriter : IColumnWriter
    {
        private Func<XArray, XArray> _converter;
        private IColumnWriter _convertedValueWriter;

        public ConvertingWriter(IColumnWriter convertedValueWriter, Func<XArray, XArray> converter)
        {
            _converter = converter;
            _convertedValueWriter = convertedValueWriter;
        }

        public Type WritingAsType => _convertedValueWriter.WritingAsType;

        public void Append(XArray xarray)
        {
            _convertedValueWriter.Append(_converter(xarray));
        }

        public void Dispose()
        {
            if (_convertedValueWriter != null)
            {
                _convertedValueWriter.Dispose();
                _convertedValueWriter = null;
            }
        }
    }

    /// <summary>
    ///  ConvertingReader wraps a type conversion function and an inner reader together to
    ///  allow reading types which can't be written directly.
    /// </summary>
    public class ConvertingReader : IColumnReader
    {
        private IColumnReader _innerReader;
        private Func<XArray, XArray> _converter;

        private XArray _currentArray;
        private ArraySelector _currentSelector;

        public ConvertingReader(IColumnReader innerReader, Func<XArray, XArray> converter)
        {
            _innerReader = innerReader;
            _converter = converter;
        }

        public int Count => _innerReader.Count;

        public XArray Read(ArraySelector selector)
        {
            if (selector.Equals(_currentSelector)) return _currentArray;

            _currentArray = _converter(_innerReader.Read(selector));
            _currentSelector = selector;
            return _currentArray;
        }

        public void Dispose()
        {
            if (_innerReader != null)
            {
                _innerReader.Dispose();
                _innerReader = null;
            }
        }
    }
}
