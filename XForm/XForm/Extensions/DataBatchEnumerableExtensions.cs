using XForm.Data;

namespace XForm.Extensions
{
    public static class DataBatchEnumerableExtensions
    {
        public static int Run(this IDataBatchEnumerator pipeline, int batchSize = 10240)
        {
            int rowsWritten = 0;
            while (true)
            {
                int batchCount = pipeline.Next(batchSize);
                if (batchCount == 0) break;
                rowsWritten += batchCount;
            }
            return rowsWritten;
        }
    }
}
