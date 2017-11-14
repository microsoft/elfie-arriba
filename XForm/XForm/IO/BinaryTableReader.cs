using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using XForm.Data;

namespace XForm.IO
{
    public interface IColumnReader : IDisposable
    {
        int Count { get; }
        DataBatch Read(ArraySelector selector);
    }

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

    public class String8ColumnReader : IColumnReader
    {
        private FileStream _bytesReader;
        private ArrayReader<int> _positionsReader;

        private byte[] _bytesBuffer;
        private String8[] _resultArray;

        public String8ColumnReader(string tableRootPath, string columnName)
        {
            string columnFilePath = Path.Combine(tableRootPath, columnName);

            _bytesReader = new FileStream(Path.Combine(columnFilePath, "V.s.bin"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _positionsReader = new ArrayReader<int>(new FileStream(Path.Combine(columnFilePath, "Vp.i32.bin"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public int Count => _positionsReader.Count;

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            // Read the end of the previous string
            int start;
            if(selector.StartIndexInclusive == 0)
            {
                start = 0;
            }
            else
            {
                start = ((int[])_positionsReader.Read(ArraySelector.All(Count).Slice(selector.StartIndexInclusive - 1, selector.StartIndexInclusive)).Array)[0];
            }

            // Read the ends of this batch
            int[] positions = (int[])_positionsReader.Read(selector).Array;
            int end = positions[selector.Count - 1];
            int lengthToRead = end - start;

            Allocator.AllocateToSize(ref _bytesBuffer, lengthToRead);
            Allocator.AllocateToSize(ref _resultArray, selector.Count);

            // Read the raw string bytes
            _bytesReader.Seek(start, SeekOrigin.Begin);
            _bytesReader.Read(_bytesBuffer, 0, lengthToRead);

            // Update the String8 array to point to them
            int previousStringEnd = start;
            for (int i = 0; i < selector.Count; ++i)
            {
                int valueEnd = positions[i];
                _resultArray[i] = new String8(_bytesBuffer, previousStringEnd - start, valueEnd - previousStringEnd);
                previousStringEnd = valueEnd;
            }

            return DataBatch.All(_resultArray, selector.Count);
        }

        public void Dispose()
        {
            if (_bytesReader != null)
            {
                _bytesReader.Dispose();
                _bytesReader = null;
            }

            if (_positionsReader != null)
            {
                _positionsReader.Dispose();
                _positionsReader = null;
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
            Reset();
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if(_readers[columnIndex] == null) _readers[columnIndex] = new String8ColumnReader(_tableRootPath, Columns[columnIndex].Name);
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
            _readers = new IColumnReader[_columns.Count];

            _readers[0] = new String8ColumnReader(_tableRootPath, Columns[0].Name);
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
