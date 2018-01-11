using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;

namespace XForm.Functions.String
{
    /// <summary>
    /// Coalesce evaluates the input columns in order and returns the current value of the first column that does not evaluate to NULL or Empty string.
    /// </summary>
    /// <example>
    /// The following command will evaluate the values of the input columns, C1, C2, C3 and return the first non-NULL value.
    ///     set [R1] Coalesce([C1], [C2], [C3])
    ///     
    /// If C1 = NULL, C2 = "bob", C3 = "fred", R2 will be set to "bob".
    /// If C1 = "", C2 = "bob", C3 = "fred", R2 will be set to "bob".
    /// If all the values of the input columns are NULL, the output column will be set to NULL.
    /// </example>
    internal class CoalesceBuilder : IFunctionBuilder
    {
        public string Name => "Coalesce";
        public string Usage => "Coalesce([ColumnName], [...n])";
        public Type ReturnType => typeof(String8);

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            List<IDataBatchColumn> columns = new List<IDataBatchColumn>();

            while (context.Parser.HasAnotherArgument)
            {
                columns.Add(context.Parser.NextColumn(source, context));
            }

            if (columns.Count < 2)
            {
                throw new ArgumentException("Coalesce requries at least two input columns.");
            }

            return CoalesceColumn.Build(
                source,
                columns);
        }
    }

    public class CoalesceColumn : IDataBatchColumn
    {
        private CoalesceColumn(IEnumerable<IDataBatchColumn> columns)
        {
            Columns = columns;

            ColumnDetails = new ColumnDetails("Coalesce", typeof(String8), true);
        }

        private IEnumerable<IDataBatchColumn> Columns { get; }
        public ColumnDetails ColumnDetails { get; }

        public Func<DataBatch> Getter()
        {
            String8[] transformedArray = null;
            bool[] nullArray = null;

            return () => CoalesceColumn.CoalesceBatch(ref transformedArray, ref nullArray, Columns.Select(column => column.Getter()()).ToArray());
        }

        public static IDataBatchColumn Build(IDataBatchEnumerator source, IEnumerable<IDataBatchColumn> columns)
        {
            // Coalesce only works on String8 columns
            if (columns.Any(column => column.ColumnDetails.Type != typeof(String8)))
            {
                throw new ArgumentOutOfRangeException("Every input columns for Coalesce must be of type String8.");
            }

            return new CoalesceColumn(columns);
        }

        /// <summary>
        /// Evaluates the input columns in order and returns the current value of the first column that does not evaluate to NULL or Empty string.
        /// </summary>
        public static DataBatch CoalesceBatch(ref String8[] transformedArray, ref bool[] nullArray, params DataBatch[] batches)
        {
            Allocator.AllocateToSize(ref transformedArray, batches[0].Count);
            Allocator.AllocateToSize(ref nullArray, batches[0].Count);
            Array.Clear(transformedArray, 0, batches[0].Count);
            Array.Clear(nullArray, 0, batches[0].Count);

            bool hasNullValues = false;

            for (int batchIndex = 0; batchIndex < batches[0].Count; ++batchIndex)
            {
                int indexInBackingArray = batches[0].Selector.IsSingleValue ? 0 : batches[0].Index(batchIndex);
                bool isNull = false;

                foreach (var column in batches)
                {
                    isNull = (column.IsNull != null && column.IsNull[indexInBackingArray]) || ((String8)column.Array.GetValue(indexInBackingArray)).Length == 0;

                    if (isNull)
                    {
                        // Skip all columns with a null or empty value.
                        continue;
                    }
                    else
                    {
                        // Take the column that contains a value.
                        transformedArray[batchIndex] = (String8)column.Array.GetValue(indexInBackingArray);
                        break;
                    }
                }

                // If all the columns were null, set the null array.
                if (isNull)
                {
                    nullArray[batchIndex] = true;
                    hasNullValues = true;
                }
            }

            return DataBatch.All(transformedArray, batches[0].Count, hasNullValues ? nullArray : null);
        }
    }
}
