using System;
using System.Collections.Generic;
using XForm.Data;
using XForm.Extensions;

namespace XForm.Transforms
{
    public class WhereEqualsFilter<T> : IDataBatchSource where T : IComparable<T>
    {
        private IDataBatchSource _source;
        private T _value;

        private Func<DataBatch> _filterColumnGetter;
        private int[] _currentIndices;
        private int _currentIndicesCount;

        public WhereEqualsFilter(IDataBatchSource source, string columnName, T value)
        {
            this._source = source;
            this._value = value;
            this._filterColumnGetter = source.ColumnGetter(source.Columns.IndexOfColumn(columnName));
        }

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Call the inner getter and return the batch with only the matching rows mapped
            return () => _source.ColumnGetter(columnIndex)().Map(_currentIndices, _currentIndicesCount);
        }

        public bool Next(int desiredCount)
        {
            if (_currentIndices == null || _currentIndices.Length < desiredCount) _currentIndices = new int[desiredCount];
            _currentIndicesCount = 0;

            while(_source.Next(desiredCount))
            {
                // Get a batch of rows from the filter column
                DataBatch filterColumnBatch = _filterColumnGetter();

                // Loop over and include indices for all matching files
                T[] array = (T[])filterColumnBatch.Array;
                if (filterColumnBatch.Indices == null)
                {
                    // :/
                    for (int i = filterColumnBatch.StartIndexInclusive; i < filterColumnBatch.EndIndexExclusive; ++i)
                    {
                        int realIndex = i;
                        if (_value.CompareTo(array[realIndex]) == 0)
                        {
                            _currentIndices[_currentIndicesCount] = realIndex;
                            _currentIndicesCount++;
                        }
                    }
                }
                else
                {
                    for (int i = filterColumnBatch.StartIndexInclusive; i < filterColumnBatch.EndIndexExclusive; ++i)
                    {
                        int realIndex = filterColumnBatch.Indices[i];
                        if (_value.CompareTo(array[realIndex]) == 0)
                        {
                            _currentIndices[_currentIndicesCount] = realIndex;
                            _currentIndicesCount++;
                        }
                    }
                }

                if (_currentIndicesCount > 0) return true;
            }

            return false;
        }

        public void Dispose()
        {
            if(_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }
}
