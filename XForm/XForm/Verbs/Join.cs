// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;
using XForm.Transforms;
using XForm.Types;

namespace XForm.Verbs
{
    internal class JoinBuilder : IVerbBuilder
    {
        public string Verb => "join";
        public string Usage => "join {FromColumn} {ToTable} {ToColumn} {NewColPrefix}";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, XDatabaseContext context)
        {
            string sourceColumnName = context.Parser.NextColumnName(source);
            IDataBatchEnumerator joinToSource = context.Parser.NextTableSource();
            string joinToColumn = context.Parser.NextColumnName(joinToSource);

            return new Join(
                source,
                sourceColumnName,
                joinToSource,
                joinToColumn,
                (string)context.Parser.NextLiteralValue());
        }
    }

    public class Join : IDataBatchEnumerator
    {
        private IDataBatchEnumerator _source;
        private Type _joinColumnType;
        private int _joinFromColumnIndex;
        private Func<DataBatch> _joinFromColumnGetter;

        private IDataBatchList _cachedJoinSource;
        private IColumnReader _joinToColumnReader;

        private IJoinDictionary _joinDictionary;

        private List<ColumnDetails> _columns;
        private List<int> _mappedColumnIndices;

        private RowRemapper _sourceJoinedRowsFilter;
        private ArraySelector _currentRightSideSelector;

        public Join(IDataBatchEnumerator source, string joinFromColumn, IDataBatchEnumerator joinToSource, string joinToColumn, string joinSidePrefix)
        {
            _source = source;

            _cachedJoinSource = joinToSource as IDataBatchList;
            if (_cachedJoinSource == null) throw new ArgumentException($"Join right-hand-side must be a built binary table.");

            // Request the JoinFromColumn Getter
            _joinFromColumnIndex = source.Columns.IndexOfColumn(joinFromColumn);
            _joinFromColumnGetter = source.ColumnGetter(_joinFromColumnIndex);
            _joinColumnType = source.Columns[_joinFromColumnIndex].Type;

            // Request the JoinToColumn Reader (we'll need it cached)
            int joinToColumnIndex = _cachedJoinSource.Columns.IndexOfColumn(joinToColumn);
            Type joinToColumnType = _cachedJoinSource.Columns[joinToColumnIndex].Type;
            if (joinToColumnType != _joinColumnType) throw new ArgumentException($"Join requires columns of matching types; join from {_joinColumnType.Name} to {joinToColumnType.Name} not supported.");
            _joinToColumnReader = _cachedJoinSource.CachedColumnReader(joinToColumnIndex);

            // All of the main source columns are passed through
            _columns = new List<ColumnDetails>(source.Columns);

            // Find and map the columns coming from the join
            _mappedColumnIndices = new List<int>();
            for (int i = 0; i < _cachedJoinSource.Columns.Count; ++i)
            {
                ColumnDetails column = _cachedJoinSource.Columns[i];
                _columns.Add(column.Rename(joinSidePrefix + column.Name));
                _mappedColumnIndices.Add(i);
            }

            _sourceJoinedRowsFilter = new RowRemapper();
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public int CurrentBatchRowCount { get; private set; }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // If this is one of the joined in columns, return the rows which matched from the join
            if (columnIndex >= _source.Columns.Count)
            {
                int joinColumnIndex = _mappedColumnIndices[columnIndex - _source.Columns.Count];
                IColumnReader cachedColumnReader = _cachedJoinSource.CachedColumnReader(joinColumnIndex);
                return () => cachedColumnReader.Read(_currentRightSideSelector);
            }

            // Otherwise, get the source getter
            Func<DataBatch> sourceGetter = (columnIndex == _joinFromColumnIndex ? _joinFromColumnGetter : _source.ColumnGetter(columnIndex));

            // Cache an array to remap rows which joined
            int[] remapArray = null;

            return () =>
            {
                // Get the source values for this batch
                DataBatch batch = sourceGetter();

                // Remap to just the rows which joined
                return _sourceJoinedRowsFilter.Remap(batch, ref remapArray);
            };
        }

        public int Next(int desiredCount)
        {
            // If this is the first call, fully cache the JoinToSource and build a lookup Dictionary
            if (_joinDictionary == null) BuildJoinDictionary();

            BitVector matchedRows = null;

            while (true)
            {
                // Get the next rows from the source
                int count = _source.Next(desiredCount);
                if (count == 0)
                {
                    CurrentBatchRowCount = 0;
                    return 0;
                }

                // Get values to join from
                DataBatch joinFromValues = _joinFromColumnGetter();

                // Find which rows matched and to what right-side row indices
                matchedRows = _joinDictionary.TryGetValues(joinFromValues, out _currentRightSideSelector);

                // Filter left-side rows to the matches (inner join)
                _sourceJoinedRowsFilter.SetMatches(matchedRows);

                if (_currentRightSideSelector.Count > 0) break;
            }

            CurrentBatchRowCount = _currentRightSideSelector.Count;
            return _currentRightSideSelector.Count;
        }

        private void BuildJoinDictionary()
        {
            DataBatch allJoinToValues = _joinToColumnReader.Read(ArraySelector.All(_cachedJoinSource.Count));
            _joinDictionary = (IJoinDictionary)Allocator.ConstructGenericOf(typeof(JoinDictionary<>), _joinColumnType, allJoinToValues.Count);
            _joinDictionary.Add(allJoinToValues, 0);
        }

        public void Reset()
        {
            _source.Reset();
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }

            if (_cachedJoinSource != null)
            {
                _cachedJoinSource.Dispose();
                _cachedJoinSource = null;
            }
        }
    }

    public interface IJoinDictionary
    {
        void Add(DataBatch keys, int firstRowIndex);
        BitVector TryGetValues(DataBatch keys, out ArraySelector rightSideSelector);
    }

    public class JoinDictionary<T> : IJoinDictionary
    {
        // JoinDictionary uses a Dictionary5 internally
        private Dictionary5<T, int> _dictionary;
        private IValueCopier<T> _valueCopier;

        // Reused buffers for the matching row vector and matching row right side indices
        private int[] _returnedIndicesBuffer;
        private BitVector _returnedVector;

        public JoinDictionary(int initialCapacity)
        {
            ITypeProvider typeProvider = TypeProviderFactory.Get(typeof(T));
            IEqualityComparer<T> comparer = new EqualityComparerAdapter<T>(typeProvider.TryGetComparer());
            _dictionary = new Dictionary5<T, int>(comparer, initialCapacity);
            _valueCopier = (IValueCopier<T>)(typeProvider.TryGetCopier());
        }

        public void Add(DataBatch keys, int firstRowIndex)
        {
            T[] keyArray = (T[])keys.Array;

            if (_valueCopier != null || keys.IsNull != null)
            {
                for (int i = 0; i < keys.Count; ++i)
                {
                    int index = keys.Index(i);
                    if (keys.IsNull != null && keys.IsNull[index]) continue;
                    T key = keyArray[index];
                    if (_valueCopier != null) key = _valueCopier.Copy(key);
                    _dictionary[key] = firstRowIndex + i;
                }
            }
            else
            {
                for (int i = 0; i < keys.Count; ++i)
                {
                    int index = keys.Index(i);
                    T key = keyArray[index];
                    _dictionary[key] = firstRowIndex + i;
                }
            }
        }

        public BitVector TryGetValues(DataBatch keys, out ArraySelector rightSideSelector)
        {
            Allocator.AllocateToSize(ref _returnedVector, keys.Count);
            Allocator.AllocateToSize(ref _returnedIndicesBuffer, keys.Count);

            _returnedVector.None();

            int countFound = 0;
            T[] keyArray = (T[])keys.Array;
            for (int i = 0; i < keys.Count; ++i)
            {
                int index = keys.Index(i);
                int foundAtIndex;
                if ((keys.IsNull != null && keys.IsNull[index]) || !_dictionary.TryGetValue(keyArray[index], out foundAtIndex))
                {
                    _returnedVector.Clear(i);
                }
                else
                {
                    _returnedVector.Set(i);
                    _returnedIndicesBuffer[countFound++] = foundAtIndex;
                }
            }

            // Write out the indices of the joined rows for each value found
            rightSideSelector = ArraySelector.Map(_returnedIndicesBuffer, countFound);

            // Return the vector of which input rows matched
            return _returnedVector;
        }
    }
}
