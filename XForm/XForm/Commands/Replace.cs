// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Commands
{
    internal class ReplaceCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "replace";
        public string Usage => this.Verb + " <find string> <replace string>";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            string findString = context.Parser.NextString();
            string replaceString = context.Parser.NextString();

            if (context.Parser.HasAnotherPart)
            {
                throw new ArgumentOutOfRangeException($"{this.Verb} only accepts two arguments.");
            }

            return new Replace(source, findString, replaceString);
        }
    }

    public class Replace : DataBatchEnumeratorWrapper
    {
        HashSet<int> _sourceColumns = new HashSet<int>();
        String8 _findString;
        String8 _replaceString;

        public Replace(IDataBatchEnumerator source, string findString, string replaceString) : base(source)
        {
            _findString = String8.Convert(findString, new byte[String8.GetLength(findString)]);
            _replaceString = String8.Convert(replaceString, new byte[String8.GetLength(replaceString)]);

            for (int i = 0; i < source.Columns.Count; i++)
            {
                if (source.Columns[i].Type == typeof(String8))
                {
                    _sourceColumns.Add(i);
                }
            }
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            String8[] transformedArray = null;
            String8Block block = new String8Block();

            // Pass through columns other than the one being converted
            if (!_sourceColumns.Contains(columnIndex)) return _source.ColumnGetter(columnIndex);

            // Cache the function to get the source data
            Func<DataBatch> sourceGetter = _source.ColumnGetter(columnIndex);

            return () => ReplaceText(sourceGetter(), block, ref transformedArray);
        }

        private DataBatch ReplaceText(DataBatch batch, String8Block block, ref String8[] transformedArray)
        {
            block.Clear();
            Allocator.AllocateToSize(ref transformedArray, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                String8 originalValue = sourceArray[batch.Index(i)];

                if (originalValue == null)
                {
                    transformedArray[i] = originalValue;
                }
                else
                {
                    transformedArray[i] = this.ReplaceText(originalValue, block);
                }
            }

            return DataBatch.All(transformedArray, batch.Count);
        }

        public String8 ReplaceText(String8 originalValue, String8Block block)
        {
            int startIndex = -1;
            int currentIndex = 0;
            String8 newValue = String8.Empty;

            while (true)
            {
                // Find the start of the escaped string
                startIndex = originalValue.IndexOf(_findString, currentIndex);

                if (startIndex >= 0)
                {
                    // Append the string before the escape sequence
                    newValue = block.Concatenate(newValue, String8.Empty, originalValue.Substring(currentIndex, startIndex - currentIndex));

                    // Append the escaped string
                    newValue = block.Concatenate(newValue, String8.Empty, _replaceString);

                    currentIndex = startIndex + _findString.Length;
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
