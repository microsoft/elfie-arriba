using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Data;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  DataReaderTabularReader maps an IDataReader to an ITabularReader.
    ///  It allows reading value type values without allocation.
    ///  
    ///  NOTE: The ITabularValues returned from Current(index) are only valid until NextRow is called.
    ///  They must be copied or used before the next row is read.
    /// </summary>
    public class DataReaderTabularReader : ITabularReader
    {
        private IDataReader _reader;
        private List<string> _columnNames;
        private int _rowCountRead;

        private String8Block _block;
        private ObjectTabularValue[] _valueBoxes;

        public DataReaderTabularReader(IDataReader reader)
        {
            this._reader = reader;

            _columnNames = new List<string>();
            foreach (DataRow row in _reader.GetSchemaTable().Rows)
            {
                _columnNames.Add(row.Field<string>("ColumnName"));
            }

            _block = new String8Block();
        }

        public IReadOnlyList<string> Columns
        {
            get { return _columnNames; }
        }

        public int CurrentRowColumns
        {
            get { return _reader.FieldCount; }
        }

        public int RowCountRead
        {
            get { return _rowCountRead; }
        }

        public long BytesRead
        {
            get { return -1; }
        }

        public int ColumnIndex(string columnNameOrIndex)
        {
            int columnIndex;
            if (int.TryParse(columnNameOrIndex, out columnIndex)) return columnIndex;

            for (int i = 0; i < this.Columns.Count; ++i)
            {
                if (String.Compare(columnNameOrIndex, this.Columns[i], true) == 0) return i;
            }

            throw new ColumnNotFoundException(String.Format("Column Name \"{0}\" not found in file.\nKnown Columns: \"{1}\"", columnNameOrIndex, String.Join(", ", _columnNames)));
        }

        public bool NextRow()
        {
            _rowCountRead++;
            _block.Clear();

            bool hasAnotherRow = _reader.Read();

            if (hasAnotherRow)
            {
                // Create a fixed set of ObjectTabularValue instances to contain returned cell values
                // without per value allocation.
                if (_valueBoxes == null || _valueBoxes.Length < _reader.FieldCount)
                {
                    _valueBoxes = new ObjectTabularValue[_reader.FieldCount];

                    for (int i = 0; i < _valueBoxes.Length; ++i)
                    {
                        _valueBoxes[i] = new ObjectTabularValue(_block);
                    }
                }
            }

            return hasAnotherRow;
        }

        public ITabularValue Current(int index)
        {
            _valueBoxes[index].SetValue(_reader[index]);
            return _valueBoxes[index];
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
    }
}
