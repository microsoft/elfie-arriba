using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System.Collections.Generic;
using XForm.Data;

namespace XForm
{
    public class String8Comparer : IEqualityComparer<String8>
    {
        public bool Equals(String8 left, String8 right)
        {
            return String8.Equals(left, right);
        }

        public int GetHashCode(String8 value)
        {
            return unchecked((int)Hashing.Hash(value, 0));
        }
    }

    public interface IJoinDictionary
    {
        void Add(DataBatch keys, int firstRowIndex);
        BitVector TryGetValues(DataBatch keys, out DataBatch foundAtIndices);
    }

    public class JoinDictionary<T> : IJoinDictionary
    {
        //private T[] _keys;
        //private int[] _values;
        //private byte[] _metadata;

        private Dictionary<T, int> _dictionary;
        private IEqualityComparer<T> _comparer;

        private int[] _returnedIndicesBuffer;
        private BitVector _returnedVector;

        public JoinDictionary(int initialCapacity, IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, int>(initialCapacity, comparer);
            _comparer = comparer;
        }

        public void Add(DataBatch keys, int firstRowIndex)
        {
            T[] keyArray = (T[])keys.Array;
            for(int i = 0; i < keys.Count; ++i)
            {
                int index = keys.Index(i);
                if(keys.IsNull != null && keys.IsNull[index]) continue;
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
