using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.IO;
using XForm.Data;

namespace XForm.IO
{
    public interface IColumnReader : IDisposable
    {
        DataBatch Next(int desiredCount);
    }

    public class String8ColumnReader : IColumnReader
    {
        private FileStream _bytesReader;
        private FileStream _positionsReader;

        private int[] _positionsBuffer;
        private byte[] _positionBytesBuffer;
        private int _position;
        private byte[] _bytesBuffer;
        private String8[] _resultArray;

        public String8ColumnReader(string tableRootPath, string columnName)
        {
            string columnFilePath = Path.Combine(tableRootPath, columnName);

            _bytesReader = new FileStream(columnFilePath + ".V.s.bin", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _positionsReader = new FileStream(columnFilePath + ".V.p.bin", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public DataBatch Next(int desiredCount)
        {
            int actualCount;

            // Read the int[] for positions
            Allocator.AllocateToSize(ref _positionsBuffer, desiredCount);
            Allocator.AllocateToSize(ref _positionBytesBuffer, desiredCount * 4);
            actualCount = _positionsReader.Read(_positionBytesBuffer, 0, desiredCount * 4) / 4;
            Buffer.BlockCopy(_positionBytesBuffer, 0, _positionsBuffer, 0, actualCount * 4);

            // Return an empty batch if we're out of rows
            if (actualCount == 0) return DataBatch.All(_resultArray, 0);

            // Determine how many bytes we need to read the desired strings
            int lastValueEnd = _positionsBuffer[actualCount - 1];
            int lengthToRead = lastValueEnd - _position;
            Allocator.AllocateToSize(ref _bytesBuffer, lengthToRead);

            // Read the raw string bytes
            int byteArrayOffset = _position;
            Array.Clear(_bytesBuffer, 0, _bytesBuffer.Length);
            _bytesReader.Read(_bytesBuffer, 0, lengthToRead);

            // Update the String8 array to point to them
            Allocator.AllocateToSize(ref _resultArray, actualCount);

            int previousStringEnd = _position;
            for (int i = 0; i < actualCount; ++i)
            {
                int valueEnd = _positionsBuffer[i];
                _resultArray[i] = new String8(_bytesBuffer, previousStringEnd - byteArrayOffset, valueEnd - previousStringEnd);
                previousStringEnd = valueEnd;
            }

            _position = previousStringEnd;

            return DataBatch.All(_resultArray, actualCount);
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
        private DataBatch[] _batches;
        private int _rowCountRead;

        public BinaryTableReader(string tableRootPath)
        {
            _tableRootPath = tableRootPath;
            _columns = SchemaSerializer.Read(_tableRootPath);
            
            _batches = new DataBatch[_columns.Count];
            Reset();
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            _readers[columnIndex] = new String8ColumnReader(_tableRootPath, Columns[columnIndex].Name);
            return () => _batches[columnIndex];
        }

        public int Next(int desiredCount)
        {
            // TODO: Lame. Add seekability to avoid getting unread columns.
            int actualCount = -1;
            for(int i = 0; i < _readers.Length; ++i)
            {
                if (_readers[i] != null)
                {
                    _batches[i] = _readers[i].Next(desiredCount);
                    if (actualCount == -1) actualCount = _batches[i].Count;
                    if (actualCount != _batches[i].Count) throw new IOException($"BinaryTable \"{_tableRootPath}\" did not read as many rows as expected from {Columns[i].Name}. After {_rowCountRead:n0} rows, got {_batches[i].Count:n0} rows but other columns had {actualCount:n0} rows.");
                }
            }

            _rowCountRead += actualCount;
            return actualCount;
        }

        public void Reset()
        {
            _rowCountRead = 0;
            _readers = new IColumnReader[_columns.Count];
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
