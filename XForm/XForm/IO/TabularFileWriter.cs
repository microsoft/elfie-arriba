using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;
using XForm.Transforms;

namespace XForm.IO
{
    public class TabularFileWriter : IDataBatchEnumerator
    {
        private string _outputFilePath;
        private IDataBatchEnumerator _source;
        private ITabularWriter _writer;

        private Func<DataBatch>[] _stringColumnGetters;

        public TabularFileWriter(IDataBatchEnumerator source, string outputFilePath)
        {
            this._source = source;
            this._outputFilePath = outputFilePath;
            Initialize();
        }

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _source.ColumnGetter(columnIndex);
        }

        public void Reset()
        {
            _source.Reset();
            Initialize();
        }

        private void Initialize()
        {
            this._stringColumnGetters = new Func<DataBatch>[_source.Columns.Count];
            for (int i = 0; i < _source.Columns.Count; ++i)
            {
                Func<DataBatch> rawGetter = _source.ColumnGetter(i);
                Func<DataBatch> stringGetter = rawGetter;

                if (_source.Columns[i].Type != typeof(String8))
                {
                    Func<DataBatch, DataBatch> converter = TypeConverterFactory.Build(_source.Columns[i].Type, typeof(String8), String8.Empty, false);
                    stringGetter = () => (converter(rawGetter()));
                }

                _stringColumnGetters[i] = stringGetter;
            }

            _writer = TabularFactory.BuildWriter(_outputFilePath);
            _writer.SetColumns(_source.Columns.Select((cd) => cd.Name));
        }

        public int Next(int desiredCount)
        {
            // Or smaller batchsize?
            int rowCount = _source.Next(desiredCount);
            if (rowCount == 0) return 0;

            DataBatch[] batches = new DataBatch[_stringColumnGetters.Length];
            for (int i = 0; i < _stringColumnGetters.Length; ++i)
            {
                batches[i] = _stringColumnGetters[i]();
            }

            for (int rowIndex = 0; rowIndex < rowCount; ++rowIndex)
            {
                for(int colIndex = 0; colIndex < _stringColumnGetters.Length; ++colIndex)
                {
                    String8 value = ((String8[])batches[colIndex].Array)[batches[colIndex].Index(rowIndex)];
                    _writer.Write(value);
                }

                _writer.NextRow();
            }

            return rowCount;
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
