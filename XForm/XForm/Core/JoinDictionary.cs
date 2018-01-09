using System.Collections.Generic;
using XForm.Data;
using XForm.Types;

namespace XForm
{
    public interface IJoinDictionary
    {
        void Add(DataBatch keys, int firstRowIndex);
        BitVector TryGetValues(DataBatch keys, out DataBatch foundAtIndices);
    }

    public class EqualityComparerAdapter<T> : IEqualityComparer<T>
    {
        private IDataBatchComparer<T> _inner;

        public EqualityComparerAdapter(IDataBatchComparer inner)
        {
            _inner = (IDataBatchComparer<T>)inner;
        }

        public bool Equals(T left, T right)
        {
            return _inner.WhereEqual(left, right);
        }

        public int GetHashCode(T value)
        {
            return _inner.GetHashCode(value);
        }
    }

    public class JoinDictionary<T> : IJoinDictionary
    {
        // JoinDictionary uses a Dictionary5 internally
        private Dictionary5<T, int> _dictionary;

        // Reused buffers for the matching row vector and matching row right side indices
        private int[] _returnedIndicesBuffer;
        private BitVector _returnedVector;

        public JoinDictionary(int initialCapacity)
        {
            IEqualityComparer<T> comparer = new EqualityComparerAdapter<T>(TypeProviderFactory.TryGet(typeof(T).Name).TryGetComparer());
            _dictionary = new Dictionary5<T, int>(comparer, initialCapacity);
        }

        public void Add(DataBatch keys, int firstRowIndex)
        {
            T[] keyArray = (T[])keys.Array;
            for (int i = 0; i < keys.Count; ++i)
            {
                int index = keys.Index(i);
                if (keys.IsNull != null && keys.IsNull[index]) continue;
                _dictionary[keyArray[index]] = firstRowIndex + i;
            }
        }

        public BitVector TryGetValues(DataBatch keys, out DataBatch foundAtIndices)
        {
            Allocator.AllocateToSize(ref _returnedVector, keys.Count);
            Allocator.AllocateToSize(ref _returnedIndicesBuffer, keys.Count);

            _returnedVector.None();

            int countFound = 0;
            T[] keyArray = (T[])keys.Array;
            for (int i = 0; i < keys.Count; ++i)
            {
                int index = keys.Index(i);
                int foundAtIndex;
                if ((keys.IsNull != null && keys.IsNull[index]) || !_dictionary.TryGetValue(keyArray[index], out foundAtIndex))
                {
                    _returnedVector.Clear(i);
                }
                else
                {
                    _returnedVector.Set(i);
                    _returnedIndicesBuffer[countFound++] = foundAtIndex;
                }
            }

            // Write out the indices of the joined rows for each value found
            foundAtIndices = DataBatch.All(_returnedIndicesBuffer, countFound);

            // Return the vector of which input rows matched
            return _returnedVector;
        }
    }
}
