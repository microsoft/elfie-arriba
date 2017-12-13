using System;
using XForm.Data;

namespace XForm.Functions
{
    public class Rename : IDataBatchColumn
    {
        private IDataBatchColumn Column { get; set; }
        public ColumnDetails ColumnDetails { get; private set; }

        public Rename(IDataBatchColumn column, string newName)
        {
            Column = column;
            ColumnDetails = column.ColumnDetails.Rename(newName);
        }

        public Func<DataBatch> Getter()
        {
            return Column.Getter();
        }
    }
}
