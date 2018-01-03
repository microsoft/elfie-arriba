using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Commands;
using XForm.Data;
using XForm.Types;

namespace XForm.Functions.String
{
    internal class CoalesceBuilder : IFunctionBuilder
    {
        public string Name => "Coalesce";
        public string Usage => "Coalesce([ColumnName], [...n])";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            List<IDataBatchColumn> columns = new List<IDataBatchColumn>();

            while (context.Parser.HasAnotherArgument)
            {
                columns.Add(context.Parser.NextColumn(source, context));
            }

            if (columns.Count < 2)
            {
                throw new ArgumentOutOfRangeException("Coalesce requries at least two input columns.");
            }

            return CoalesceColumn.Build(
                source,
                columns);
        }
    }

    public class CoalesceColumn : IDataBatchColumn
    {
        private IEnumerable<IDataBatchColumn> Columns { get; }

        private CoalesceColumn(IEnumerable<IDataBatchColumn> columns)
        {
            Columns = columns;

            ColumnDetails = new ColumnDetails("Coalesce", typeof(String8), true);
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

        public ColumnDetails ColumnDetails
        {
            get;
        }

        public Func<DataBatch> Getter()
        {
            String8[] transformedArray = null;
            bool[] nullArray = null;

            return () => CoalesceColumn.CoalesceBatch(ref transformedArray, ref nullArray, Columns.Select(column => column.Getter()()).ToArray());
        }

        public static DataBatch CoalesceBatch(ref String8[] transformedArray, ref bool[] nullArray, params DataBatch[] batches)
        {
            Allocator.AllocateToSize(ref transformedArray, batches[0].Count);
            Allocator.AllocateToSize(ref nullArray, batches[0].Count);

            for (int i = 0; i < batches[0].Count; ++i)
            {
                foreach (var column in batches)
                {
                    // Choose the first non-null/empty value
                    if ((column.IsNull != null && !column.IsNull[i]) || (String8)column.Array.GetValue(i) != String8.Empty)
                    {
                        transformedArray[i] = (String8)column.Array.GetValue(i);
                        break;
                    }
                }

                // Update the null array if the value is null/empty
                if (transformedArray[i] == String8.Empty)
                {
                    nullArray[i] = true;
                }
            }

            return DataBatch.All(transformedArray, batches[0].Count, nullArray);
        }
    }
}
