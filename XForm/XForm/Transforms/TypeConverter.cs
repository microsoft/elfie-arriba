using System;
using System.Collections.Generic;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Transforms
{
    public class TypeConverter : DataBatchEnumeratorWrapper
    {
        private int _sourceColumnIndex;
        private Func<DataBatch, DataBatch> _converter;
        private List<ColumnDetails> _columns;

        public TypeConverter(IDataBatchEnumerator source, string columnName, Type targetType, object defaultValue, bool strict) : base(source)
        {
            _sourceColumnIndex = source.Columns.IndexOfColumn(columnName);

            ColumnDetails sourceColumn = source.Columns[_sourceColumnIndex];
            if (!sourceColumn.Type.Equals(targetType))
            {
                _converter = TypeConverterFactory.GetConverter(sourceColumn.Type, targetType, defaultValue, strict);
            }

            _columns = new List<ColumnDetails>();
            for(int i = 0; i < source.Columns.Count; ++i)
            {
                _columns.Add((i == _sourceColumnIndex ? source.Columns[i].ChangeType(targetType) : source.Columns[i]));
            }
        }

        public override IReadOnlyList<ColumnDetails> Columns => _columns;

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being converted
            if (columnIndex != _sourceColumnIndex) return _source.ColumnGetter(columnIndex);

            // Cache the function to get the source data
            Func<DataBatch> sourceGetter = _source.ColumnGetter(columnIndex);

            // Return the getter alone if the type was already right
            if (_converter == null) return sourceGetter;

            // Return the converter if conversion was needed
            return () => _converter(sourceGetter());
        }
    }
}
