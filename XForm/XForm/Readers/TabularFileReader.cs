using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm.Sources
{
    public class TabularFileReader : IDataBatchSource
    {
        private ITabularReader _reader;
        private List<ColumnDetails> _columns;

        public TabularFileReader(ITabularReader reader)
        {
            this._reader = reader;

            this._columns = new List<ColumnDetails>();
            foreach(string columnName in this._reader.Columns)
            {
                this._columns.Add(new ColumnDetails(columnName, typeof(String8), false));
            }
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Make a single item array to contain values (once)
            String8[] array = new String8[1];

            return () =>
            {
                array[0] = _reader.Current(columnIndex).ToString8();
                return DataBatch.All(array, 1);
            };
        }

        public bool Next(int desiredCount)
        {
            return _reader.NextRow();
        }

        public void Dispose()
        {
            if(_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
    }
}
