// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Arriba.Model
{
    /// <summary>
    /// DistributionDictionary allows you to easily contain distributions in the form
    /// of occurrence counts per value. For example, if tracking the distribution of
    /// the number of favorites people have, you can create a DistributionDictionary
    /// and then, for each person, call Increment(numberOfFavoritesForThisPerson).
    /// </summary>
    /// <typeparam name="T">Type of values we're counting occurrences of</typeparam>
    public class DistributionDictionary<T>
    {
        private Dictionary<T, int> CountPerValue { get; set; }

        public DistributionDictionary()
        {
            this.CountPerValue = new Dictionary<T, int>();
        }

        /// <summary>
        ///  Increment the count of occurrences for 'value';
        ///  adds to the set if empty or increments.
        /// </summary>
        /// <param name="value"></param>
        public void Increment(T value)
        {
            int count = 0;
            this.CountPerValue.TryGetValue(value, out count);
            this.CountPerValue[value] = count + 1;
        }

        /// <summary>
        ///  Get the set of distinct values we've seen occurrences of.
        /// </summary>
        public IEnumerable<T> Keys
        {
            get { return this.CountPerValue.Keys; }
        }

        public IEnumerable<KeyValuePair<T, int>> Pairs
        {
            get { return this.CountPerValue; }
        }

        /// <summary>
        ///  Get the number of occurrences reported for a specific value.
        /// </summary>
        /// <param name="key">Value to check</param>
        /// <returns>Occurrences for value or zero</returns>
        public int this[T key]
        {
            get
            {
                int count = 0;
                this.CountPerValue.TryGetValue(key, out count);
                return count;
            }
        }
    }
}
