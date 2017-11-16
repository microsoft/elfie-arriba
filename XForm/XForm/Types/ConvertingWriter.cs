// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Types
{
    /// <summary>
    ///  ConvertingReader wraps a type conversion function and an inner writer together to
    ///  allow writing types which can't be written directly.
    ///  
    ///  [DateTime is converted to a UTC Ticks long and written via the long array.]
    /// </summary>
    public class ConvertingWriter : IColumnWriter
    {
        private IColumnWriter _innerWriter;
        private Func<DataBatch, DataBatch> _converter;

        public ConvertingWriter(IColumnWriter innerWriter, Func<DataBatch, DataBatch> converter)
        {
            _innerWriter = innerWriter;
            _converter = converter;
        }

        public void Append(DataBatch batch)
        {
            _innerWriter.Append(_converter(batch));
        }

        public void Dispose()
        {
            if (_innerWriter != null)
            {
                _innerWriter.Dispose();
                _innerWriter = null;
            }
        }
    }
}
