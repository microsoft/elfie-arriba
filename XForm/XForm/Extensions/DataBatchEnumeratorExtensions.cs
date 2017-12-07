// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

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

                DataBatch batch = getter();
                T[] array = (T[])(getter().Array);
                return array[batch.Index(0)];
            }
        }

        public static List<T> ToList<T>(this IDataBatchEnumerator pipeline, string columnName)
        {
            List<T> result = new List<T>();

            using (pipeline)
            {
                Func<DataBatch> getter = pipeline.ColumnGetter(pipeline.Columns.IndexOfColumn(columnName));

                while (pipeline.Next(DefaultBatchSize) != 0)
                {
                    DataBatch batch = getter();
                    T[] array = (T[])batch.Array;
                    for (int i = 0; i < batch.Count; ++i)
                    {
                        result.Add(array[batch.Index(i)]);
                    }
                }
            }

            return result;
        }
    }
}
