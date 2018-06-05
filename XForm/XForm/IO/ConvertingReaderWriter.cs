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

        private XArray _lastSource;
        private XArray _lastConverted;

        public ConvertingWriter(IColumnWriter convertedValueWriter, Func<XArray, XArray> converter)
        {
            _converter = converter;
            _convertedValueWriter = convertedValueWriter;
        }

        public Type WritingAsType => _convertedValueWriter.WritingAsType;

        private XArray Convert(XArray xarray)
        {
            if (xarray.Equals(_lastSource)) return _lastConverted;

            _lastSource = xarray;
            _lastConverted = _converter(xarray);

            return _lastConverted;

        }

        public bool CanAppend(XArray xarray)
        {
            return _convertedValueWriter.CanAppend(Convert(xarray));
        }

        public void Append(XArray xarray)
        {
            _convertedValueWriter.Append(Convert(xarray));
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

        private ConvertingReader(IColumnReader innerReader, Func<XArray, XArray> converter)
        {
            _innerReader = innerReader;
            _converter = converter;
        }

        public static IColumnReader Build(IColumnReader innerReader, Func<XArray, XArray> converter)
        {
            // If the inner column is null, return null (so the column cache can track columns which aren't present)
            if (innerReader == null) return null;

            return new ConvertingReader(innerReader, converter);
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
