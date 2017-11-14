using XForm.Data;

namespace XForm.Extensions
{
    public static class DataBatchEnumeratorExtensions
    {
        public const int DefaultBatchSize = 10240;

        public static int Run(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
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

        public static int RunAndDispose(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            using (pipeline)
            {
                return Run(pipeline, batchSize);
            }
        }
    }
}
