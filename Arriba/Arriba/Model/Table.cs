// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Arriba.Extensions;
using Arriba.Model.Column;
using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  A Table represents a set of items in Arriba, containing one or more columns with values.
    /// </summary>
    public class Table : ITable, IDisposable
    {
        public bool RunParallel { get; set; }
        public string Name;

        private ReaderWriterLockSlim _locker;
        private List<Partition> _partitions;
        private ColumnAliasCorrector _columnAliasCorrector;
        private Tuple<Type, IComputePartition> _splitter;

        private byte _partitionBits;

        public ParallelOptions ParallelOptions { get; set; }

        /// <summary>
        /// Creates a new table large enough to hold the number of items specified
        /// </summary>
        /// <param name="tableName">name of the table</param>
        /// <param name="requiredItemCount">number of items the table is required to hold (it may be capable of holding more); this will dictate the partition count</param>
        public Table(string tableName, long requiredItemCount) : this()
        {
            this.Name = tableName;

            // Pad the min row count by 5% to account for imperfect distribution of items
            // based on the hashing algorithm.
            long paddedMinItemCount = (long)(requiredItemCount * 1.05);

            // Translate the item limit to a number of partition bits (64k items per partition)
            _partitionBits = (byte)Math.Max(Math.Ceiling(Math.Log(requiredItemCount, 2)) - 16, 0);

            // Create the partitions
            PartitionMask[] maskSet = PartitionMask.BuildSet(_partitionBits);
            _partitions.Clear();
            for (int i = 0; i < maskSet.Length; ++i)
            {
                Partition p = new Partition(maskSet[i]);
                _partitions.Add(p);
            }
        }

        /// <summary>
        ///  Serialization-only constructor
        /// </summary>
        public Table()
        {
            this.RunParallel = true;
            this.ParallelOptions = new ParallelOptions();

            this.Name = String.Empty;

            _locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _partitions = new List<Partition>();
            _partitions.Add(new Partition(PartitionMask.All));

            _columnAliasCorrector = new ColumnAliasCorrector(this);
        }

        /// <summary>
        ///  Return the total number of items in this Table across all partitions
        /// </summary>
        public uint Count
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    uint totalItemCount = 0;

                    foreach (Partition p in _partitions)
                    {
                        totalItemCount += p.Count;
                    }

                    return totalItemCount;
                }
                finally
                {
                    _locker.ExitReadLock();
                }
            }
        }

        public int PartitionCount
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    return _partitions.Count;
                }
                finally
                {
                    _locker.ExitReadLock();
                }
            }
        }

        protected IReadOnlyList<Partition> GetPartitions()
        {
            return _partitions.AsReadOnly();
        }

        #region Column Operations
        public ColumnDetails IDColumn
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    return _partitions[0].IDColumn;
                }
                finally
                {
                    _locker.ExitReadLock();
                }
            }
        }

        public ICollection<ColumnDetails> ColumnDetails
        {
            get
            {
                _locker.EnterReadLock();
                try
                {
                    return _partitions[0].ColumnDetails;
                }
                finally
                {
                    _locker.ExitReadLock();
                }
            }
        }

        public ColumnDetails GetDetails(string columnName)
        {
            _locker.EnterReadLock();
            try
            {
                return _partitions[0].DetailsByColumn[columnName];
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        public Type GetColumnType(string columnName)
        {
            _locker.EnterReadLock();

            try
            {
                IUntypedColumn column;
                if (_partitions[0].Columns.TryGetValue(columnName, out column))
                {
                    return column.ColumnType;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        ///  Add a new column with the given type descriptor and default.
        ///  Columns must be added before values can be set on them.
        /// </summary>
        /// <param name="details">Details of the column to add</param>
        public void AddColumn(ColumnDetails details)
        {
            AddColumn(details, 0);
        }

        /// <summary>
        ///  Add a new column with the given type descriptor and default.
        ///  Columns must be added before values can be set on them.
        /// </summary>
        /// <param name="details">Details of the column to add</param>
        public void AddColumn(ColumnDetails details, ushort initialCapacity)
        {
            _locker.EnterWriteLock();
            try
            {
                foreach (Partition p in _partitions)
                {
                    p.AddColumn(details, initialCapacity);
                }

                // Reset column alias mappings
                _columnAliasCorrector.SetMappings(this);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds a new set of columns for the given column descriptors. 
        /// </summary>
        /// <param name="columns">Columns to add.</param>
        public void AddColumns(IEnumerable<ColumnDetails> columns)
        {
            AddColumns(columns, 0);
        }

        /// <summary>
        /// Adds a new set of columns for the given column descriptors. 
        /// </summary>
        /// <remarks>
        /// Columns can grow beyond initialCapacity up to ushort.MaxValue in size therefore it is just a suggestion for initial column creation
        /// </remarks>
        /// <param name="columns">Columns to add.</param>
        /// <param name="initialCapacity">initial capacity (not size) to create the columns at</param>
        public void AddColumns(IEnumerable<ColumnDetails> columns, ushort initialCapacity)
        {
            _locker.EnterWriteLock();
            try
            {
                // Ensure if there is a primary key in the set; add this column first
                foreach (var col in columns.OrderBy(c => c.IsPrimaryKey))
                {
                    foreach (Partition p in _partitions)
                    {
                        p.AddColumn(col, initialCapacity);
                    }
                }

                // Reset column alias mappings
                _columnAliasCorrector.SetMappings(this);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        /// <summary>
        ///  Change the type of a column to a new type. Values are copied from the existing
        ///  column to the new one, if value conversion is possible.
        /// </summary>
        /// <param name="details">Details with existing name and new other details</param>
        public void AlterColumn(ColumnDetails details)
        {
            _locker.EnterWriteLock();
            try
            {
                if (this.RunParallel)
                {
                    Parallel.ForEach(_partitions, (p) =>
                    {
                        p.AlterColumn(details);
                    });
                }
                else
                {
                    foreach (Partition p in _partitions)
                    {
                        p.AlterColumn(details);
                    }
                }

                // Reset column alias mappings
                _columnAliasCorrector.SetMappings(this);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        /// <summary>
        ///  Remove the column with the given name.
        /// </summary>
        /// <param name="columnName">Name of column to remove.</param>
        public void RemoveColumn(string columnName)
        {
            _locker.EnterWriteLock();
            try
            {
                foreach (Partition p in _partitions)
                {
                    p.RemoveColumn(columnName);
                }

                // Reset column alias mappings
                _columnAliasCorrector.SetMappings(this);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
        #endregion

        #region AddOrUpdate (insert, update)
        public void AddColumnsFromBlock(DataBlock.ReadOnlyDataBlock values)
        {
            List<ColumnDetails> columnsToAdd = new List<ColumnDetails>();

            // Find the ID column
            //  [The existing one, or one marked as primary key on the block, or one ending with 'ID', or the first column]
            ColumnDetails idColumn = _partitions[0].IDColumn
                ?? values.Columns.FirstOrDefault((cd) => cd.IsPrimaryKey)
                ?? values.Columns.FirstOrDefault((cd) => cd.Name.EndsWith("ID"))
                ?? values.Columns.FirstOrDefault();

            // Mark the ID column
            idColumn.IsPrimaryKey = true;

            for (int columnIndex = 0; columnIndex < values.ColumnCount; ++columnIndex)
            {
                ColumnDetails details = values.Columns[columnIndex];
                bool hasNonDefaultValues = false;

                // If this column was already added, no need to scan these values
                if (_partitions[0].ContainsColumn(details.Name)) continue;

                // Figure out the column type. Did the DataBlock provide one?
                Type determinedType = ColumnFactory.GetTypeFromTypeString(details.Type);

                // If not, is the DataBlock column array typed?
                determinedType = determinedType ?? values.GetTypeForColumn(columnIndex);
                if (determinedType == typeof(object) || determinedType == typeof(Value)) determinedType = null;

                // Get the column default, if provided, or the default for the type, if provided
                object columnDefault = details.Default;
                if (columnDefault == null && determinedType != null)
                {
                    columnDefault = ColumnFactory.GetDefaultValueFromTypeString(determinedType.Name);
                }

                Type inferredType = null;
                Value v = Value.Create(null);
                DateTime defaultUtc = default(DateTime).ToUniversalTime();

                for (int rowIndex = 0; rowIndex < values.RowCount; ++rowIndex)
                {
                    object value = values[rowIndex, columnIndex];

                    // Identify the best type for all block values, if no type was already determined
                    if (determinedType == null)
                    {
                        v.Assign(value);
                        Type newBestType = v.BestType(inferredType);

                        // If the type has changed, get an updated default value
                        if (newBestType != determinedType)
                        {
                            columnDefault = ColumnFactory.GetDefaultValueFromTypeString(newBestType.Name);
                            inferredType = newBestType;
                        }
                    }

                    // Track whether any non-default values were seen [could be raw types or Value wrapper]
                    if (hasNonDefaultValues == false && value != null && !value.Equals("") && !value.Equals(defaultUtc))
                    {
                        if (columnDefault == null || value.Equals(columnDefault) == false)
                        {
                            hasNonDefaultValues = true;
                        }
                    }
                }

                // Set the column type
                if (String.IsNullOrEmpty(details.Type) || details.Type.Equals(Arriba.Model.Column.ColumnDetails.UnknownType))
                {
                    details.Type = ColumnFactory.GetCanonicalTypeName(determinedType ?? inferredType ?? typeof(string));
                }

                // Add the column if it had any non-default values (and didn't already exist)
                if (hasNonDefaultValues || details.IsPrimaryKey) columnsToAdd.Add(details);
            }

            // Add the discovered columns. If any names match existing columns they'll be merged properly in Partition.AddColumn.
            AddColumns(columnsToAdd);
        }

        /// <summary>
        ///  Add or Update the given items with the given values. The ID column must be passed
        ///  and must be the first column. If an ID is not known, the item will be added.
        ///  For each item, the value for each column is set to the provided values.
        /// </summary>
        /// <param name="values">Set of Columns and values to add or update</param>
        public void AddOrUpdate(DataBlock.ReadOnlyDataBlock values)
        {
            AddOrUpdate(values, new AddOrUpdateOptions());
        }

        /// <summary>
        ///  Add or Update the given items with the given values. The ID column must be passed
        ///  and must be the first column. If an ID is not known, the item will be added.
        ///  For each item, the value for each column is set to the provided values.
        /// </summary>
        /// <param name="values">Set of Columns and values to add or update</param>
        /// <param name="options">Options to adjust behavior of AddOrUpdate</param>
        public void AddOrUpdate(DataBlock.ReadOnlyDataBlock values, AddOrUpdateOptions options)
        {
            _locker.EnterWriteLock();
            try
            {
                // Add columns from data, if this is the first data and columns weren't predefined
                if (options.AddMissingColumns) AddColumnsFromBlock(values);

                ColumnDetails idColumn = _partitions[0].IDColumn;
                if (idColumn == null) throw new ArribaException("Items cannot be added to this Table because it does not yet have an ID column defined. Call AddColumn with exactly one column with 'IsPrimaryKey' true and then items may be added.");
                int idColumnIndex = values.IndexOfColumn(idColumn.Name);
                if (idColumnIndex == -1) throw new ArribaException(StringExtensions.Format("AddOrUpdates must be passed the ID column, '{0}', in order to tell which items to update.", idColumn.Name));

                // Verify all passed columns exist (if not adding them)
                if (options.AddMissingColumns == false)
                {
                    foreach (ColumnDetails column in values.Columns)
                    {
                        ColumnDetails foundColumn;
                        if (!_partitions[0].DetailsByColumn.TryGetValue(column.Name, out foundColumn))
                        {
                            throw new ArribaException(StringExtensions.Format("AddOrUpdate failed because values were passed for column '{0}', which is not in the table. Use AddColumn to add all columns first or ensure the first block added to the Table has all desired columns.", column.Name));
                        }
                    }
                }

                // Non-Parallel Implementation
                if (_partitions.Count == 1)
                {
                    _partitions[0].AddOrUpdate(values, options);
                    return;
                }

                // Determine the partition each item should go to
                int[] partitionIds;
                TargetPartitionInfo[] partitionInfo;
                Type idColumnArrayType = values.GetTypeForColumn(idColumnIndex);
                if (_splitter == null || _splitter.Item2 == null || _splitter.Item1 != idColumnArrayType)
                {
                    IComputePartition splitter = NativeContainer.CreateTypedInstance<IComputePartition>(typeof(ComputePartitionHelper<>), idColumnArrayType);
                    _splitter = Tuple.Create(idColumnArrayType, splitter);
                }
                _splitter.Item2.ComputePartition(this, values, idColumnIndex, out partitionIds, out partitionInfo);

                // Sort/group the incoming items by paritition and then by index to ensure they 
                // are processed in the order they were presented in the input ReadOnlyDataBlock
                int[] sortOrder = new int[values.RowCount];
                for (int i = 0; i < values.RowCount; ++i)
                {
                    int p = partitionIds[i];
                    int startIndex = partitionInfo[p].StartIndex + partitionInfo[p].Count;
                    sortOrder[startIndex] = i;
                    partitionInfo[p].Count++;
                }

                Action<Tuple<int, int>, ParallelLoopState> forBody =
                    delegate (Tuple<int, int> range, ParallelLoopState unused)
                    {
                        for (int p = range.Item1; p < range.Item2; ++p)
                        {
                            int startIndex = partitionInfo[p].StartIndex;
                            int length = partitionInfo[p].Count;
                            DataBlock.ReadOnlyDataBlock partitionValues = values.ProjectChain(sortOrder, startIndex, length);
                            _partitions[p].AddOrUpdate(partitionValues, options);
                        }
                    };

                // In parallel, each partition will add items which belong to it
                if (this.RunParallel)
                {
                    var rangePartitioner = Partitioner.Create(0, _partitions.Count);
                    Parallel.ForEach(rangePartitioner, this.ParallelOptions, forBody);
                }
                else
                {
                    var range = Tuple.Create(0, _partitions.Count);
                    forBody(range, null);
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        private struct TargetPartitionInfo
        {
            public int StartIndex;
            public int Count;
        }
        #endregion

        #region Delete
        /// <summary>
        ///  Delete items from this Table which meet the provided criteria.
        /// </summary>
        /// <param name="where">Expression matching items to delete</param>
        /// <returns>Result including the number deleted</returns>
        public DeleteResult Delete(IExpression where)
        {
            _locker.EnterWriteLock();
            try
            {
                // Correct query
                where = _columnAliasCorrector.Correct(where);

                if (_partitions.Count == 1) return _partitions[0].Delete(where);

                DeleteResult[] deletionsPerPartition = new DeleteResult[_partitions.Count];

                if (this.RunParallel)
                {
                    Parallel.For(0, _partitions.Count, (i) =>
                    {
                        deletionsPerPartition[i] = _partitions[i].Delete(where);
                    });
                }
                else
                {
                    for (int i = 0; i < _partitions.Count; ++i)
                    {
                        deletionsPerPartition[i] = _partitions[i].Delete(where);
                    }
                }

                DeleteResult mergedResult = new DeleteResult();
                for (int i = 0; i < deletionsPerPartition.Length; ++i)
                {
                    mergedResult.Count += deletionsPerPartition[i].Count;
                    mergedResult.Details.Merge(deletionsPerPartition[i].Details);
                }

                return mergedResult;
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
        #endregion

        #region Query
        /// <summary>
        ///  Run the provided query and return a result across this ITable.
        /// </summary>
        /// <typeparam name="T">Type of result to return</typeparam>
        /// <param name="query">Query to run</param>
        /// <returns>Result for Query across this ITable</returns>
        public T Query<T>(IQuery<T> query)
        {
            _locker.EnterReadLock();
            try
            {
                Stopwatch w = Stopwatch.StartNew();

                // Notify query
                query.OnBeforeQuery(this);

                // Run all correctors
                query.Correct(_columnAliasCorrector);

                // Non-Parallel implementation
                if (_partitions.Count == 1 && query.RequireMerge == false)
                {
                    return _partitions[0].Query(query);
                }

                // Determine the aggregate value per partition
                ObjectCache<T> mergePool = new ObjectCache<T>(null);
                if (this.RunParallel)
                {
                    Parallel.For(0, _partitions.Count, (i) =>
                    {
                        T partitionResult = _partitions[i].Query(query);

                        // Merge results in pairs and put back each merged result to
                        // a pool to be merged with a future result.  This allows merging
                        // to be a primarily parallel operation.  A final merge of the oustanding
                        // items in the pool will be done afterwards.
                        T mergeResult;
                        bool gotFromPool = mergePool.TryGet(out mergeResult);
                        if (gotFromPool)
                        {
                            T afterMergeResult = query.Merge(new T[] { partitionResult, mergeResult });
                            mergePool.Put(afterMergeResult);
                        }
                        else
                        {
                            mergePool.Put(partitionResult);
                        }
                    });
                }
                else
                {
                    for (int i = 0; i < _partitions.Count; ++i)
                    {
                        T partitionResult = _partitions[i].Query(query);
                        mergePool.Put(partitionResult);
                    }
                }

                // Merge the results
                T mergedResult = query.Merge(mergePool.ClearAndReturnAllItems());
                if (mergedResult is IBaseResult) ((IBaseResult)(object)mergedResult).Runtime = w.Elapsed;

                return mergedResult;
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }
        #endregion

        #region Split
        private interface IComputePartition
        {
            void ComputePartition(Table table, DataBlock.ReadOnlyDataBlock values, int idColumnIndex, out int[] partitionIds, out TargetPartitionInfo[] partitionInfo);
        }

        private class ComputePartitionHelper<T> : IComputePartition
        {
            /// <summary>
            /// Computes the target partition for each item in the ReadOnlyDataBlock
            /// </summary>
            /// <param name="table">Table where values will be added</param>
            /// <param name="values">DataBlock containing values to be added to the table</param>
            /// <param name="idColumnIndex">Index of the id column</param>
            /// <param name="partitionIds">[Out] array of the partition ids for each element</param>
            public void ComputePartition(Table table, DataBlock.ReadOnlyDataBlock values, int idColumnIndex, out int[] partitionIds, out TargetPartitionInfo[] partitionInfo)
            {
                int rowCount = values.RowCount;

                // TODO: [danny chen] it would be nice if I could get rid of this tunneling of GetColumn
                // from the ReadOnlyDataBlock (and avoid the special casing for non-projected blocks)
                // but I can't see a way to allow strongly types random access without a bunch of work
                // incurred on each access (fetch, cast the array).
                T[] idColumn = (T[])values.GetColumn(idColumnIndex);

                int[] localPartitionIds = new int[rowCount];
                TargetPartitionInfo[] localPartitionInfo = new TargetPartitionInfo[table.PartitionCount];

                var rangePartitioner = Partitioner.Create(0, rowCount);
                Parallel.ForEach(rangePartitioner,
                    delegate (Tuple<int, int> range, ParallelLoopState unused)
                    {
                        ValueTypeReference<T> vtr = new ValueTypeReference<T>();
                        Value v = Value.Create(null);
                        for (int i = range.Item1; i < range.Item2; ++i)
                        {
                            // Hash the ID for each item and compute the partition that the item belongs to
                            vtr.Value = idColumn[i];
                            v.Assign(vtr);
                            int idHash = v.GetHashCode();
                            int partitionId = PartitionMask.IndexOfHash(idHash, table._partitionBits);

                            localPartitionIds[i] = partitionId;
                            Interlocked.Increment(ref localPartitionInfo[partitionId].Count);
                        }
                    });

                int nextStartIndex = 0;
                for (int i = 0; i < table.PartitionCount; ++i)
                {
                    if (localPartitionInfo[i].Count == 0)
                    {
                        localPartitionInfo[i].StartIndex = -1;
                    }
                    else
                    {
                        localPartitionInfo[i].StartIndex = nextStartIndex;
                        nextStartIndex += localPartitionInfo[i].Count;

                        // NOTE: Count field is cleared here because it is
                        //   reused to track per-partition indexes when 
                        //   building up the sort key data 
                        localPartitionInfo[i].Count = 0;
                    }
                }

                partitionIds = localPartitionIds;
                partitionInfo = localPartitionInfo;
            }
        }
        #endregion

        #region Management
        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            _locker.EnterReadLock();
            try
            {
                if (_partitionBits != _partitions[0].Mask.BitCount || Math.Pow(2, _partitionBits) != this.PartitionCount)
                {
                    details.AddError(ExecutionDetails.TablePartitionBitsWrong, _partitionBits, _partitions[0].Mask.BitCount, this.PartitionCount);
                }

                if (this.RunParallel)
                {
                    Parallel.ForEach(_partitions, (p) =>
                    {
                        p.VerifyConsistency(level, details);
                    });
                }
                else
                {
                    foreach (Partition p in _partitions)
                    {
                        p.VerifyConsistency(level, details);
                    }
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }
        #endregion

        #region Serialization
        /// <summary>
        ///  Returns the path where a Table with the given name will be serialized.
        ///  Used so that additional metadata (ex: security) can be written within it.
        /// </summary>
        /// <param name="tableName">TableName for which to return path</param>
        /// <returns>Full Path where Table will be serialized</returns>
        public static string TableCachePath(string tableName)
        {
            return BinarySerializable.FullPath(Path.Combine("Tables", tableName));
        }

        /// <summary>
        ///  Returns whether a given table exists on disk. This requires the folder
        ///  for the table to exist and to contain at least one .bin file.
        /// </summary>
        /// <param name="tableName">Name of table to check</param>
        /// <returns>True if table exists, False otherwise</returns>
        public static bool Exists(string tableName)
        {
            string tablePath = TableCachePath(tableName);

            foreach (string partitionFile in BinarySerializable.EnumerateUnder(tablePath))
            {
                if (partitionFile.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void Load(string tableName)
        {
            Load(tableName, null);
        }

        public virtual void Load(string tableName, IEnumerable<string> partitions)
        {
            _locker.EnterWriteLock();
            try
            {
                _partitions.Clear();
                this.Name = tableName;

                string tablePath = TableCachePath(tableName);

                if (partitions != null)
                {
                    foreach (string partitionFile in partitions)
                    {
                        Partition p = new Partition();
                        p.Read(Path.Combine(tablePath, partitionFile + ".bin"));
                        _partitions.Add(p);
                    }
                }
                else
                {
                    // Load each bin file in the Table folder (*NOT* bin.new; this means a write was in progress)
                    foreach (string partitionFile in BinarySerializable.EnumerateUnder(tablePath))
                    {
                        if (partitionFile.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                        {
                            Partition p = new Partition();
                            p.Read(partitionFile);
                            _partitions.Add(p);
                        }
                    }
                }

                // If there are no partitions to load, re-add the default 'all' one
                if (_partitions.Count == 0) _partitions.Add(new Partition(PartitionMask.All));

                // Reset the number of partition bits
                _partitionBits = _partitions[0].Mask.BitCount;

                // Reset ColumnAliasCorrector mappings
                _columnAliasCorrector.SetMappings(this);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                DateTime lastWriteTimeUtc = DateTime.MinValue;

                var tablePath = BinarySerializable.FullPath(Path.Combine("Tables", this.Name));
                if (Directory.Exists(tablePath))
                {
                    foreach (string filePath in Directory.GetFiles(tablePath))
                    {
                        lastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
                        break;
                    }
                }

                return lastWriteTimeUtc;
            }
        }

        public void Save()
        {
            _locker.EnterReadLock();
            try
            {
                var tablePath = BinarySerializable.FullPath(Path.Combine("Tables", this.Name));
                Directory.CreateDirectory(tablePath);

                // Write parition data 
                foreach (Partition p in _partitions)
                {
                    p.Write(Path.Combine(tablePath, StringExtensions.Format("{0}.bin", p.Mask)));
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }
        #endregion

        #region Drop
        public void Drop()
        {
            _locker.EnterReadLock();
            try
            {
                Drop(this.Name);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        public static void Drop(string tableName)
        {
            string tablePath = BinarySerializable.FullPath(Path.Combine("Tables", tableName));

            // Not saved 
            if (!Directory.Exists(tablePath))
            {
                return;
            }

            // Delete everything in the table folder (including any additional data, like security)
            Directory.Delete(tablePath, true);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (_locker != null)
            {
                _locker.Dispose();
                _locker = null;
            }
        }
        #endregion
    }
}
