// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.Query;
using XForm.Types;

namespace XForm.IO
{
    internal class WriteCommandBuilder : IVerbBuilder
    {
        public string Verb => "write";
        public string Usage => "write {Table}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            string filePath = context.Parser.NextOutputTableName();
            if (filePath.StartsWith("Table\\", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".xform", StringComparison.OrdinalIgnoreCase))
            {
                return new BinaryTableWriter(source, context, filePath);
            }
            else
            {
                return new TabularFileWriter(source, context.StreamProvider, filePath);
            }
        }
    }

    public class BinaryTableWriter : XTableWrapper
    {
        private XDatabaseContext _xDatabaseContext;
        private string _tableRootPath;

        private Func<XArray>[] _getters;
        private XArray[] _currentArrays;
        private IColumnWriter[] _writers;

        private TableMetadata _metadata;

        public BinaryTableWriter(IXTable source, XDatabaseContext xDatabaseContext, string tableRootPath) : base(source)
        {
            _xDatabaseContext = xDatabaseContext;
            _tableRootPath = tableRootPath;

            int columnCount = source.Columns.Count;

            _metadata = new TableMetadata();
            _metadata.Query = xDatabaseContext.CurrentQuery;

            _getters = new Func<XArray>[columnCount];
            _currentArrays = new XArray[columnCount];

            // Subscribe to all of the columns
            for (int i = 0; i < columnCount; ++i)
            {
                _getters[i] = source.Columns[i].CurrentGetter();
            }
        }

        private void BuildWriters()
        {
            // Delete the previous table (if any) only once we've successfully gotten some rows to write
            _xDatabaseContext.StreamProvider.Delete(_tableRootPath);

            int columnCount = _source.Columns.Count;
            _writers = new IColumnWriter[columnCount];

            for (int i = 0; i < columnCount; ++i)
            {
                ColumnDetails column = _source.Columns[i].ColumnDetails;
                string columnPath = Path.Combine(_tableRootPath, column.Name);

                // Build a writer for each column
                _writers[i] = TypeProviderFactory.TryGetColumnWriter(_xDatabaseContext.StreamProvider, column.Type, columnPath);
                if (_writers[i] == null) throw new ArgumentException($"No writer or String8 converter for {column.Type.Name} was available. Could not build column writer.");

                // If the column was converted to String8, write String8 in the schema
                if (column.Type != typeof(String8) && _writers[i].WritingAsType == typeof(String8))
                {
                    column = column.ChangeType(typeof(String8));
                }

                _metadata.Schema.Add(column);
            }
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            if (_writers == null) BuildWriters();

            int count = _source.Next(desiredCount, cancellationToken);
            if (count == 0)
            {
                // Ensure Writers flush
                DisposeWriters();
                return 0;
            }

            // Get the next set of arrays (parallel might not be safe)
            for (int i = 0; i < _getters.Length; ++i)
            {
                _currentArrays[i] = _getters[i]();
            }

            // Write them out (Parallel safe)
            if (_xDatabaseContext.ForceSingleThreaded)
            {
                for (int i = 0; i < _getters.Length; ++i)
                {
                    _writers[i].Append(_currentArrays[i]);
                }
            }
            else
            {
                Parallel.For(0, _getters.Length, (i) =>
                {
                    _writers[i].Append(_currentArrays[i]);
                });
            }

            _metadata.RowCount += count;
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

                // Write the schema and query only if the table was valid
                if (_metadata.Schema.Count > 0)
                {
                    // Write table metadata for the completed table
                    TableMetadataSerializer.Write(_xDatabaseContext.StreamProvider, _tableRootPath, _metadata);

                    // On Dispose, tell the StreamProvider to publish the table
                    _xDatabaseContext.StreamProvider.Publish(_tableRootPath);
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeWriters();
        }
    }
}
