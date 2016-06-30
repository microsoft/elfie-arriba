// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Arriba.Structures
{
    /// <summary>
    ///  Untyped interface to UniqueValueMerger<T>
    /// </summary>
    internal interface IUniqueValueMerger
    {
        void Add(System.Collections.IEnumerable source);
        Array GetUniqueValues(int count);
        int Count { get; }
    }

    /// <summary>
    /// Merges multiple sources and returns back the set of unique elements
    /// </summary>
    internal class UniqueValueMerger<T> : IUniqueValueMerger where T : IEquatable<T>
    {
        private HashSet<T> _hashTable;

        public UniqueValueMerger()
        {
            _hashTable = new HashSet<T>();
        }

        public void Add(System.Collections.IEnumerable source)
        {
            IEnumerable<T> typedSource = source as IEnumerable<T>;

            if (typedSource != null)
            {
                _hashTable.UnionWith(typedSource);
            }
        }

        public Array GetUniqueValues(int desiredCount)
        {
            // TODO: do we need the values sorted before pulling top N?

            int count = _hashTable.Count;

            if (desiredCount != 0 && desiredCount < count)
            {
                count = desiredCount;
            }

            T[] values = new T[count];

            _hashTable.CopyTo(values, 0, count);

            return values;
        }

        public int Count
        {
            get { return _hashTable.Count; }
        }
    }
}
