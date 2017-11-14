using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using XForm.Data;
using XForm.Types;

namespace XForm.IO
{
    public class ArrayReader<T> : IColumnReader
    {
        private int _bytesPerItem;
        private FileStream _stream;
        private byte[] _bytesBuffer;
        private T[] _array;

        public ArrayReader(FileStream stream)
        {
            _stream = stream;
            _bytesPerItem = (typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>());
        }

        public int Count => (int)(_stream.Length / _bytesPerItem);

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            int bytesToRead = _bytesPerItem * selector.Count;

            Allocator.AllocateToSize(ref _bytesBuffer, bytesToRead);
            Allocator.AllocateToSize(ref _array, selector.Count);

            _stream.Seek(_bytesPerItem * selector.StartIndexInclusive, SeekOrigin.Begin);
            _stream.Read(_bytesBuffer, 0, bytesToRead);
            Buffer.BlockCopy(_bytesBuffer, 0, _array, 0, bytesToRead);

            return DataBatch.All(_array, selector.Count);
        }

        public void Dispose()
        {
            if(_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }

    public class BinaryTableReader : IDataBatchEnumerator
    {
        private string _tableRootPath;
        private List<ColumnDetails> _columns;
        private IColumnReader[] _readers;

        private int _totalCount;
        private int _currentRowIndex;
        private int _currentBatchCount;

        public BinaryTableReader(string tableRootPath)
        {
            _tableRootPath = tableRootPath;
            _columns = SchemaSerializer.Read(_tableRootPath);
            _readers = new IColumnReader[_columns.Count];
            Reset();
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if (_readers[columnIndex] == null)
            {
                ColumnDetails column = Columns[columnIndex];
                _readers[columnIndex] = TypeProviderFactory.TryGet(column.Type).BinaryReader(Path.Combine(_tableRootPath, column.Name));
            }

            return () => _readers[columnIndex].Read(ArraySelector.All(_totalCount).Slice(_currentRowIndex, _currentRowIndex + _currentBatchCount));
        }

        public int Next(int desiredCount)
        {
            _currentRowIndex += _currentBatchCount;
            _currentBatchCount = desiredCount;
            if (_currentRowIndex + _currentBatchCount > _totalCount) _currentBatchCount = _totalCount - _currentRowIndex;

            return _currentBatchCount;
        }

        public void Reset()
        {
            _currentRowIndex = 0;

            // Get the first reader in order to get the row count
            Func<DataBatch> unused = ColumnGetter(0);
            _totalCount = _readers[0].Count;
        }

        public void Dispose()
        {
            if(_readers != null)
            {
                foreach(IColumnReader reader in _readers)
                {
                    if(reader != null) reader.Dispose();
                }

                _readers = null;
            }
        }
    }
}
