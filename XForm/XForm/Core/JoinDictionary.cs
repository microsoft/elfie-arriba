using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Reflection;
using XForm.Data;

// Fix TODOs!
//  Add large scale performance test in PerformanceComparisons. 1M join to 100k with 90% match rate?
namespace XForm
{
    public class String8EqualityComparer : IEqualityComparer<String8>
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

    public class IntEqualityComparer : IEqualityComparer<int>
    {
        public bool Equals(int left, int right)
        {
            return left == right;
        }

        public int GetHashCode(int value)
        {
            return unchecked((int)Hashing.Hash(value, 0));
        }
    }

    // TODO: Use DataBatch comparer infrastructure instead
    //  - Need to add GetHashCode to IDataBatchComparer.
    //  - Likely slow until Dictionary hashes and compares in bulk.
    public static class ComparerFactory
    {
        public static object BuildComparer<T>(string name)
        {
            if (String.IsNullOrEmpty(name)) name = typeof(T).Name;

            switch(name.ToLowerInvariant())
            {
                case "string8":
                    return new String8EqualityComparer();
                case "int":
                case "int32":
                    return new IntEqualityComparer();
                default:
                    throw new NotImplementedException($"No IEqualityComparer known with name \"{name}\".");
            }
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

        // TODO: Move this method to a non-generic class container. Allocator?
        public static IJoinDictionary BuildTypedJoinDictionary(Type elementType, int capacity, string comparerName)
        {
            Type typedDictionary = typeof(JoinDictionary<>).MakeGenericType(elementType);
            ConstructorInfo ctor = typedDictionary.GetConstructor(new Type[] { typeof(int), typeof(string) });
            return (IJoinDictionary)ctor.Invoke(new object[] { capacity, comparerName });
        }

        public JoinDictionary(int initialCapacity) : this(initialCapacity, null)
        { }

        public JoinDictionary(int initialCapacity, string comparerName)
        {
            IEqualityComparer<T> comparer = (IEqualityComparer<T>)ComparerFactory.BuildComparer<T>(comparerName);

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
