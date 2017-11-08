using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XForm.Data;

namespace XForm.Writers
{
    public interface IColumnWriter : IDisposable
    {
        void Write(DataBatch batch);
    }

    public class String8ColumnWriter : IColumnWriter
    {
        private FileStream _bytesWriter;
        private FileStream _positionsWriter;

        private int[] _positionsBuffer;
        private byte[] _positionBytesBuffer;
        private int _position;

        public String8ColumnWriter(string tableRootPath, string columnName)
        {
            string columnFilePath = Path.Combine(tableRootPath, columnName);

            _bytesWriter = new FileStream(columnFilePath + ".V.s.bin", FileMode.Create, FileAccess.Write, FileShare.Delete);
            _positionsWriter = new FileStream(columnFilePath + ".V.p.bin", FileMode.Create, FileAccess.Write, FileShare.Delete);
        }

        public void Write(DataBatch batch)
        {
            if (_positionsBuffer == null || _positionsBuffer.Length < batch.Count) _positionsBuffer = new int[batch.Count];
            if (_positionBytesBuffer == null || _positionBytesBuffer.Length * 4 < batch.Count) _positionBytesBuffer = new byte[batch.Count * 4];

            String8[] array = (String8[])batch.Array;
            for(int i = 0; i < batch.Count; ++i)
            {
                String8 value = array[batch.Index(i)];
                value.WriteTo(_bytesWriter);
                _position += value.Length;
                _positionsBuffer[i] = _position;
            }

            Buffer.BlockCopy(_positionsBuffer, 0, _positionBytesBuffer, 0, 4 * batch.Count);
            _positionsWriter.Write(_positionBytesBuffer, 0, 4 * batch.Count);
        }

        public void Dispose()
        {
            if(_bytesWriter != null)
            {
                _bytesWriter.Dispose();
                _bytesWriter = null;
            }

            if(_positionsWriter != null)
            {
                _positionsWriter.Dispose();
                _positionsWriter = null;
            }
        }
    }

    public class BinaryTableWriter : IDisposable
    {
        private IDataBatchEnumerator _source;
        private string _tableRootPath;

        private Func<DataBatch>[] _getters;
        private IColumnWriter[] _writers;

        public BinaryTableWriter(IDataBatchEnumerator source, string tableRootPath)
        {
            _source = source;
            _tableRootPath = tableRootPath;
            Directory.CreateDirectory(tableRootPath);

            int columnCount = source.Columns.Count;

            _getters = new Func<DataBatch>[columnCount];
            _writers = new IColumnWriter[columnCount];

            for (int i = 0; i < columnCount; ++i)
            {
                _getters[i] = source.ColumnGetter(i);
                _writers[i] = new String8ColumnWriter(tableRootPath, source.Columns[i].Name);
            }
        }

        public int RowCountWritten { get; private set; }

        public void Copy(int batchSize = 10240)
        {
            while (true)
            {
                int batchCount = _source.Next(batchSize);
                if (batchCount == 0) break;
                
                for (int i = 0; i < _getters.Length; ++i)
                {
                    _writers[i].Write(_getters[i]());
                }

                RowCountWritten += batchCount;
            }
        }

        public void Dispose()
        {
            if(_source != null)
            {
                _source.Dispose();
                _source = null;
            }

            if(_writers != null)
            {
                foreach(IColumnWriter writer in _writers)
                {
                    writer.Dispose();
                }

                _writers = null;
            }
        }
    }
}
