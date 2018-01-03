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
    internal class CosmosUnescapeCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "cosmosUnescape";
        public string Usage => this.Verb;

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            if (context.Parser.HasAnotherPart)
            {
                throw new ArgumentOutOfRangeException($"{this.Verb} does not accept any arguments.");
            }

            return new CosmosUnescape(source);
        }
    }

    public class CosmosUnescape : DataBatchEnumeratorWrapper
    {
        HashSet<int> _sourceColumns = new HashSet<int>();
        Dictionary<String8, String8> _replacementMap;
        String8 _bookmarkChar = String8.Convert("#", new byte[1]);

        public CosmosUnescape(IDataBatchEnumerator source) : base(source)
        {
            for (int i = 0; i < source.Columns.Count; i++)
            {
                if (source.Columns[i].Type == typeof(String8))
                {
                    _sourceColumns.Add(i);
                }
            }
        }

        public Dictionary<String8, String8> ReplacementMap
        {
            get
            {
                if (_replacementMap == null)
                {
                    Dictionary<String8, String8> tempMap = new Dictionary<String8, String8>();
                    tempMap.Add(String8.Convert("#R#", new byte[3]), String8.Convert("\r", new byte[1]));
                    tempMap.Add(String8.Convert("#N#", new byte[3]), String8.Convert("\n", new byte[1]));
                    tempMap.Add(String8.Convert("#TAB#", new byte[5]), String8.Convert("\t", new byte[1]));
                    tempMap.Add(String8.Convert("#NULL#", new byte[6]), String8.Empty);
                    tempMap.Add(String8.Convert("#HASH#", new byte[6]), String8.Convert("#", new byte[1]));

                    _replacementMap = tempMap;
                }

                return _replacementMap;
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

            return () => Unescape(sourceGetter(), block, ref transformedArray);
        }

        private DataBatch Unescape(DataBatch batch, String8Block block, ref String8[] transformedArray)
        {
            block.Clear();
            Allocator.AllocateToSize(ref transformedArray, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                String8 originalValue = sourceArray[batch.Index(i)];
                transformedArray[i] = this.Unescape(originalValue, block);
            }

            return DataBatch.All(transformedArray, batch.Count);
        }

        public String8 Unescape(String8 originalValue, String8Block block)
        {
            int startIndex;
            int endIndex = -1;
            int currentIndex = 0;
            String8 newValue = String8.Empty;

            while (true)
            {
                // Find the start of the escaped string
                startIndex = originalValue.IndexOf(_bookmarkChar, currentIndex);

                if (startIndex >= 0)
                {
                    // Find the end of the escaped string
                    endIndex = originalValue.IndexOf(_bookmarkChar, startIndex + 1);
                }

                // If there's no escaped string
                if (startIndex < 0 || endIndex < 0)
                {
                    break;
                }

                // Found a potential escaped string
                String8 escapedValue = originalValue.Substring(startIndex, endIndex - startIndex + 1);
                String8 unescapedValue;

                if (!this.ReplacementMap.TryGetValue(escapedValue, out unescapedValue))
                {
                    // Since the escaped value wasn't one of the known escaped values, do not replace with the unescaped value.
                    unescapedValue = originalValue.Substring(startIndex, endIndex - startIndex);
                    endIndex = endIndex - 1;
                }

                // Append the string before the escape sequence
                newValue = block.Concatenate(newValue, String8.Empty, originalValue.Substring(currentIndex, startIndex - currentIndex));

                // Append the escaped string
                newValue = block.Concatenate(newValue, String8.Empty, unescapedValue);

                currentIndex = endIndex + 1;
            }

            // Copy the remaining string
            newValue = block.Concatenate(newValue, String8.Empty, originalValue.Substring(currentIndex, originalValue.Length - currentIndex));

            return newValue;
        }
    }
}
