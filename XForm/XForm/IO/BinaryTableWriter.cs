// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
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
                return BinaryTableWriter.Build(source, context, filePath);
            }
            else
            {
                return new TabularFileWriter(source, context.StreamProvider, filePath);
            }
        }
    }

    public class PartitionedBinaryTableWriter : XTableWrapper
    {
        private XDatabaseContext _xDatabaseContext;
        private string _tableRootPath;

        private BinaryTableWriter _partitionWriter;
        private TableMetadata _metadata;

        private Func<XArray>[] _getters;
        private XArray[] _currentArrays;

        public PartitionedBinaryTableWriter(IXTable source, XDatabaseContext xDatabaseContext, string tableRootPath) : base(source)
        {
            _xDatabaseContext = xDatabaseContext;
            _tableRootPath = tableRootPath;

            _metadata = new TableMetadata();
            _metadata.Query = xDatabaseContext.CurrentQuery;

            _currentArrays = new XArray[source.Columns.Count];
            _getters = _source.Columns.Select((col) => col.CurrentGetter()).ToArray();
        }

        private void NextPartitionWriter()
        {
            if (_partitionWriter != null) _partitionWriter.Dispose();

            string nextPartitionId = _metadata.Partitions.Count.ToString();
            _partitionWriter = BinaryTableWriter.BuildPartition(_source, _xDatabaseContext, Path.Combine(_tableRootPath, nextPartitionId));
            _metadata.Partitions.Add(nextPartitionId);
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            if (_partitionWriter == null) NextPartitionWriter();

            int countRetrieved = _source.Next(desiredCount, cancellationToken);
            if (countRetrieved == 0)
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

            int countLeft = countRetrieved;
            while (true)
            {
                // Write as many rows as possible
                int countWritten = _partitionWriter.Write(_currentArrays);
                _metadata.RowCount += countWritten;

                // If all rows fit, stop
                if (countWritten == countLeft) break;

                // Otherwise, open a new partition and continue writing
                NextPartitionWriter();

                // Slice off the remaining rows to write to the next partition
                countLeft -= countWritten;
                for (int i = 0; i < _getters.Length; ++i)
                {
                    _currentArrays[i] = _currentArrays[i].Slice(countWritten, countRetrieved);
                }
            }

            return countRetrieved;
        }

        public override void Reset()
        {
            _source.Reset();
            DisposeWriters();
        }

        private void DisposeWriters()
        {
            if (_partitionWriter != null)
            {
                _metadata.Schema = _partitionWriter.Metadata.Schema;

                _partitionWriter.Dispose();
                _partitionWriter = null;
            }

            // Write the schema and query only if the table was valid
            if (_metadata.RowCount > 0)
            {
                // Write table metadata for the partition set
                TableMetadataSerializer.Write(_xDatabaseContext.StreamProvider, _tableRootPath, _metadata);

                // On Dispose, tell the StreamProvider to publish the table
                _xDatabaseContext.StreamProvider.Publish(_tableRootPath);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeWriters();
        }
    }

    public class BinaryTableWriter : XTableWrapper
    {
        /// <summary>
        ///  File Size limit for each individual column file.
        ///  2GB because XForm uses integers for row indices and to seek in streams, and so XForm can allocate arrays to hold cached files whole.
        ///  This may be lowered later to accomodate ideal sizes for cloud storage.
        /// </summary>
        public static long ColumnFileSizeLimit = (long)int.MaxValue;

        public TableMetadata Metadata { get; private set; }
        private XDatabaseContext _xDatabaseContext;
        private string _tableRootPath;
        private IColumnWriter[] _writers;

        private Func<XArray>[] _getters;
        private XArray[] _currentArrays;

        private BinaryTableWriter(IXTable source, XDatabaseContext xDatabaseContext, string tableRootPath) : base(source)
        {
            _xDatabaseContext = xDatabaseContext;
            _tableRootPath = tableRootPath;

            Metadata = new TableMetadata();
            Metadata.Query = xDatabaseContext.CurrentQuery;

            // Defer subscribing to columns; if wrapped in a PartitionedBinaryTableWriter, it will take that over.
        }

        /// <summary>
        ///  Build constructs an IXTable to write a (potentially multi-partition) Binary Table.
        /// </summary>
        /// <param name="source">IXTable to write data from</param>
        /// <param name="xDatabaseContext">XDatabaseContext for database</param>
        /// <param name="tableRootPath">Table Path (Table\[Name]\Full\[UtcDateTime])</param>
        /// <returns>IXTable to enumerate to write the table and pass through rows</returns>
        public static IXTable Build(IXTable source, XDatabaseContext xDatabaseContext, string tableRootPath)
        {
            //return new BinaryTableWriter(source, xDatabaseContext, tableRootPath);

            // Enable Partitioning
            return new PartitionedBinaryTableWriter(source, xDatabaseContext, tableRootPath);
        }

        internal static BinaryTableWriter BuildPartition(IXTable source, XDatabaseContext xDatabaseContext, string tableRootPath)
        {
            return new BinaryTableWriter(source, xDatabaseContext, tableRootPath);
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

                Metadata.Schema.Add(column);
            }
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Subscribe to all columns (on first call, just before Next())
            if(_currentArrays == null)
            {
                _currentArrays = new XArray[_source.Columns.Count];
                _getters = _source.Columns.Select((col) => col.CurrentGetter()).ToArray();
            }

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

            return Write(_currentArrays);
        }

        public int Write(XArray[] arrays)
        {
            if (_writers == null) BuildWriters();

            int countWritten = arrays[0].Count;

            // Try to write all rows
            bool succeeded = WriteAll(arrays);

            if (!succeeded)
            {
                // If the partition is full, write single rows until full
                countWritten = WriteRowByRow(arrays);
            }

            Metadata.RowCount += countWritten;
            return countWritten;
        }

        private bool WriteAll(XArray[] arrays)
        {
            // Validate partition isn't full
            bool canAppend = true;
            for (int i = 0; i < _writers.Length; ++i)
            {
                canAppend &= _writers[i].CanAppend(arrays[i]);
                if (!canAppend) return false;
            }

            // Write all rows if not
            if (_xDatabaseContext.ForceSingleThreaded)
            {
                for (int i = 0; i < _writers.Length; ++i)
                {
                    _writers[i].Append(arrays[i]);
                }
            }
            else
            {
                Parallel.For(0, _writers.Length, (i) =>
                {
                    _writers[i].Append(arrays[i]);
                });
            }

            return true;
        }

        private int WriteRowByRow(XArray[] arrays)
        {
            XArray[] currentSingleRow = new XArray[_writers.Length];
            int countWritten;

            for (countWritten = 0; countWritten < arrays[0].Count; ++countWritten)
            {
                for (int i = 0; i < _writers.Length; ++i)
                {
                    currentSingleRow[i] = arrays[i].Slice(countWritten, countWritten + 1);
                }

                if (!WriteAll(currentSingleRow)) break;
            }

            return countWritten;
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
                    if(writer != null) writer.Dispose();
                }

                _writers = null;

                // Write the schema and query only if the table was valid
                if (Metadata.RowCount > 0)
                {
                    // Write table metadata for the completed table
                    TableMetadataSerializer.Write(_xDatabaseContext.StreamProvider, _tableRootPath, Metadata);

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
