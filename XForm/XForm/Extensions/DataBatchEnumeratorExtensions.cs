// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        public static T RunAndGetSingleValue<T>(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            Func<DataBatch> getter = pipeline.ColumnGetter(0);
            using (pipeline)
            {
                pipeline.Next(batchSize);

                DataBatch result = getter();
                T[] array = (T[])(getter().Array);
                return array[result.Index(0)];
            }
        }
    }
}
