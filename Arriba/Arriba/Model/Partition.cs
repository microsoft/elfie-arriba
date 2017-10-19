// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Extensions;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  Partition represents a set of items in Arriba, containing one or more columns with values.
    ///  Tables are made up of a set of partitions distributed across machines and cores.
    /// </summary>
    public class Partition : ITable, IBinarySerializable
    {
        private ushort _itemCount;

        public PartitionMask Mask { get; private set; }
        public SortedList<string, IUntypedColumn> Columns;
        public SortedList<string, ColumnDetails> DetailsByColumn;
        private ColumnDetails _cachedIdColumnDetails;

        internal Partition(PartitionMask mask) : this()
        {
            this.Mask = mask;
        }

        /// <summary>
        ///  Serialization-only constructor
        /// </summary>
        internal Partition()
        {
            _itemCount = 0;
            this.Columns = new SortedList<string, IUntypedColumn>(StringComparer.OrdinalIgnoreCase);
            this.DetailsByColumn = new SortedList<string, ColumnDetails>(StringComparer.OrdinalIgnoreCase);
            this.Mask = PartitionMask.All;
        }

        /// <summary>
        ///  Return the number of items in this partition. This is based on the set size,
        ///  so new items still with default values are included in this count.
        /// </summary>
        public ushort Count
        {
            get { return _itemCount; }
        }

        internal bool ContainsColumn(string columnName)
        {
            return this.Columns.ContainsKey(columnName);
        }

        #region Column Operations
        /// <summary>
        ///  Return the details of columns in this Table.
        /// </summary>
        public ICollection<ColumnDetails> ColumnDetails
        {
            get { return this.DetailsByColumn.Values; }
        }

        /// <summary>
        ///  Return the details of the ID Column of this Table.
        /// </summary>
        public ColumnDetails IDColumn
        {
            get
            {
                if (_cachedIdColumnDetails == null)
                {
                    _cachedIdColumnDetails = this.DetailsByColumn.Values.Where((cd) => cd.IsPrimaryKey).FirstOrDefault();
                }

                return _cachedIdColumnDetails;
            }
        }

        /// <summary>
        ///  Return the names of all columns in this Table.
        /// </summary>
        public ICollection<string> ColumnNames
        {
            get
            {
                return this.Columns.Keys;
            }
        }

        /// <summary>
        ///  Add a new column with the given details. Columns must be added before values can be set on them.
        /// </summary>
        /// <param name="details">Details of the column to add</param>
        public void AddColumn(ColumnDetails details)
        {
            AddColumn(details, 0);
        }

        /// <summary>
        ///  Add a new column with the given details. Columns must be added before values can be set on them.
        /// </summary>
        /// <param name="details">Details of the column to add</param>
        /// <param name="initialCapacity">suggested initial capacity of the column</param>
        public void AddColumn(ColumnDetails details, ushort initialCapacity)
        {
            if (details == null) throw new ArgumentNullException("details");

            if (this.Columns.ContainsKey(details.Name))
            {
                if (!this.DetailsByColumn[details.Name].Type.Equals(details.Type))
                {
                    AlterColumn(details);
                    return;
                }

                // If the column exists and type matches, we can only update side details (alias)
                this.DetailsByColumn[details.Name] = details;
            }
            else
            {
                if (details.IsPrimaryKey)
                {
                    ColumnDetails idColumnDetails = this.IDColumn;
                    if (idColumnDetails != null)
                    {
                        throw new ArribaException(StringExtensions.Format("Column '{0}' to be added is marked as the primary key but cannot be added because column '{1}' is already the primary key column.", details.Name, idColumnDetails.Name));
                    }
                }

                IUntypedColumn newColumn = ColumnFactory.Build(details, initialCapacity);
                this.Columns[details.Name] = newColumn;
                this.DetailsByColumn[details.Name] = details;
                newColumn.SetSize(_itemCount);
            }
        }

        /// <summary>
        ///  Change the type of a column to a new type. Values are copied from the existing
        ///  column to the new one, if value conversion is possible.
        /// </summary>
        /// <param name="details">Details with existing name and new other details</param>
        public void AlterColumn(ColumnDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");

            if (!this.Columns.ContainsKey(details.Name)) throw new ArribaException(StringExtensions.Format("Column '{0}' does not exist; it can't be altered.", details.Name));

            // Get the old column and build the new one
            IUntypedColumn currentcolumn = this.Columns[details.Name];
            IUntypedColumn replacementColumn = ColumnFactory.Build(details, currentcolumn.Count);

            // Size the new column and copy each value to it
            ushort count = this.Count;
            replacementColumn.SetSize(count);
            for (ushort i = 0; i < count; ++i)
            {
                replacementColumn[i] = currentcolumn[i];
            }

            // Store the new column
            this.Columns[details.Name] = replacementColumn;
            this.DetailsByColumn[details.Name] = details;
        }

        /// <summary>
        ///  Remove the column with the given name.
        /// </summary>
        /// <param name="columnName">Name of column to remove.</param>
        public void RemoveColumn(string columnName)
        {
            if (!this.Columns.ContainsKey(columnName)) throw new ArribaException(StringExtensions.Format("Column '{0}' does not exist; it can't be removed.", columnName));

            this.Columns.Remove(columnName);
            this.DetailsByColumn.Remove(columnName);
        }

        /// <summary>
        ///  Return the set of full ColumnDetails for a given set of column names only
        /// </summary>
        /// <param name="columnNames">Names of columns to get details for</param>
        /// <returns>ColumnDetails for each requested column</returns>
        public List<ColumnDetails> GetDetails(IEnumerable<string> columnNames)
        {
            if (columnNames == null) throw new ArgumentNullException("columnNames");

            List<ColumnDetails> result = new List<ColumnDetails>();

            foreach (string columnName in columnNames)
            {
                result.Add(this.DetailsByColumn[columnName]);
            }

            return result;
        }

        public List<ColumnDetails> GetDetails(IEnumerable<ColumnDetails> partialDetails)
        {
            if (partialDetails == null)
            {
                throw new ArgumentNullException("partialDetails");
            }

            return GetDetails(partialDetails.Select((cd) => cd.Name));
        }
        #endregion

        #region AddOrUpdate (insert, update)
        /// <summary>
        ///  Add or Update the given items with the given values. The specific values added are represented in the linked list
        ///  starting at partitionChains[chainHead]
        /// </summary>
        /// <param name="values">Set of Columns and values to add or update</param>
        /// <param name="partitionChains">storage for the set of linked lists indicating which values are in each partition.  The index is the row number in values
        /// of the corresponding item.  The value is the next item in the chain with -1 indicating the end.</param>
        /// <param name="chainHead">starting index for the list of items that this partition should add</param>
        public void AddOrUpdate(DataBlock.ReadOnlyDataBlock values, AddOrUpdateOptions options)
        {
            int columnCount = values.ColumnCount;
            int idColumnIndex = values.IndexOfColumn(this.IDColumn.Name);

            // Look up the LID for each item or add it
            ushort[] itemLIDs = FindOrAssignLIDs(values, idColumnIndex, options.Mode);

            // If there are new items, resize every column for them
            ushort newCount = (ushort)(_itemCount);
            for (int columnIndex = 0; columnIndex < this.Columns.Count; ++columnIndex)
            {
                IColumn<object> column = this.Columns.Values[columnIndex];
                if (column.Count != newCount) column.SetSize(newCount);
            }

            // Set values for each other provided column which exists
            //  Columns which don't exist at this point were omitted because they had no non-default values
            for (int columnIndex = 0; columnIndex < columnCount; ++columnIndex)
            {
                if (this.ContainsColumn(values.Columns[columnIndex].Name))
                {
                    FillPartitionColumn(values, columnIndex, itemLIDs);
                }
            }

            // Commit every column [ones with new values and ones resized with defaults]
            for (int columnIndex = 0; columnIndex < this.Columns.Count; ++columnIndex)
            {
                IColumn<object> column = this.Columns.Values[columnIndex];
                if (column is ICommittable) (column as ICommittable).Commit();
            }
        }

        private ushort[] FindOrAssignLIDs(DataBlock.ReadOnlyDataBlock values, int idColumnIndex, AddOrUpdateMode mode)
        {
            Type idColumnDataType = values.GetTypeForColumn(idColumnIndex);

            // If the insert array matches types with the column then we can use the native type to do a direct assignment from the input array
            // to the column array.  If the types do not match, we need to fallback to object to allow the Value class to handle the type conversion
            ITypedAddOrUpdateWorker worker;
            if (_cachedWorkers.TryGetValue(idColumnDataType, out worker) == false)
            {
                worker = NativeContainer.CreateTypedInstance<ITypedAddOrUpdateWorker>(typeof(AddOrUpdateWorker<>), idColumnDataType);
                _cachedWorkers.Add(idColumnDataType, worker);
            }

            return worker.FindOrAssignLIDs(this, values, idColumnIndex, mode);
        }

        private Value _cachedValue = Value.Create(null);

        private void FillPartitionColumn(DataBlock.ReadOnlyDataBlock values, int columnIndex, ushort[] itemLIDs)
        {
            string columnName = values.Columns[columnIndex].Name;
            if (columnName.Equals(this.IDColumn.Name, StringComparison.OrdinalIgnoreCase)) return;

            Type dataBlockColumnDataType = values.GetTypeForColumn(columnIndex);

            // If the insert array matches types with the column then we can use the native type to do a direct assignment from the input array
            // to the column array.  If the types do not match, we need to fallback to object to allow the Value class to handle the type conversion
            ITypedAddOrUpdateWorker worker;
            if (_cachedWorkers.TryGetValue(dataBlockColumnDataType, out worker) == false)
            {
                worker = NativeContainer.CreateTypedInstance<ITypedAddOrUpdateWorker>(typeof(AddOrUpdateWorker<>), dataBlockColumnDataType);
                _cachedWorkers.Add(dataBlockColumnDataType, worker);
            }

            worker.FillPartitionColumn(this, values, columnIndex, itemLIDs);
        }

        private Dictionary<Type, ITypedAddOrUpdateWorker> _cachedWorkers = new Dictionary<Type, ITypedAddOrUpdateWorker>();

        private interface ITypedAddOrUpdateWorker
        {
            ushort[] FindOrAssignLIDs(Partition p, DataBlock.ReadOnlyDataBlock values, int idColumnIndex, AddOrUpdateMode mode);
            void FillPartitionColumn(Partition p, DataBlock.ReadOnlyDataBlock values, int columnIndex, ushort[] itemLIDs);
        }

        /// <summary>
        /// Strongly typed implementations of core loops used to operate on arrays in their native type to eliminate boxing/unboxing coversions
        /// </summary>
        /// <typeparam name="T">Type of the column array</typeparam>
        private class AddOrUpdateWorker<T> : ITypedAddOrUpdateWorker
        {
            private ValueTypeReference<T> vtr = new ValueTypeReference<T>();
            Value v = Value.Create(null);

            public ushort[] FindOrAssignLIDs(Partition p, DataBlock.ReadOnlyDataBlock values, int idColumnIndex, AddOrUpdateMode mode)
            {
                ushort[] itemLIDs = new ushort[values.RowCount];
                int addCount = 0;

                IUntypedColumn idColumn = p.Columns[p.IDColumn.Name];
                IColumn<T> typedIdColumn = null;

                if (typeof(T) == idColumn.ColumnType)
                {
                    typedIdColumn = (IColumn<T>)idColumn.InnerColumn;
                }

                for (int index = 0; index < values.RowCount; ++index)
                {
                    // Look for the LIDs a
                    T externalID = values.GetValueT<T>(index, idColumnIndex);
                    if (typedIdColumn != null)
                    {
                        typedIdColumn.TryGetIndexOf(externalID, out itemLIDs[index]);
                    }
                    else
                    {
                        idColumn.TryGetIndexOf(externalID, out itemLIDs[index]);
                    }

                    if (itemLIDs[index] == ushort.MaxValue) addCount++;

                    // Verify this item was routed to the right partition
                    vtr.Value = externalID;
                    v.Assign(vtr);
                    int idHash = v.GetHashCode();
                    if (!p.Mask.Matches(idHash))
                    {
                        throw new ArribaException(StringExtensions.Format("Item with ID '{0}', hash '{1:x}' incorrectly routed to Partition {2}.", externalID, idHash, p.Mask));
                    }
                }

                // Go back and add the items which need to be added in a batch
                if (mode != AddOrUpdateMode.UpdateAndIgnoreAdds)
                {
                    Dictionary<T, ushort> newlyAssignedLIDs = null;

                    for (int index = 0; index < values.RowCount; ++index)
                    {
                        T idValue = values.GetValueT<T>(index, idColumnIndex);
                        ushort lid = itemLIDs[index];

                        // If this is an add...
                        if (lid == ushort.MaxValue)
                        {
                            // If we have adds, we'll need to track new IDs
                            if (newlyAssignedLIDs == null) newlyAssignedLIDs = new Dictionary<T, ushort>(addCount);

                            T externalID = idValue;

                            // If this ID was already added in this batch, this time it's an update
                            if (newlyAssignedLIDs.TryGetValue(externalID, out lid) == false)
                            {
                                // If in "UpdateOnly" mode, throw
                                if (mode == AddOrUpdateMode.UpdateOnly)
                                {
                                    throw new ArribaWriteException(externalID, p.IDColumn.Name, externalID,
                                        new ArribaException("AddOrUpdate was in UpdateOnly mode but contained a new ID, which is an error."));
                                }

                                // If this was a new item and not added in this batch, assign it a LID
                                lid = p._itemCount;

                                if (lid == ushort.MaxValue)
                                {
                                    throw new ArribaWriteException(externalID, p.IDColumn.Name, externalID,
                                        new ArribaException("Column full in Partition. Unable to add items."));
                                }

                                p._itemCount++;
                                idColumn.SetSize((ushort)(p._itemCount));

                                if (typedIdColumn != null)
                                {
                                    typedIdColumn[lid] = externalID;
                                }
                                else
                                {
                                    idColumn[lid] = externalID;
                                }

                                newlyAssignedLIDs[externalID] = lid;
                            }
                        }

                        itemLIDs[index] = lid;
                    }

                    // Commit the updates to the values column if the column requires it (FastAddSortedColumn does)
                    if (idColumn is ICommittable) (idColumn as ICommittable).Commit();
                }

                return itemLIDs;
            }

            public void FillPartitionColumn(Partition p, DataBlock.ReadOnlyDataBlock values, int columnIndex, ushort[] itemLIDs)
            {
                string columnName = values.Columns[columnIndex].Name;
                if (columnName.Equals(p.IDColumn.Name, StringComparison.OrdinalIgnoreCase)) return;

                IUntypedColumn untypedColumn = p.Columns[columnName];
                IColumn<T> typedColumn = null;

                if (typeof(T) == untypedColumn.ColumnType)
                {
                    typedColumn = (IColumn<T>)untypedColumn.InnerColumn;
                }

                for (int rowIndex = 0; rowIndex < values.RowCount; ++rowIndex)
                {
                    T value = values.GetValueT<T>(rowIndex, columnIndex);
                    // If the item is new and no LID was assigned, we don't set values
                    if (itemLIDs[rowIndex] == ushort.MaxValue) continue;
                    try
                    {
                        if (typedColumn != null)
                        {
                            typedColumn[itemLIDs[rowIndex]] = value;
                        }
                        else
                        {
                            untypedColumn[itemLIDs[rowIndex]] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ArribaWriteException(values[rowIndex, 0], columnName, value, ex);
                    }

                }
            }
        }
        #endregion

        #region Delete
        /// <summary>
        ///  Delete items from this Partition which meet the provided criteria.
        /// </summary>
        /// <param name="where">Expression matching items to delete</param>
        /// <param name="details">Details of execution</param>
        /// <returns>Result including number deleted</returns>
        public DeleteResult Delete(IExpression where)
        {
            if (where == null) throw new ArgumentNullException("where");

            DeleteResult result = new DeleteResult();

            // Find the set of items to delete
            ShortSet whereSet = new ShortSet(this.Count);
            where.TryEvaluate(this, whereSet, result.Details);

            if (result.Details.Succeeded)
            {
                // Swap each item to delete with the last item in the set
                ushort lastItemIndex = (ushort)(this.Count - 1);
                ushort[] itemsToDelete = whereSet.Values;
                for (int i = itemsToDelete.Length - 1; i >= 0; --i)
                {
                    // If this isn't the last item, swap the last item with it
                    ushort itemToDelete = itemsToDelete[i];
                    if (itemToDelete != lastItemIndex)
                    {
                        foreach (IColumn<object> c in this.Columns.Values)
                        {
                            c[itemToDelete] = c[lastItemIndex];
                        }
                    }

                    lastItemIndex--;
                }

                // Resize the set to exclude all of the deleted items (now at the end)
                foreach (IColumn<object> c in this.Columns.Values)
                {
                    c.SetSize((ushort)(lastItemIndex + 1));
                }

                // Record the new count in the Partition itself
                _itemCount = (ushort)(lastItemIndex + 1);

                // Return the count deleted
                result.Count = whereSet.Count();
            }

            return result;
        }
        #endregion

        #region Query
        /// <summary>
        ///  Run the provided query and return a partition-specific result.
        /// </summary>
        /// <typeparam name="T">Type of result for query</typeparam>
        /// <param name="queryToExecute">Query to execute</param>
        /// <returns>Result for Query within this Partition</returns>
        public T Query<T>(IQuery<T> queryToExecute)
        {
            if (queryToExecute == null) throw new ArgumentNullException("queryToExecute");

            return queryToExecute.Compute(this);
        }
        #endregion

        #region Management
        public void VerifyConsistency(VerificationLevel level, ExecutionDetails details)
        {
            if (details == null) throw new ArgumentNullException("details");

            ushort expectedColumnSize = this.Count;

            // Verify each column internally
            foreach (IColumn column in this.Columns.Values)
            {
                column.VerifyConsistency(level, details);

                // Verify columns are all the same item count
                if (column.Count != expectedColumnSize)
                {
                    details.AddError(ExecutionDetails.ColumnSizeIsUnexpected, column.Name, column.Count, expectedColumnSize);
                }
            }

            // Verify all IDs are supposed to be in this partition, if this table has data yet
            // [Tables without data are allowed to not have any columns yet, do IDColumn would be null]
            if (this.Count > 0)
            {
                if (this.IDColumn == null)
                {
                    details.AddError(ExecutionDetails.PartitionHasNoIDColumn);
                }
                else
                {
                    Value id = Value.Create(null);
                    IColumn<object> idColumn = this.Columns[this.IDColumn.Name];
                    for (ushort i = 0; i < this.Count; ++i)
                    {
                        id.Assign(idColumn[i]);
                        int hashCode = id.GetHashCode();
                        if (!this.Mask.Matches(hashCode))
                        {
                            details.AddError(ExecutionDetails.ItemInWrongPartition, id, hashCode, this.Mask);
                        }
                    }
                }
            }
        }
        #endregion

        #region IBinarySerializable
        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.Columns.Clear();
            this.DetailsByColumn.Clear();

            this.Mask.ReadBinary(context);
            _itemCount = context.Reader.ReadUInt16();
            int columnCount = context.Reader.ReadInt32();

            for (int i = 0; i < columnCount; ++i)
            {
                ColumnDetails details = new ColumnDetails();
                details.ReadBinary(context);

                AddColumn(details);
                this.Columns[details.Name].ReadBinary(context);
            }
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            this.Mask.WriteBinary(context);
            context.Writer.Write(_itemCount);
            context.Writer.Write(this.Columns.Count);

            foreach (string columnName in this.Columns.Keys)
            {
                this.DetailsByColumn[columnName].WriteBinary(context);
                this.Columns[columnName].WriteBinary(context);
            }
        }
        #endregion
    }
}
