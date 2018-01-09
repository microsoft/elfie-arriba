using System;
using XForm.Data;
using XForm.Types;

namespace XForm.Core
{
    public enum ChooseDirection
    {
        Minimum,
        Maximum
    }

    public interface IDictionaryColumn
    {
        int Length { get; }
        Array Values { get; }
        void Reset(int size);
        int HashCurrent(int hash);
        void SetBatch(DataBatch batch);
        void SetCurrent(uint index);
        void SwapCurrent(uint index);
        bool EqualsCurrent(uint index);
        bool BetterThanCurrent(uint index, ChooseDirection direction);
    }

    internal class DictionaryColumn<TColumnType> : IDictionaryColumn
    {
        private IDataBatchComparer<TColumnType> _comparer;
        private TColumnType[] _values;
        private TColumnType _current;

        private DataBatch _currentBatch;
        private TColumnType[] _currentBatchArray;

        public DictionaryColumn()
        {
            _comparer = (IDataBatchComparer<TColumnType>)TypeProviderFactory.TryGet(typeof(TColumnType)).TryGetComparer();
        }

        public int Length => _values.Length;
        public Array Values => _values;

        public void Reset(int size)
        {
            _values = new TColumnType[size];
        }

        public int HashCurrent(int hash)
        {
            return (hash << 5) - hash + _comparer.GetHashCode(_current);
        }

        public void SetBatch(DataBatch batch)
        {
            _currentBatch = batch;
            _currentBatchArray = (TColumnType[])batch.Array;
        }

        public void SetCurrent(uint index)
        {
            int realIndex = _currentBatch.Index((int)index);
            if(_currentBatch.IsNull != null && _currentBatch.IsNull[realIndex])
            {
                _current = default(TColumnType);
            }
            else
            {
                _current = _currentBatchArray[realIndex];
            }
        }

        public bool EqualsCurrent(uint index)
        {
            return _comparer.WhereEqual(_current, _values[index]);
        }

        public bool BetterThanCurrent(uint index, ChooseDirection direction)
        {
            if(direction == ChooseDirection.Maximum)
            {
                return _comparer.WhereGreaterThan(_current, _values[index]);
            }
            else
            {
                return _comparer.WhereLessThan(_current, _values[index]);
            }
        }

        public void SwapCurrent(uint index)
        {
            TColumnType temp = _values[index];
            _values[index] = _current;
            _current = temp;
        }
    }

    public class ChooseDictionary : HashCore
    {
        private ChooseDirection _chooseDirection;
        private ColumnDetails[] _keyColumns;
        private ColumnDetails _rankColumn;

        private IDictionaryColumn[] _keys;
        private IDictionaryColumn _ranks;
        private IDictionaryColumn _bestRowIndices;

        private int _totalRowCount;
        private BitVector _bestRowVector;
        private int[] _rowBuffer;

        public ChooseDictionary(ChooseDirection direction, ColumnDetails[] keyColumns, ColumnDetails rankColumn, int initialCapacity = -1) : base(initialCapacity)
        {
            _chooseDirection = direction;
            _keyColumns = keyColumns;
            _rankColumn = rankColumn;

            // Create a strongly typed column for each key
            _keys = new IDictionaryColumn[keyColumns.Length];
            for(int i = 0; i < keyColumns.Length; ++i)
            {
                _keys[i] = (IDictionaryColumn)Allocator.ConstructGenericOf(typeof(DictionaryColumn<>), keyColumns[i].Type);
            }

            // Create a strongly typed column to hold rank values
            _ranks = (IDictionaryColumn)Allocator.ConstructGenericOf(typeof(DictionaryColumn<>), rankColumn.Type);
            _bestRowIndices = (IDictionaryColumn)Allocator.ConstructGenericOf(typeof(DictionaryColumn<>), typeof(int));
        }

        public void Add(DataBatch[] keys, DataBatch rankValues, int startIndexInclusive)
        {
            // Convert the index range to a DataBatch (lame, for consistency with resize path)
            Allocator.AllocateToSize(ref _rowBuffer, rankValues.Count);
            for(int i = 0; i < rankValues.Count; ++i)
            {
                _rowBuffer[i] = startIndexInclusive + i;
            }

            Add(keys, rankValues, DataBatch.All(_rowBuffer, rankValues.Count));
        }

        private void Add(DataBatch[] keys, DataBatch rankValues, DataBatch bestRowBatch)
        {
            if (keys.Length != _keys.Length) throw new ArgumentOutOfRangeException("keys.Length");

            // Give the DataBatches to the keys and rank columns
            for (int keyIndex = 0; keyIndex < keys.Length; ++keyIndex)
            {
                _keys[keyIndex].SetBatch(keys[keyIndex]);
            }
            _ranks.SetBatch(rankValues);
            _bestRowIndices.SetBatch(bestRowBatch);

            for (uint rowIndex = 0; rowIndex < rankValues.Count; ++rowIndex)
            {
                // Set values to insert as current
                SetCurrent(rowIndex);

                // Get the hash of the row
                uint hash = HashCurrent();

                // Add the new item
                if (!this.Add(hash))
                {
                    Expand();

                    SetCurrent(rowIndex);
                    Add(hash);
                }
            }

            // Keep the total of rows added (to know the vector size to make)
            _totalRowCount += rankValues.Count;
        }

        public DataBatch GetChosenRows(int firstRowIndex, int startIndexInclusive, int endIndexExclusive)
        {
            // Allocate a buffer to hold matching rows in the range
            Allocator.AllocateToSize(ref _rowBuffer, endIndexExclusive - startIndexInclusive);

            // If we haven't converted best rows to a vector, do so (one time)
            if (_bestRowVector == null) ConvertChosenToVector();

            // Will this be faster because there will often be far more items than wanted?
            //int count = 0;
            //for (int i = startIndexInclusive; i < endIndexExclusive; ++i)
            //{
            //    if (_bestRowVector[i]) _rowBuffer[count++] = i;
            //}

            // Get a page from the vector starting with the first index
            int lastIndex = startIndexInclusive;
            int count = _bestRowVector.Page(_rowBuffer, ref lastIndex);

            // If we got values from endIndexExclusive onward, remove them
            // Binary search?
            if(lastIndex > endIndexExclusive)
            {
                while (_rowBuffer[count - 1] >= endIndexExclusive) count--;
            }

            // Return the matches
            return DataBatch.All(_rowBuffer, count);
        }

        private void ConvertChosenToVector()
        {
            _bestRowVector = new BitVector(_totalRowCount);

            // Build a bit vector of all of the best rows identified
            byte[] metadata = this.Metadata;
            int[] bestRowIndices = (int[])this._bestRowIndices.Values;
            for(int i = 0; i < metadata.Length; ++i)
            {
                if(metadata[i] != 0)
                {
                    _bestRowVector.Set(bestRowIndices[i]);
                }
            }
        }

        protected override void Reset(int size)
        {
            for (int i = 0; i < _keys.Length; ++i)
            {
                _keys[i].Reset(size);
            }

            this._ranks.Reset(size);
            this._bestRowIndices.Reset(size);
        }

        private void SetCurrent(uint index)
        {
            // Set the row in each DataBatch as current
            for (int keyIndex = 0; keyIndex < _keys.Length; ++keyIndex)
            {
                _keys[keyIndex].SetCurrent(index);
            }
            _ranks.SetCurrent(index);
            _bestRowIndices.SetCurrent(index);
        }

        private uint HashCurrent()
        {
            int hash = 0;

            for (int keyIndex = 0; keyIndex < _keys.Length; ++keyIndex)
            {
                hash = _keys[keyIndex].HashCurrent(hash);
            }

            return unchecked((uint)hash);
        }

        protected override bool EqualsCurrent(uint index)
        {
            bool equals = true;
            for (int i = 0; equals == true && i < _keys.Length; ++i)
            {
                equals &= _keys[i].EqualsCurrent(index);
            }
            return equals;
        }

        protected override void SwapWithCurrent(uint index)
        {
            // If the keys matched but the rank isn't better, don't change the matching row
            // Need to fix: Doubling Equals calls
            if(EqualsCurrent(index))
            {
                if (!_ranks.BetterThanCurrent(index, _chooseDirection)) return;
            }
            else
            {
                // If the keys didn't match, swap them
                for (int i = 0; i < _keys.Length; ++i)
                {
                    _keys[i].SwapCurrent(index);
                }
            }

            // If the rank is better or we're swapping with a non-match, write the new rank and row
            _ranks.SwapCurrent(index);
            _bestRowIndices.SwapCurrent(index);
        }

        protected override void Expand()
        {
            // Build a selector of table values which were non-empty
            int[] indices = new int[this._bestRowIndices.Length];

            byte[] metadata = this.Metadata;
            int count = 0;
            for (int i = 0; i < indices.Length; ++i)
            {
                if (metadata[i] != 0) indices[count++] = i;
            }

            // Save the old keys, ranks, and row indices in DataBatches
            DataBatch[] keyBatches = new DataBatch[this._keys.Length];
            for (int i = 0; i < this._keys.Length; ++i)
            {
                keyBatches[i] = DataBatch.All(this._keys[i].Values).Reselect(ArraySelector.Map(indices, count));
            }

            DataBatch rankBatch = DataBatch.All(this._ranks.Values).Reselect(ArraySelector.Map(indices, count));
            DataBatch bestRowBatch = DataBatch.All(this._bestRowIndices.Values).Reselect(ArraySelector.Map(indices, count));

            // Expand the table
            Reset(HashCore.ResizeToSize(this._bestRowIndices.Length));

            // Add items to the enlarged table
            Add(keyBatches, rankBatch, bestRowBatch);
        }
    }
}
