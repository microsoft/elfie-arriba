using System;
using XForm.Data;

namespace XForm.Data
{
    public interface IDataBatchColumn
    {
        ColumnDetails ColumnDetails { get; }
        Func<DataBatch> Getter();
    }
}
