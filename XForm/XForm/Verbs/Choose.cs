// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Transforms;

namespace XForm.Verbs
{
    internal class ChooseBuilder : IVerbBuilder
    {
        public string Verb => "choose";
        public string Usage => "'choose' [Max|Min] [RankColumn] [IdentityColumns]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            ChooseDirection direction = context.Parser.NextEnum<ChooseDirection>();
            IDataBatchColumn rankColumn = context.Parser.NextColumn(source, context);

            List<IDataBatchColumn> identityColumns = new List<IDataBatchColumn>();
            do
            {
                identityColumns.Add(context.Parser.NextColumn(source, context));
            } while (context.Parser.HasAnotherPart);

            return new Choose(source, direction, rankColumn, identityColumns);
        }
    }

    public class Choose : IDataBatchEnumerator
    {
        private IDataBatchEnumerator _source;

        private RowRemapper _chosenRowsFilter;
        private ChooseDictionary _dictionary;
        private bool _isDictionaryBuilt;
        private int _totalRowsRead;

        private Func<DataBatch>[] _keyColumnGetters;
        private Func<DataBatch> _rankColumnGetter;

        public Choose(IDataBatchEnumerator source, ChooseDirection direction, IDataBatchColumn rankColumn, IList<IDataBatchColumn> keyColumns)
        {
            _source = source;

            // Build a typed dictionary to handle the rank and key column types
            _dictionary = new ChooseDictionary(direction, rankColumn.ColumnDetails, keyColumns.Select((col) => col.ColumnDetails).ToArray());

            // Retrieve the getters for all columns
            _rankColumnGetter = rankColumn.Getter();
            _keyColumnGetters = keyColumns.Select((col) => col.Getter()).ToArray();

            _chosenRowsFilter = new RowRemapper();
        }

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public int CurrentBatchRowCount { get; private set; }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Get the source getter
            Func<DataBatch> sourceGetter = _source.ColumnGetter(columnIndex);

            // Cache an array to remap rows which were chosen
            int[] remapArray = null;

            return () =>
            {
                // Get the source values for this batch
                DataBatch batch = sourceGetter();

                // Remap to just the rows chosen
                return _chosenRowsFilter.Remap(batch, ref remapArray);
            };
        }

        public int Next(int desiredCount)
        {
            // If this is the first call, walk all rows once to find best rows
            if (!_isDictionaryBuilt)
            {
                _isDictionaryBuilt = true;
                BuildChooseDictionary();
                _source.Reset();
            }

            int outerCount;
            while ((outerCount = _source.Next(desiredCount)) > 0)
            {
                // Ask for the indices of rows which were chosen in this page
                DataBatch chosenRows = _dictionary.GetChosenRows(_totalRowsRead, _totalRowsRead + outerCount, _totalRowsRead);

                // Track the total row count (so we know which rows to ask for chosens each time)
                _totalRowsRead += outerCount;

                // Tell the remapper to filter to chosen rows
                _chosenRowsFilter.SetMatches((int[])chosenRows.Array, chosenRows.Count);

                if (chosenRows.Count > 0) return chosenRows.Count;
            }

            return 0;
        }

        private void BuildChooseDictionary()
        {
            DataBatch[] keyBatches = new DataBatch[_keyColumnGetters.Length];
            int[] rowIndices = new int[DataBatchEnumeratorExtensions.DefaultBatchSize];

            int totalSoFar = 0;
            int count;
            while ((count = _source.Next(DataBatchEnumeratorExtensions.DefaultBatchSize)) != 0)
            {
                // Get the rank and key column batches
                DataBatch rankBatch = _rankColumnGetter();
                for (int i = 0; i < keyBatches.Length; ++i)
                {
                    keyBatches[i] = _keyColumnGetters[i]();
                }

                // Build a row index batch for these global row IDs
                for (int i = 0; i < count; ++i)
                {
                    rowIndices[i] = totalSoFar + i;
                }
                DataBatch rowIndexBatch = DataBatch.All(rowIndices, count);
                totalSoFar += count;

                // Add these to the choose dictionary
                _dictionary.Add(keyBatches, rankBatch, rowIndexBatch);
            }
        }

        public void Reset()
        {
            _source.Reset();
            _totalRowsRead = 0;
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}