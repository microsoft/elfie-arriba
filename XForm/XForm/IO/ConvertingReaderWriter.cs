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
        private Func<DataBatch, DataBatch> _converter;
        private IColumnWriter _convertedValueWriter;

        public ConvertingWriter(IColumnWriter convertedValueWriter, Func<DataBatch, DataBatch> converter)
        {
            _converter = converter;
            _convertedValueWriter = convertedValueWriter;
        }

        public void Append(DataBatch batch)
        {
            _convertedValueWriter.Append(_converter(batch));
        }

        public void Dispose()
        {
            if(_convertedValueWriter != null)
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
        private Func<DataBatch, DataBatch> _converter;

        private DataBatch _currentBatch;
        private ArraySelector _currentSelector;

        public ConvertingReader(IColumnReader innerReader, Func<DataBatch, DataBatch> converter)
        {
            _innerReader = innerReader;
            _converter = converter;
        }

        public int Count => _innerReader.Count;

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Equals(_currentSelector)) return _currentBatch;

            _currentBatch = _converter(_innerReader.Read(selector));
            _currentSelector = selector;
            return _currentBatch;
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
