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

        //private String8Block _block;
        //private String8[][] _cells;
        //private int _currentBatchCount;

        public TabularFileReader(ITabularReader reader)
        {
            this._reader = reader;

            this._columns = new List<ColumnDetails>();
            foreach(string columnName in this._reader.Columns)
            {
                this._columns.Add(new ColumnDetails(columnName, typeof(String8), false));
            }

            //_block = new String8Block();
            //_cells = new String8[this._columns.Count][];
            //for(int i = 0; i < _cells.Length; ++i)
            //{
            //    _cells[i] = new String8[10];
            //}
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            String8[] array = new String8[1];

            return () =>
            {
                array[0] = _reader.Current(columnIndex).ToString8();
                return DataBatch.All(array, 1);
            };

            //return () =>
            //{
            //    return DataBatch.All(_cells[columnIndex], _currentBatchCount);
            //};
        }

        public bool Next(int desiredCount)
        {
            return _reader.NextRow();
            //_block.Clear();
            //_currentBatchCount = 0;

            //while(_reader.NextRow())
            //{
            //    for(int i = 0; i < _cells.Length; ++i)
            //    {
            //        _cells[i][_currentBatchCount] = _block.GetCopy(_reader.Current(i).ToString8());
            //    }

            //    _currentBatchCount++;
            //    if (_currentBatchCount == 10) return true;
            //}

            //return _currentBatchCount > 0;
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
