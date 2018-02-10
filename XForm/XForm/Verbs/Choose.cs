// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using XForm.Columns;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Transforms;

namespace XForm.Verbs
{
    internal class ChooseBuilder : IVerbBuilder
    {
        public string Verb => "choose";
        public string Usage => "choose {Max|Min} {RankColumn} {IdentityColumns}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            ChooseDirection direction = context.Parser.NextEnum<ChooseDirection>();
            IXColumn rankColumn = context.Parser.NextColumn(source, context);

            List<IXColumn> identityColumns = new List<IXColumn>();
            do
            {
                identityColumns.Add(context.Parser.NextColumn(source, context));
            } while (context.Parser.HasAnotherPart);

            return new Choose(source, direction, rankColumn, identityColumns);
        }
    }

    public class Choose : IXTable
    {
        private IXTable _source;
        private IXColumn[] _columns;

        private RowRemapper _chosenRowsFilter;
        private ChooseDictionary _dictionary;
        private bool _isDictionaryBuilt;
        private int _totalRowsRead;

        private Func<XArray>[] _keyColumnGetters;
        private Func<XArray> _rankColumnGetter;

        public Choose(IXTable source, ChooseDirection direction, IXColumn rankColumn, IList<IXColumn> keyColumns)
        {
            if (source == null) throw new ArgumentNullException("source");
            _source = source;

            // Build a typed dictionary to handle the rank and key column types
            _dictionary = new ChooseDictionary(direction, rankColumn.ColumnDetails, keyColumns.Select((col) => col.ColumnDetails).ToArray());

            // Retrieve the getters for all columns
            _rankColumnGetter = rankColumn.CurrentGetter();
            _keyColumnGetters = keyColumns.Select((col) => col.CurrentGetter()).ToArray();

            _chosenRowsFilter = new RowRemapper();

            _columns = source.Columns.Select((col) => new RemappedColumn(col, _chosenRowsFilter)).ToArray();
        }

        public IReadOnlyList<IXColumn> Columns => _columns;
        public int CurrentRowCount { get; private set; }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // If this is the first call, walk all rows once to find best rows
            if (!_isDictionaryBuilt)
            {
                _isDictionaryBuilt = true;
                BuildChooseDictionary(cancellationToken);
                _source.Reset();
            }

            int outerCount;
            while ((outerCount = _source.Next(desiredCount, cancellationToken)) > 0)
            {
                // Ask for the indices of rows which were chosen in this page
                XArray chosenRows = _dictionary.GetChosenRows(_totalRowsRead, _totalRowsRead + outerCount, _totalRowsRead);

                // Track the total row count (so we know which rows to ask for chosens each time)
                _totalRowsRead += outerCount;

                // Tell the remapper to filter to chosen rows
                _chosenRowsFilter.SetMatches((int[])chosenRows.Array, chosenRows.Count);

                if (chosenRows.Count > 0)
                {
                    CurrentRowCount = chosenRows.Count;
                    return CurrentRowCount;
                }
            }

            return 0;
        }

        private void BuildChooseDictionary(CancellationToken cancellationToken)
        {
            XArray[] keyarrays = new XArray[_keyColumnGetters.Length];
            int[] rowIndices = new int[XTableExtensions.DefaultBatchSize];

            int totalSoFar = 0;
            int count;
            while ((count = _source.Next(XTableExtensions.DefaultBatchSize, cancellationToken)) != 0)
            {
                // Get the rank and key column arrays
                XArray rankArray = _rankColumnGetter();
                for (int i = 0; i < keyarrays.Length; ++i)
                {
                    keyarrays[i] = _keyColumnGetters[i]();
                }

                // Build a row index xarray for these global row IDs
                for (int i = 0; i < count; ++i)
                {
                    rowIndices[i] = totalSoFar + i;
                }
                XArray rowIndexArray = XArray.All(rowIndices, count);
                totalSoFar += count;

                // Add these to the choose dictionary
                _dictionary.Add(keyarrays, rankArray, rowIndexArray);
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