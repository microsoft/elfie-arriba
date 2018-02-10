// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Serialization;

using XForm.Data;
using XForm.IO.StreamProvider;

namespace XForm.IO
{
    internal class TabularColumn : IXColumn
    {
        private TabularFileReader _table;
        private String8[] _currentArray;

        public ColumnDetails ColumnDetails { get; private set; }

        public TabularColumn(TabularFileReader table, string columnName)
        {
            _table = table;
            ColumnDetails = new ColumnDetails(columnName, typeof(String8));
        }

        public void SetValues(String8[] currentArray)
        {
            _currentArray = currentArray;
        }

        public Func<XArray> CurrentGetter()
        {
            return () => XArray.All(_currentArray, _table.CurrentRowCount);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            return null;
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        public Type IndicesType => null;

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }
    }

    public class TabularFileReader : IXTable
    {
        private IStreamProvider _streamProvider;
        private string _filePath;

        private ITabularReader _reader;
        private TabularColumn[] _columns;

        private String8Block _block;
        private String8[][] _cells;

        public TabularFileReader(IStreamProvider streamProvider, string filePath)
        {
            _streamProvider = streamProvider;
            _filePath = filePath;
            _block = new String8Block();
            Reset();
        }

        public IReadOnlyList<IXColumn> Columns => _columns;
        public int CurrentRowCount { get; private set; }

        public void Reset()
        {
            _reader = TabularFactory.BuildReader(_streamProvider.OpenRead(_filePath), _filePath);

            _columns = new TabularColumn[_reader.Columns.Count];
            _cells = new String8[_reader.Columns.Count][];
            for (int i = 0; i < _reader.Columns.Count; ++i)
            {
                _columns[i] = new TabularColumn(this, _reader.Columns[i]);
            }
        }

        public int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Stop reading on cancellation
            if (cancellationToken.IsCancellationRequested) return 0;

            if (_cells[0] == null || _cells[0].Length < desiredCount)
            {
                for (int i = 0; i < _cells.Length; ++i)
                {
                    Allocator.AllocateToSize(ref _cells[i], desiredCount);
                }
            }

            //return _reader.NextRow();

            _block.Clear();
            CurrentRowCount = 0;

            while (_reader.NextRow())
            {
                for (int i = 0; i < _cells.Length; ++i)
                {
                    _cells[i][CurrentRowCount] = _block.GetCopy(_reader.Current(i).ToString8());
                }

                CurrentRowCount++;
                if (CurrentRowCount == desiredCount) break;
            }

            for (int i = 0; i < _columns.Length; ++i)
            {
                _columns[i].SetValues(_cells[i]);
            }

            return CurrentRowCount;
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
    }
}
