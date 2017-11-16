// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Types
{
    /// <summary>
    ///  ConvertingReader wraps a type conversion function and an inner reader together to
    ///  allow reading types which can't be written directly.
    ///  
    ///  [DateTime is converted to a UTC Ticks long and written via the long array.]
    /// </summary>
    public class ConvertingReader : IColumnReader
    {
        private IColumnReader _innerReader;
        private Func<DataBatch, DataBatch> _converter;

        public ConvertingReader(IColumnReader innerReader, Func<DataBatch, DataBatch> converter)
        {
            _innerReader = innerReader;
            _converter = converter;
        }

        public int Count => _innerReader.Count;

        public void Dispose()
        {
            if (_innerReader != null)
            {
                _innerReader.Dispose();
                _innerReader = null;
            }
        }

        public DataBatch Read(ArraySelector selector)
        {
            return _converter(_innerReader.Read(selector));
        }
    }
}
