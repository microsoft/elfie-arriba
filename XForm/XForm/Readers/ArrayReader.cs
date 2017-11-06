using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm.Readers
{
    public class ArrayReader : IDataBatchSource
    {
        private List<ColumnDetails> _columns;
        private List<Array> _arrays;
        private List<int> _arrayLengths;
        private bool _walked;

        public ArrayReader()
        {
            _columns = new List<ColumnDetails>();
            _arrays = new List<Array>();
            _arrayLengths = new List<int>();
        }

        public void AddColumn(ColumnDetails details, Array array, int length)
        {
            for(int i = 0; i < _columns.Count; ++i)
            {
                if (_columns[i].Name.Equals(details.Name, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"Can't add duplicate column. ArrayReader already has a column {details.Name}.");
            }

            _columns.Add(details);
            _arrays.Add(array);
            _arrayLengths.Add(length);
        }

        public IReadOnlyList<ColumnDetails> Columns => this._columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return () => DataBatch.All(_arrays[columnIndex], _arrayLengths[columnIndex]);
        }

        public bool Next(int desiredCount)
        {
            if(!_walked)
            {
                _walked = true;
                return true;
            }

            return false;
        }

        public void Dispose()
        { }
    }
}
