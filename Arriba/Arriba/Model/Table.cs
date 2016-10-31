// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public void AddColumnsFromBlock(DataBlock values)
        {
            bool foundIdColumn = (_partitions[0].IDColumn != null);
            List<ColumnDetails> discoveredNewColumns = new List<ColumnDetails>();

            for (int columnIndex = 0; columnIndex < values.ColumnCount; ++columnIndex)
            {
                // Get the column name from the block
                string columnName = values.Columns[columnIndex].Name;

                // Add or alter columns only which weren't manually added
                if (_partitions[0].ContainsColumn(columnName)) continue;

                // Make the ID column the first one to end with 'ID' or the first column
                bool isIdColumn = (foundIdColumn == false && columnName.EndsWith("ID"));

                // Walk all values in this block to infer the column type
                Type bestColumnType = null;
                for (int rowIndex = 0; rowIndex < values.RowCount; ++rowIndex)
                {
                    bestColumnType = Value.Create(values[rowIndex, columnIndex]).BestType(bestColumnType);
                }

                // If no values were set, default to string [can't tell actual best type]
                if (bestColumnType == null) bestColumnType = typeof(String);

                discoveredNewColumns.Add(new ColumnDetails(columnName, bestColumnType.Name, null) { IsPrimaryKey = isIdColumn });
                foundIdColumn |= isIdColumn;
            }

            // If no column name ended with 'ID', the first one is the ID column
            if (!foundIdColumn && discoveredNewColumns.Count > 0)
            {
                discoveredNewColumns[0].IsPrimaryKey = true;
            }

            // Add the discovered columns. If any names match existing columns they'll be merged properly in Partition.AddColumn.
            AddColumns(discoveredNewColumns);
        }

        /// <summary>
        ///  Add or Update the given items with the given values. The ID column must be passed
        ///  and must be the first column. If an ID is not known, the item will be added.
        ///  For each item, the value for each column is set to the provided values.
        /// </summary>
        /// <param name="values">Set of Columns and values to add or update</param>
        public void AddOrUpdate(DataBlock values)
        {
            AddOrUpdate(values, new AddOrUpdateOptions());
        }

        /// <summary>
        ///  Add or Update the given items with the given values. The ID column must be passed
        ///  and must be the first column. If an ID is not known, the item will be added.
        ///  For each item, the value for each column is set to the provided values.
        /// </summary>
        /// <param name="values">Set of Columns and values to add or update</param>
        public void AddOrUpdate(DataBlock values, AddOrUpdateOptions options)
        {
            _locker.EnterWriteLock();
            try
            {
                if (values == null) throw new ArgumentNullException("values");

                // Add columns from data, if this is the first data and columns weren't predefined
                if (options.AddMissingColumns) AddColumnsFromBlock(values);

                ColumnDetails idColumn = _partitions[0].IDColumn;
                if (idColumn == null) throw new ArribaException("Items cannot be added to this Table because it does not yet have an ID column defined. Call AddColumn with exactly one column with 'IsPrimaryKey' true and then items may be added.");
                int idColumnIndex = values.IndexOfColumn(idColumn.Name);
                if (idColumnIndex == -1) throw new ArribaException(StringExtensions.Format("AddOrUpdates must be passed the ID column, '{0}', in order to tell which items to update.", idColumn.Name));

                // Verify all passed columns exist
                foreach (ColumnDetails column in values.Columns)
                {
                    ColumnDetails foundColumn;
                    if (!_partitions[0].DetailsByColumn.TryGetValue(column.Name, out foundColumn))
                    {
                        throw new ArribaException(StringExtensions.Format("AddOrUpdate failed because values were passed for column '{0}', which is not in the table. Use AddColumn to add all columns first or ensure the first block added to the Table has all desired columns.", column.Name));
                    }
                }

                // Non-Parallel Implementation
                if (_partitions.Count == 1)
                {
                    _partitions[0].AddOrUpdate(values, options);
                    return;
                }

                // Determine the partition each item should go to
                int[] partitionChains = null;
                int[] partitionChainHeads = null;
                Array idColumnArray = values.GetColumn(idColumnIndex);
                Type idColumnArrayType = idColumnArray.GetType().GetElementType();

                IChooseSplit splitter = NativeContainer.CreateTypedInstance<IChooseSplit>(typeof(ChooseSplitHelper<>), idColumnArrayType);
                splitter.ChooseSplit(this, idColumnArray, out partitionChains, out partitionChainHeads);

                Action<Tuple<int, int>, ParallelLoopState> forBody =
                    delegate (Tuple<int, int> range, ParallelLoopState unused)
                    {
                        for (int i = range.Item1; i < range.Item2; ++i)
                        {
                            _partitions[i].AddOrUpdate(values, options, partitionChains, partitionChainHeads[i]);
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

        #region Select
        /// <summary>
        ///  Select returns items matching the given query from the Table, like SQL SELECT.
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <returns>SelectResult with the count and values returned by the query</returns>
        public SelectResult Select(SelectQuery query)
        {
            _locker.EnterReadLock();
            try
            {
                Stopwatch w = Stopwatch.StartNew();
                string idColumnName = _partitions[0].IDColumn.Name;

                // Run all correctors
                query.Correct(_columnAliasCorrector);

                // Prepare this query to run (expand '*', default ORDER BY column, ...)
                query.Prepare(_partitions[0]);

                // If this is already an ID only query, just run it directly
                if (query.Columns.Count == 1 && query.Columns[0].Equals(idColumnName))
                {
                    SelectResult result = this.SelectInner(query);
                    result.Runtime = w.Elapsed;
                    return result;
                }

                // Otherwise, query for ID only (no highlight) to find the exact set to return
                SelectQuery chooseItemsQuery = new SelectQuery(query);
                chooseItemsQuery.Columns = new string[] { idColumnName };
                chooseItemsQuery.Highlighter = null;
                SelectResult chooseItemsResult = this.SelectInner(chooseItemsQuery);

                // If the query failed or was empty, return as-is
                if (!chooseItemsResult.Details.Succeeded || chooseItemsResult.CountReturned == 0)
                {
                    chooseItemsResult.Query = query;
                    chooseItemsResult.Runtime = w.Elapsed;
                    return chooseItemsResult;
                }

                // Requery to get all columns for the exact items by ID only
                // Include the previous where clauses for highlighting
                SelectQuery getValuesQuery = new SelectQuery(query);
                getValuesQuery.Count = chooseItemsResult.CountReturned;
                getValuesQuery.Skip = 0;
                getValuesQuery.Where = new AndExpression(new TermInExpression(idColumnName, chooseItemsResult.Values.GetColumn(0)), query.Where);
                SelectResult getValuesResult = this.SelectInner(getValuesQuery);

                // Tie the original query, real total, and full runtime with the result
                getValuesResult.Query = query;
                getValuesResult.Total = chooseItemsResult.Total;
                getValuesResult.Runtime = w.Elapsed;

                return getValuesResult;
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        ///  Select returns items matching the given query from the Table, like SQL SELECT.
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <returns>SelectResult with the count and values returned by the query</returns>
        private SelectResult SelectInner(SelectQuery query)
        {
            // Add the Order By Column
            List<string> columns = new List<string>(query.Columns);
            columns.Add(query.OrderByColumn);
            query.Columns = columns;

            // Get all items (need to do 'skip' on merge)
            ushort originalCount = query.Count;
            query.Count = (ushort)(query.Count + query.Skip);
            query.Skip = 0;

            // Run the query
            SelectResult[] partitionResults = new SelectResult[_partitions.Count];
            if (this.RunParallel)
            {
                Parallel.For(0, _partitions.Count, (i) =>
                {
                    partitionResults[i] = _partitions[i].Query(query);
                });
            }
            else
            {
                for (int i = 0; i < _partitions.Count; ++i)
                {
                    partitionResults[i] = _partitions[i].Query(query);
                }
            }

            // Change the column list and skip/count back
            columns.RemoveAt(columns.Count - 1);
            query.Columns = columns;
            query.Skip = (uint)(query.Count - originalCount);
            query.Count = originalCount;

            // Merge the results
            return query.Merge(partitionResults);
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
            // Route SelectQuery to the Select API. It has a different flow, but users shouldn't have to know not to call Query with SelectQuery instances.
            if (query is SelectQuery) return (T)(object)this.Select((SelectQuery)query);

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
        private interface IChooseSplit
        {
            void ChooseSplit(Table table, Array values, out int[] partitionChains, out int[] partitionChainHeads);
        }

        private class ChooseSplitHelper<T> : IChooseSplit
        {
            /// <summary>
            ///  Identify the Partition for each item to go to and return a map in the form if an array which contains Partitions.Count number of 
            ///  non-overlapping linked lists.  The head of each list is returned in the array partitionChainHeads.  The next item of each list is 
            ///  kept as the value at each index in partitionChains with -1 signifying the last node.
            /// </summary>
            /// <param name="values">DataBlock containing values to be added to the table</param>
            /// <param name="idColumnIndex">Index of the ID column in values</param>
            /// <param name="partitionChains">[out] storage for the set of linked lists of nodes assigned to each partition.  The index is the row number of
            /// of the matching value in the values DataBlock and the value is the index of the next item in the list.  -1 signifies the last node. </param>
            /// <param name="partitionChainHeads">[out] The set of all of the linked lists.  The index is the ID of the partition that each value in the list
            /// should be inserted into.  The value is the index of the first item in this list in the partitionsChain array.</param>
            public void ChooseSplit(Table table, Array values, out int[] partitionChains, out int[] partitionChainHeads)
            {
                // Allocate storage for our return values and preset to -1, the terminal value.
                int itemCount = values.Length;
                int[] localPartitionChains = new int[itemCount];
                for (int i = 0; i < itemCount; ++i) localPartitionChains[i] = -1;

                int partitionCount = table._partitions.Count;
                int[] localPartitionChainHeads = new int[partitionCount];
                for (int i = 0; i < partitionCount; ++i) localPartitionChainHeads[i] = -1;

                Action<Tuple<int, int>, ParallelLoopState> forBody =
                    delegate (Tuple<int, int> range, ParallelLoopState unused)
                    {
                        ValueTypeReference<T> vtr = new ValueTypeReference<T>();
                        Value v = Value.Create(null);
                        for (int i = range.Item1; i < range.Item2; ++i)
                        {
                            // Hash the ID for each item and compute the partition that the item belongs to
                            vtr.Value = ((T[])values)[i];
                            v.Assign(vtr);
                            int idHash = v.GetHashCode();
                            int partitionId = PartitionMask.IndexOfHash(idHash, table._partitionBits);

                            // Add a link into the list for the matching partition including the index of the current item
                            // order doesn't mattter for the linked list, only that the final list is accurate.
                            // It's possible for 2 threads to compute the same partitionId at the same time.  If that happens, 
                            // one will "win" at getting the current head index while the other will get the index from the thread
                            // that was just set.  Both links will then be inserted in to localPartitionChains at the same time and the
                            // full list will still be accurate.

                            // First, insert ourself as the head of the list while retreiving the former head.  
                            // The former head is the next item in the chain with respect to ourself.
                            int oldHead = Interlocked.Exchange(ref localPartitionChainHeads[partitionId], i);

                            // Store the former head as being behind this value in the linked list.
                            localPartitionChains[i] = oldHead;
                        }
                    };

                if (table.RunParallel)
                {
                    var rangePartitioner = Partitioner.Create(0, itemCount);
                    Parallel.ForEach(rangePartitioner, table.ParallelOptions, forBody);
                }
                else
                {
                    var range = Tuple.Create(0, itemCount);
                    forBody(range, null);
                }

                partitionChains = localPartitionChains;
                partitionChainHeads = localPartitionChainHeads;
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
