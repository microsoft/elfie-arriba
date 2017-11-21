//// Copyright (c) Microsoft. All rights reserved.
//// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using Microsoft.CodeAnalysis.Elfie.Model.Strings;
//using System;
//using System.Collections.Generic;

//using XForm.Data;
//using XForm.Extensions;
//using XForm.IO;

//namespace XForm.Commands
//{
//    public class Join : IDataBatchEnumerator
//    {
//        private IDataBatchEnumerator _source;

//        private IDataBatchEnumerator _rawJoinSource;
//        private Func<DataBatch> _joinToColumnGetter;

//        private ArrayEnumerator _cachedJoinSource;
//        private Dictionary<String8, int> _joinDictionary;

//        private List<ColumnDetails> _columns;
//        private List<int> _mappedColumnIndices;

//        private RowRemapper _mapper;
//        private int _joinFromColumnIndex;
//        private Func<DataBatch> _joinFromColumnGetter;
//        private int[] _currentJoinRowIndices;

//        public Join(IDataBatchEnumerator source, string joinFromColumn, IDataBatchEnumerator joinToSource, string joinToColumn, IEnumerable<string> addColumns)
//        {
//            _source = source;
//            _rawJoinSource = joinToSource;

//            // Request the Join From Column Getter
//            _joinFromColumnIndex = source.Columns.IndexOfColumn(joinFromColumn);
//            _joinFromColumnGetter = source.ColumnGetter(_joinFromColumnIndex);

//            // Request the Join To Column Getter
//            _joinToColumnGetter = joinToSource.ColumnGetter(joinToSource.Columns.IndexOfColumn(joinToColumn));

//            // All of the main source columns are passed through
//            _columns = new List<ColumnDetails>(source.Columns);

//            // Find and map the columns coming from the join
//            _mappedColumnIndices = new List<int>();
//            foreach(string columnName in addColumns)
//            {
//                int columnIndex = joinToSource.Columns.IndexOfColumn(columnName);
//                _columns.Add(joinToSource.Columns[columnIndex]);
//                _mappedColumnIndices.Add(columnIndex);
//            }

//            // Build a mapper to hold matching rows and remap source batches
//            _mapper = new RowRemapper();
//        }

//        public IReadOnlyList<ColumnDetails> Columns => _columns;

//        public Func<DataBatch> ColumnGetter(int columnIndex)
//        {
//            // Return shared getter for join column, if that was requested
//            if (columnIndex == _joinFromColumnIndex) return _joinFromColumnGetter;

//            // The first columns are from the source
//            if (columnIndex < _source.Columns.Count) return _source.ColumnGetter(columnIndex);

//            int joinColumnIndex = _mappedColumnIndices[columnIndex - _source.Columns.Count];

//            return () =>
//            {

//            }
//        }

//        public int Next(int desiredCount)
//        {
//            // If this is the first call, fully cache the JoinToSource and build a lookup Dictionary
//            if(_joinDictionary == null)
//            {
//                _joinDictionary = new Dictionary<String8, int>();
//                _cachedJoinSource = new ArrayEnumerator();

//            }

//            // Get the next rows from the source
//            int count = _source.Next(desiredCount);
//            DataBatch joinFromValues = _joinFromColumnGetter();
//            String8[] array = (String8[])joinFromValues.Array;

//            // Find the matching row index for each value
//            Allocator.AllocateToSize(ref _currentJoinRowIndices, count);
//            for (int i = 0; i < count; ++i)
//            {
//                String8 joinFromValue = array[joinFromValues.Index(i)];
//                _currentJoinRowIndices[i] = _joinDictionary[joinFromValue];
//            }

//            return count;
//        }

//        private void CacheJoinToTable()
//        {
//            _joinDictionary = new Dictionary<String8, int>();
//            _cachedJoinSource = new ArrayEnumerator();

//            while(_rawJoinSource.Next(10240) != 0)
//            {

//            }
//        }

//        public void Reset()
//        {
//            _source.Reset();
//        }

//        public void Dispose()
//        {
//            if (_source != null)
//            {
//                _source.Dispose();
//                _source = null;
//            }

//            if(_rawJoinSource != null)
//            {
//                _rawJoinSource.Dispose();
//                _rawJoinSource = null;
//            }
//        }
//    }
//}
