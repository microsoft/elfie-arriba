using System;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Functions
{
    public class Column : IDataBatchColumn
    {
        private string ColumnName { get; set; }
        private int ColumnIndex { get; set; }
        private IDataBatchEnumerator Source { get; set; }
        private Func<DataBatch> _cachedGetter;

        public ColumnDetails ColumnDetails => Source.Columns[ColumnIndex];

        public Column(IDataBatchEnumerator source, WorkflowContext context)
        {
            ColumnName = context.Parser.NextColumnName(source);
            ColumnIndex = source.Columns.IndexOfColumn(ColumnName);
            Source = source;
        }

        public Func<DataBatch> Getter()
        {
            if (_cachedGetter == null) _cachedGetter = Source.ColumnGetter(ColumnIndex);
            return _cachedGetter;
        }
    }
}
