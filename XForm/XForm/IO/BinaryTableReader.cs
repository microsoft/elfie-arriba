// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using XForm.Data;
using XForm.Query;
using XForm.Types;

namespace XForm.IO
{
    internal class ReadCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "read" };
        public string Usage => "'read' [tableNameOrFilePath]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            if (source != null) throw new ArgumentException($"'read' must be the first stage in a pipeline.");
            string filePath = parser.NextTableName();
            if (filePath.EndsWith("xform"))
            {
                return new BinaryTableReader(filePath);
            }
            else
            {
                return new TabularFileReader(filePath);
            }
        }
    }

    public class BinaryTableReader : IDataBatchList
    {
        private string _tableRootPath;
        private List<ColumnDetails> _columns;
        private IColumnReader[] _readers;
        private int _totalCount;

        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public BinaryTableReader(string tableRootPath)
        {
            _tableRootPath = tableRootPath;
            _columns = SchemaSerializer.Read(_tableRootPath);
            _readers = new IColumnReader[_columns.Count];
            Reset();
        }

        public int Count => _totalCount;

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            if (_readers[columnIndex] == null)
            {
                ColumnDetails column = Columns[columnIndex];
                _readers[columnIndex] = TypeProviderFactory.Get(column.Type).BinaryReader(Path.Combine(_tableRootPath, column.Name));
            }

            return () => _readers[columnIndex].Read(_currentSelector);
        }

        public int Next(int desiredCount)
        {
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_totalCount, desiredCount);
            _currentSelector = _currentEnumerateSelector;
            return _currentEnumerateSelector.Count;
        }

        public void Get(ArraySelector selector)
        {
            _currentSelector = selector;
        }

        public void Reset()
        {
            // Get the first reader in order to get the row count
            Func<DataBatch> unused = ColumnGetter(0);
            _totalCount = _readers[0].Count;

            // Mark our current position (nothing read yet)
            _currentEnumerateSelector = ArraySelector.All(_totalCount).Slice(0, 0);
        }

        public void Dispose()
        {
            if (_readers != null)
            {
                foreach (IColumnReader reader in _readers)
                {
                    if (reader != null) reader.Dispose();
                }

                _readers = null;
            }
        }
    }
}
