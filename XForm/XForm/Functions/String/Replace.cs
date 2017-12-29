using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using XForm.Commands;
using XForm.Data;
using XForm.Types;

namespace XForm.Functions
{
    internal class ReplaceBuilder : IFunctionBuilder
    {
        public string Name => "Replace";
        public string Usage => "Replace([InputColumnName], [FindText], [ReplaceText?], [ExactMatch?])";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return ReplaceColumn.Build(
                source,
                context.Parser.NextColumn(source, context),
                context.Parser.NextString(),
                context.Parser.NextString(),
                (context.Parser.HasAnotherArgument ? context.Parser.NextBoolean() : false));
        }
    }

    public class ReplaceColumn : IDataBatchColumn
    {
        private IDataBatchColumn Column { get; }
        private String8 FindText { get; }
        private String8 ReplaceText { get; }
        private bool ExactMatch { get; }

        private ReplaceColumn(IDataBatchColumn column, String8 findText, String8 replaceText, bool exactMatch)
        {
            Column = column;
            FindText = findText;
            ReplaceText = replaceText;
            ExactMatch = exactMatch;
        }

        public static IDataBatchColumn Build(IDataBatchEnumerator source, IDataBatchColumn inputColumn, string findText, string replaceText, bool exactMatch)
        {
            // Replace only works on String8 columns
            if (inputColumn.ColumnDetails.Type != typeof(String8))
            {
                return inputColumn;
            }

            String8 find = findText == null ? String8.Empty : String8.Convert(findText, new byte[String8.GetLength(findText)]);
            String8 replace = replaceText == null ? String8.Empty : String8.Convert(replaceText, new byte[String8.GetLength(replaceText)]);

            if (inputColumn is Constant)
            {
                // If the inner value is a constant, convert once and store the new constant
                return new Constant(source, ReplaceColumn.Replace((String8)((Constant)inputColumn).Value, find, replace, new String8Block()), typeof(String8));
            }
            else
            {
                return new ReplaceColumn(inputColumn, find, replace, exactMatch);
            }
        }

        public ColumnDetails ColumnDetails
        {
            get
            {
                return Column.ColumnDetails;
            }
        }

        public Func<DataBatch> Getter()
        {
            String8[] transformedArray = null;
            String8Block block = new String8Block();
            return () => ReplaceBatch(Column.Getter()(), block, ref transformedArray);
        }

        private DataBatch ReplaceBatch(DataBatch batch, String8Block block, ref String8[] transformedArray)
        {
            block.Clear();
            Allocator.AllocateToSize(ref transformedArray, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                String8 originalValue = sourceArray[batch.Index(i)];
                transformedArray[i] = ReplaceColumn.Replace(originalValue, FindText, ReplaceText, block);
            }

            return DataBatch.All(transformedArray, batch.Count);
        }

        public static String8 Replace(String8 originalValue, String8 findString, String8 replaceString, String8Block block)
        {
            int startIndex = -1;
            int currentIndex = 0;
            String8 newValue = String8.Empty;

            while (true)
            {
                // Find the start of the escaped string
                startIndex = originalValue.IndexOf(findString, currentIndex);

                if (startIndex >= 0)
                {
                    // Append the string before the escape sequence
                    newValue = block.Concatenate(newValue, String8.Empty, originalValue.Substring(currentIndex, startIndex - currentIndex));

                    // Append the escaped string
                    newValue = block.Concatenate(newValue, String8.Empty, replaceString);

                    currentIndex = startIndex + findString.Length;
                }
                else
                {
                    break;
                }
            }

            // Copy the remaining string
            if (currentIndex < originalValue.Length)
            {
                newValue = block.Concatenate(newValue, String8.Empty, originalValue.Substring(currentIndex, originalValue.Length - currentIndex));
            }

            return newValue;
        }
    }
}
