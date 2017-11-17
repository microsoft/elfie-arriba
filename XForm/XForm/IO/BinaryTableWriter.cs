// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Transforms;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryTableWriter : DataBatchEnumeratorWrapper
    {
        private string _tableRootPath;
        private Func<DataBatch>[] _innerGetters;
        private Func<DataBatch>[] _getters;
        private IColumnWriter[] _writers;

        public BinaryTableWriter(IDataBatchEnumerator source, string tableRootPath) : base(source)
        {
            _tableRootPath = tableRootPath;
            DirectoryIO.DeleteAllContents(tableRootPath);
            Directory.CreateDirectory(tableRootPath);

            int columnCount = source.Columns.Count;

            List<ColumnDetails> columnSchemaToWrite = new List<ColumnDetails>();

            _innerGetters = new Func<DataBatch>[columnCount];
            _getters = new Func<DataBatch>[columnCount];

            for (int i = 0; i < columnCount; ++i)
            {
                ColumnDetails column = source.Columns[i];

                Func<DataBatch> directGetter = source.ColumnGetter(i);
                Func<DataBatch> outputTypeGetter = directGetter;

                // Build a direct writer for the column type, if available
                ITypeProvider columnTypeProvider = TypeProviderFactory.TryGet(column.Type);

                // If the column type doesn't have a provider or writer, convert to String8 and write that
                if (columnTypeProvider == null)
                {
                    Func<DataBatch, DataBatch> converter = TypeConverterFactory.GetConverter(column.Type, typeof(String8), null, false);
                    outputTypeGetter = () => converter(directGetter());
                    column = column.ChangeType(typeof(String8));
                }

                columnSchemaToWrite.Add(column);
                _innerGetters[i] = directGetter;
                _getters[i] = outputTypeGetter;
            }

            SchemaSerializer.Write(_tableRootPath, columnSchemaToWrite);
        }

        private void BuildWriters()
        {
            int columnCount = _source.Columns.Count;
            _writers = new IColumnWriter[columnCount];

            for (int i = 0; i < columnCount; ++i)
            {
                ColumnDetails column = _source.Columns[i];

                IColumnWriter writer = null;

                // Build a direct writer for the column type, if available
                ITypeProvider columnTypeProvider = TypeProviderFactory.TryGet(column.Type);
                if (columnTypeProvider != null) writer = columnTypeProvider.BinaryWriter(Path.Combine(_tableRootPath, _source.Columns[i].Name));

                // If the column type doesn't have a provider or writer, convert to String8 and write that
                if (writer == null)
                {
                    Func<DataBatch, DataBatch> converter = TypeConverterFactory.GetConverter(column.Type, typeof(String8), null, false);
                    writer = TypeProviderFactory.TryGet(typeof(String8)).BinaryWriter(Path.Combine(_tableRootPath, _source.Columns[i].Name));
                    column = column.ChangeType(typeof(String8));
                }

                _writers[i] = writer;
            }
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _innerGetters[columnIndex];
        }

        public override int Next(int desiredCount)
        {
            if (_writers == null) BuildWriters();

            int count = _source.Next(desiredCount);
            if (count == 0)
            {
                // Ensure Writers flush
                DisposeWriters();
                return 0;
            }

            for (int i = 0; i < _getters.Length; ++i)
            {
                _writers[i].Append(_getters[i]());
            }

            return count;
        }

        public override void Reset()
        {
            _source.Reset();
            DisposeWriters();
        }

        private void DisposeWriters()
        {
            if (_writers != null)
            {
                foreach (IColumnWriter writer in _writers)
                {
                    writer.Dispose();
                }

                _writers = null;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeWriters();
        }
    }
}
