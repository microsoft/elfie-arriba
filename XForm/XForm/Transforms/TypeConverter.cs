using System;
using System.Collections.Generic;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Transforms
{
    public class TypeConverter : IDataBatchSource
    {
        private IDataBatchSource _source;
        private int _sourceColumnIndex;
        private Func<DataBatch, DataBatch> _converter;
        private List<ColumnDetails> _columns;

        public TypeConverter(IDataBatchSource source, string columnName, Type targetType, object defaultValue, bool strict)
        {
            _source = source;
            _sourceColumnIndex = source.Columns.IndexOfColumn(columnName);

            ColumnDetails sourceColumn = source.Columns[_sourceColumnIndex];
            _converter = TypeConverterFactory.Build(sourceColumn.Type, targetType, defaultValue, strict);

            _columns = new List<ColumnDetails>();
            for(int i = 0; i < source.Columns.Count; ++i)
            {
                _columns.Add((i == _sourceColumnIndex ? source.Columns[i].ChangeType(targetType) : source.Columns[i]));
            }
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being converted
            if (columnIndex != _sourceColumnIndex) return _source.ColumnGetter(columnIndex);

            // Cache the function to get the source data
            Func<DataBatch> sourceGetter = _source.ColumnGetter(columnIndex);

            // Build the appropriate converter and conversion function
            return () => _converter(sourceGetter());
        }

        public void Reset()
        {
            _source.Reset();
        }

        public int Next(int desiredCount)
        {
            return _source.Next(desiredCount);
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}
