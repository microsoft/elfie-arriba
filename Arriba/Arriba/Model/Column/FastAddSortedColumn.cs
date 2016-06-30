// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Arriba.Extensions;
using Arriba.Model.Expressions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Model
{
    /// <summary>
    ///  SortedColumn wraps an underlying column and tracks the sort order of 
    ///  the underlying IDs, enabling fast Where clauses for comparison 
    ///  operators and IndexOf. SortedColumn adds two bytes per value to keep
    ///  the order.
    /// </summary>
    /// <typeparam name="T">Type of underlying values indexed</typeparam>
    public class FastAddSortedColumn<T> : SortedColumn<T>, ICommittable where T : IComparable<T>
    {
        private ushort _commitCount;

        private CompareValues<T> _valueComparer;

        public FastAddSortedColumn(IColumn<T> column) : this(column, 0)
        {
        }

        public FastAddSortedColumn(IColumn<T> column, ushort initialCapacity) : base(column, initialCapacity)
        {
            this.SortedIDs = ArrayExtensions.CreateRecommendedArray<ushort>(initialCapacity);
            this.SortedIDCount = 0;
            _valueComparer = new CompareValues<T>(this);
        }

        public override T this[ushort lid]
        {
            get { return this.Column[lid]; }
            set
            {
                if (lid < this.SortedIDCount)
                {
                    // Re-sort SortedIDs to include the new value on updates
                    UpdateSortedIDs(value, lid);
                }

                // Set the new value
                this.Column[lid] = value;
            }
        }

        /// <summary>
        /// Commits pending adds.  This must be done before querying the column
        /// </summary>
        public override void Commit()
        {
            base.Commit();

            // If there is nothing new to commit, nothing to do.
            if (_commitCount == this.SortedIDCount) return;

            // Sort the added elements
            Array.Sort(this.SortedIDs, this.SortedIDCount, (_commitCount - this.SortedIDCount), _valueComparer);

            // Get the index of the first added item
            ushort newItemPosition = this.SortedIDCount;
            T addedItem = this[this.SortedIDs[newItemPosition]];

            // Find the index where the inserts start
            ushort insertPosition = (ushort)FindInsertionPosition(addedItem, this.SortedIDs[newItemPosition]);
            ushort copyCount = (ushort)(this.SortedIDCount - insertPosition);

            // Check out a temporary workspace array
            ushort[] workspace;
            s_workspacePool.TryGet(out workspace);

            // Leave everything up to the insert position in place (these are already correct) and copy the rest to our workspace
            Array.Copy(this.SortedIDs, insertPosition, workspace, 0, copyCount);

            int copyPosition = 0;
            while (copyPosition < copyCount && newItemPosition < _commitCount)
            {
                T currentItem = this[workspace[copyPosition]];
                addedItem = this[this.SortedIDs[newItemPosition]];

                int cmp = currentItem.CompareTo(addedItem);

                if (cmp <= 0)
                {
                    this.SortedIDs[insertPosition] = workspace[copyPosition];
                    copyPosition++;
                }
                else
                {
                    this.SortedIDs[insertPosition] = this.SortedIDs[newItemPosition];
                    newItemPosition++;
                }

                insertPosition++;
            }

            if (copyPosition < copyCount)
            {
                // Left over items in the original set, copy them all
                Array.Copy(workspace, copyPosition, this.SortedIDs, insertPosition, (copyCount - copyPosition));
            }

            s_workspacePool.Put(workspace);
            this.SortedIDCount = _commitCount;
        }

        public override void SetSize(ushort size)
        {
            ushort currentSize = this.Column.Count;

            if (size > currentSize)
            {
                // Ensure the column has enough space to store the new items, use Grow to avoid shrinking the column if it was preallocated with a higher capacity
                ArrayExtensions.Grow(ref this.SortedIDs, size, ushort.MaxValue);

                int countToInsert = size - currentSize;

                // Find the index where IDs with the new value should be inserted
                //int insertionIndex = FindInsertionPosition(this.DefaultValue, (ushort)(size - 1));
                int insertionIndex = currentSize;

                // Insert SortedIDs for new items at the correct place
                for (int i = currentSize; i < size; ++i)
                {
                    this.SortedIDs[i] = (ushort)(i);
                }

                // Set the number of items now in the SortedID set - after insertions so searches have correct bounds
                _commitCount = size;
                this.Column.SetSize(size);
            }
            else
            {
                this.Commit();
                base.SetSize(size);
                _commitCount = size;
            }
        }

        public override void ReadBinary(ISerializationContext context)
        {
            base.ReadBinary(context);
            _commitCount = this.SortedIDCount;
        }

        private class CompareValues<V> : IComparer<ushort> where V : IComparable<V>
        {
            private FastAddSortedColumn<V> _parentColumn;

            public CompareValues(FastAddSortedColumn<V> parentColumn)
            {
                _parentColumn = parentColumn;
            }

            public int Compare(ushort leftLID, ushort rightLID)
            {
                // sort first by value and then by LID if values match
                int valueCompare = _parentColumn[leftLID].CompareTo(_parentColumn[rightLID]);

                if (valueCompare == 0)
                {
                    valueCompare = leftLID.CompareTo(rightLID);
                }

                return valueCompare;
            }
        }

        // A pool of ushort[] for resorting to avoid creating one for every partition
        private static ObjectCache<ushort[]> s_workspacePool = new ObjectCache<ushort[]>(() => new ushort[ushort.MaxValue]);
    }
}
