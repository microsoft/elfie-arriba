using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.Linq;
using XForm.Data;

namespace XForm.Writers
{
    public class TabularFileWriter : IDisposable
    {
        private IDataBatchSource _source;
        private ITabularWriter _writer;

        public TabularFileWriter(IDataBatchSource source, ITabularWriter writer)
        {
            this._source = source;
            this._writer = writer;
        }

        public void Copy(int rowLimit = -1)
        {
            int columnCount = _source.Columns.Count;

            // :/
            Func<DataBatch>[] getters = new Func<DataBatch>[columnCount];
            for (int i = 0; i < columnCount; ++i)
            {
                getters[i] = _source.ColumnGetter(i);
            }

            // :/
            DataBatch[] columnBatches = new DataBatch[columnCount];
            String8[][] columnArrays = new String8[columnCount][];

            _writer.SetColumns(_source.Columns.Select((cd) => cd.Name));

            while (_source.Next(1))
            {
                for (int i = 0; i < getters.Length; ++i)
                {
                    columnBatches[i] = getters[i]();
                    columnArrays[i] = (String8[])columnBatches[i].Array;
                }

                // Note: Invariant: All columns should have ranges of the same length
                int length = columnBatches[0].EndIndexExclusive - columnBatches[0].StartIndexInclusive;

                for (int rowIndex = 0; rowIndex < length; ++rowIndex)
                {
                    for(int colIndex = 0; colIndex < columnCount; ++colIndex)
                    {
                        // :/
                        int arrayIndex = columnBatches[colIndex].StartIndexInclusive + rowIndex;
                        if (columnBatches[colIndex].Indices != null) arrayIndex = columnBatches[colIndex].Indices[arrayIndex];
                        String8 value = columnArrays[colIndex][arrayIndex];

                        _writer.Write(value);
                    }

                    _writer.NextRow();
                    if (_writer.RowCountWritten == rowLimit) break;
                }
            }
        }

        public void Dispose()
        {
            if(this._source != null)
            {
                this._source.Dispose();
                this._source = null;
            }

            if(this._writer != null)
            {
                this._writer.Dispose();
                this._writer = null;
            }
        }
    }
}
