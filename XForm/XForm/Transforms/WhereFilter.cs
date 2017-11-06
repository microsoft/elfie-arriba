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

        private IDataBatchTransform _comparer;
        private DataBatch _filterResult;

        public WhereEqualsFilter(IDataBatchSource source, string columnName, T value)
        {
            this._source = source;
            this._value = value;
            this._filterColumnGetter = source.ColumnGetter(source.Columns.IndexOfColumn(columnName));

            this._comparer = new ComparableEqualsComparer<int>((int)(object)_value);
            //this._comparer = new IntEqualsComparer(_value);
        }

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Call the inner getter and return the batch with only the matching rows mapped
            return () => _source.ColumnGetter(columnIndex)().Map(_filterResult.Indices, _filterResult.Count);
        }

        public bool Next(int desiredCount)
        {
            while(_source.Next(desiredCount))
            {
                // Get a batch of rows from the filter column
                DataBatch filterColumnBatch = _filterColumnGetter();

                // Get the filtered results
                _filterResult = _comparer.Transform(filterColumnBatch);

                // Stop if we got rows, otherwise get the next source batch
                if (_filterResult.Count > 0) return true;
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

    internal abstract class BaseComparer : IDataBatchTransform
    {
        protected int _currentIndicesCount;
        protected int[] _currentIndices;

        public virtual DataBatch Transform(DataBatch source)
        {
            // Ensure we have room for the map indices
            if (_currentIndices == null || _currentIndices.Length < source.Count) _currentIndices = new int[source.Count];

            // Reset output
            _currentIndicesCount = 0;

            // Run the actual where comparison
            TransformInternal(source);

            // Return a mapped DataBatch of the resulting matches
            return DataBatch.All(source.Array, source.Array.Length).Map(_currentIndices, _currentIndicesCount);
        }

        public abstract void TransformInternal(DataBatch source);
    }

    internal class ComparableEqualsComparer<T> : BaseComparer where T : IComparable<T>
    {
        private T _value;

        public ComparableEqualsComparer(T value)
        {
            _value = value;
        }

        public override void TransformInternal(DataBatch source)
        {
            T[] sourceArray = (T[])source.Array;
            for (int i = source.StartIndexInclusive; i < source.EndIndexExclusive; ++i)
            {
                int realIndex = i;
                if(source.Indices != null) realIndex = source.Indices[realIndex];
                if (_value.CompareTo(sourceArray[realIndex]) == 0) _currentIndices[_currentIndicesCount++] = i;
            }
        }
    }

    internal class IntEqualsComparer : BaseComparer
    {
        private int _value;

        public IntEqualsComparer(object value)
        {
            _value = (int)value;
        }

        public override void TransformInternal(DataBatch source)
        {
            int[] sourceArray = (int[])source.Array;
            for (int i = source.StartIndexInclusive; i < source.EndIndexExclusive; ++i)
            {
                int realIndex = i;
                if (source.Indices != null) realIndex = source.Indices[realIndex];
                if (sourceArray[realIndex] == _value) _currentIndices[_currentIndicesCount++] = i;
            }
        }
    }
}
