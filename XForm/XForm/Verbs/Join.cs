// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

using XForm.Columns;
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

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            // NOTE: Not parallel to build only one copy of the JoinTo table.

            string sourceColumnName = context.Parser.NextColumnName(source);
            IXTable joinToSource = context.Parser.NextTableSource();
            string joinToColumn = context.Parser.NextColumnName(joinToSource);

            return new Join(
                source,
                sourceColumnName,
                joinToSource,
                joinToColumn,
                (string)context.Parser.NextLiteralValue());
        }
    }

    public class Join : IXTable
    {
        private IXTable _source;
        private IXColumn[] _columns;
        private SeekedColumn[] _rightSideColumns;

        private Type _joinColumnType;
        private Func<XArray> _joinFromColumnGetter;

        private IXTable _joinToSource;
        private IXColumn _joinToColumn;
        private Func<ArraySelector, XArray> _joinToSeekGetter;

        private IJoinDictionary _joinDictionary;

        private RowRemapper _sourceJoinedRowsFilter;
        private ArraySelector _currentRightSideSelector;

        public Join(IXTable source, string joinFromColumn, IXTable joinToSource, string joinToColumn, string joinSidePrefix)
        {
            if (source == null) throw new ArgumentNullException("source");

            _source = source;
            _joinToSource = joinToSource;

            // Request the JoinFromColumn Getter
            IXColumn joinFrom = source.Columns.Find(joinFromColumn);
            _joinColumnType = joinFrom.ColumnDetails.Type;
            _joinFromColumnGetter = joinFrom.CurrentGetter();

            // Request the JoinToColumn Reader (we'll need it cached)
            _joinToColumn = _joinToSource.Columns.Find(joinToColumn);
            Type joinToColumnType = _joinToColumn.ColumnDetails.Type;
            if (joinToColumnType != _joinColumnType) throw new ArgumentException($"Join requires columns of matching types; join from {_joinColumnType.Name} to {joinToColumnType.Name} not supported.");
            _joinToSeekGetter = _joinToColumn.SeekGetter();

            // Build a remapper for left side columns
            _sourceJoinedRowsFilter = new RowRemapper();

            // Build column wrappers
            _columns = new IXColumn[source.Columns.Count + joinToSource.Columns.Count];
            _rightSideColumns = new SeekedColumn[joinToSource.Columns.Count];

            // Left Side columns are filtered to rows that joined (inner join)
            for (int i = 0; i < source.Columns.Count; ++i)
            {
                _columns[i] = new RemappedColumn(source.Columns[i], _sourceJoinedRowsFilter);
            }

            // Right side columns are seeked to the right side matching rows
            for (int i = 0; i < joinToSource.Columns.Count; ++i)
            {
                SeekedColumn column = new SeekedColumn(RenamedColumn.Build(joinToSource.Columns[i], joinSidePrefix + joinToSource.Columns[i].ColumnDetails.Name));
                _rightSideColumns[i] = column;
                _columns[i + source.Columns.Count] = column;
            }
        }

        public IReadOnlyList<IXColumn> Columns => _columns;

        public int CurrentRowCount { get; private set; }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // If this is the first call, fully cache the JoinToSource and build a lookup Dictionary
            if (_joinDictionary == null) BuildJoinDictionary(cancellationToken);

            BitVector matchedRows = null;

            while (true)
            {
                // Get the next rows from the source
                int count = _source.Next(desiredCount, cancellationToken);
                if (count == 0)
                {
                    CurrentRowCount = 0;
                    return 0;
                }

                // Get values to join from
                XArray joinFromValues = _joinFromColumnGetter();

                // Find which rows matched and to what right-side row indices
                matchedRows = _joinDictionary.TryGetValues(joinFromValues, out _currentRightSideSelector);

                if (_currentRightSideSelector.Count > 0) break;
            }

            // Filter left-side rows to the matches (inner join)
            _sourceJoinedRowsFilter.SetMatches(matchedRows);

            // Seek right-side rows to the matches
            for (int i = 0; i < _rightSideColumns.Length; ++i)
            {
                _rightSideColumns[i].Set(_currentRightSideSelector);
            }

            CurrentRowCount = _currentRightSideSelector.Count;
            return _currentRightSideSelector.Count;
        }

        private void BuildJoinDictionary(CancellationToken cancellationToken)
        {
            // Validate the RHS is a seekable table (only on build, so that Suggest doesn't fail)
            ISeekableXTable joinToSource = _joinToSource as ISeekableXTable;
            if (joinToSource == null) throw new ArgumentException($"Join requires a single built Binary Table as the right side table.");

            XArray allJoinToValues = _joinToSeekGetter(ArraySelector.All(joinToSource.Count));
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

            if (_joinToSource != null)
            {
                _joinToSource.Dispose();
                _joinToSource = null;
            }
        }
    }

    public interface IJoinDictionary
    {
        void Add(XArray keys, int firstRowIndex);
        BitVector TryGetValues(XArray keys, out ArraySelector rightSideSelector);
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

        public void Add(XArray keys, int firstRowIndex)
        {
            T[] keyArray = (T[])keys.Array;

            if (_valueCopier != null || keys.HasNulls)
            {
                for (int i = 0; i < keys.Count; ++i)
                {
                    int index = keys.Index(i);
                    if (keys.HasNulls && keys.NullRows[index]) continue;
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

        public BitVector TryGetValues(XArray keys, out ArraySelector rightSideSelector)
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
                if ((keys.HasNulls && keys.NullRows[index]) || !_dictionary.TryGetValue(keyArray[index], out foundAtIndex))
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
