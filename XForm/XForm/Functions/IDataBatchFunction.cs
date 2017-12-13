using System;
using XForm.Data;

namespace XForm.Functions
{
    public interface IDataBatchFunction
    {
        ColumnDetails ReturnType { get; }
        Func<int, DataBatch> Getter();
    }
}
