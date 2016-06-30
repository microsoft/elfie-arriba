// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;

namespace Arriba.Extensions
{
    public static class ArrayExtensions
    {
        public enum ArraySizePolicy
        {
            /// <summary>
            /// Reduces memory consumption by resizing incrementally based on ArrayExtensions.GrowthFactor
            /// </summary>
            Conservative,

            /// <summary>
            /// Reduces allocations at the expensive of memory consumption by choosing maximum size for arrays where possible
            /// </summary>
            CreateAtSuggestedCapacity,
        }

        /// <summary>
        ///  Minimum Size to allocate arrays. This is equal to the object overhead in .NET in x64,
        ///  so a byte[] will be half overhead at this size.
        /// </summary>
        public const int MinimumSize = 24;

        /// <summary>
        ///  Minimum amount to increase an array. Expand() will increase by this value unless 
        ///  size * GrowthFactor is more.
        /// </summary>
        public const int MinimumGrowthAmount = 24;

        /// <summary>
        ///  Minimum Factor by which to grow. Expand() will increase by this percentage unless
        ///  MinimumGrowthAmount is more.
        /// </summary>
        public const double GrowthFactor = 0.2;

        /// <summary>
        /// Set the global sizing policy for newly created arrays
        /// </summary>
        public static ArraySizePolicy SizePolicy = ArraySizePolicy.Conservative;

        /// <summary>
        /// Creates a new array of a size recommended by the SizePolicy
        /// </summary>
        /// <typeparam name="T">Type of array items</typeparam>
        /// <param name="maximumSize">maximum size</param>
        /// <returns>new array sized appropriate to policy</returns>
        public static T[] CreateRecommendedArray<T>(ushort initialCapacity)
        {
            // never less than minimum size and always minimum size if size policy is conservative
            int initialSize = (SizePolicy == ArraySizePolicy.Conservative) ? MinimumSize : Math.Max(MinimumSize, (int)initialCapacity);
            T[] newArray = new T[initialSize];

            return newArray;
        }

        /// <summary>
        /// Grow the given array to accomodate the needed number of items.  This function will
        /// only expand arrays that cannot accomodate requiredSize it will not shrink arrays.
        /// </summary>
        /// <typeparam name="T">Type of array items</typeparam>
        /// <param name="array">Array to resize</param>
        /// <param name="requiredSize">Number of items which must fit in array</param>
        /// <param name="maximumSize">Maximum size for array</param>
        public static void Grow<T>(ref T[] array, int requiredSize, int maximumSize)
        {
            int existingLength = array.Length;

            if (requiredSize > existingLength)
            {
                Resize(ref array, requiredSize, maximumSize);
            }
        }

        /// <summary>
        ///  Resize the given array to accomodate the needed number of items, controlling
        ///  the number of resizes by resizing by a minimum amount and factor. The array
        ///  may be larger than the requiredSize.
        /// </summary>
        /// <typeparam name="T">Type of array items</typeparam>
        /// <param name="array">Array to resize</param>
        /// <param name="requiredSize">Number of items which must fit in array</param>
        /// <param name="maximumSize">Maximum size for array</param>
        public static void Resize<T>(ref T[] array, int requiredSize, int maximumSize)
        {
            int existingLength = array.Length;
            int recommendedSize = RecommendedSize(existingLength, requiredSize, maximumSize);
            if (recommendedSize == existingLength) return;

            Debug.Assert(recommendedSize >= MinimumSize && recommendedSize <= maximumSize);

            // Expand (or Shrink) and copy the array
            T[] newArray = new T[recommendedSize];
            Array.Copy(array, newArray, Math.Min(existingLength, recommendedSize));
            array = newArray;
        }

        /// <summary>
        ///  Recommend a size for an array, providing enough room for items but not resizing
        ///  too frequently.
        /// </summary>
        /// <param name="existingLength">Existing Array Size</param>
        /// <param name="requiredSize">Needed number of items</param>
        /// <param name="maximumSize">Maximum allowed size</param>
        /// <returns>Recommend size; same as current if a resize isn't worthwhile.</returns>
        public static int RecommendedSize(int existingLength, int requiredSize, int maximumSize)
        {
            // If the maximum is required, recommend that
            if (requiredSize >= maximumSize) return maximumSize;

            if (existingLength < requiredSize)
            {
                // If the array is too small, increase the size by at least MinimumGrowthAmount, at least GrowthFactor, and at least enough to fit the new items
                int increaseBy = requiredSize - existingLength;
                if (increaseBy < MinimumGrowthAmount) increaseBy = MinimumGrowthAmount;
                if (increaseBy < existingLength * GrowthFactor) increaseBy = (int)(existingLength * GrowthFactor);

                // Don't resize beyond maximum size
                if (existingLength + increaseBy > maximumSize) increaseBy = maximumSize - existingLength;

                return existingLength + increaseBy;
            }
            else if (existingLength > requiredSize)
            {
                // If the array is too big, decrease by at least MinimumGrowthAmount, at least GrowthFactor would be saved, and at least enough to fit the items
                int shrinkBy = existingLength - requiredSize;
                if (shrinkBy < MinimumGrowthAmount) shrinkBy = MinimumGrowthAmount;
                if (shrinkBy < existingLength * GrowthFactor) shrinkBy = (int)(existingLength * (1 - GrowthFactor));

                // Don't resize below minimum size
                if (existingLength - shrinkBy < MinimumSize) shrinkBy = existingLength - MinimumSize;

                // If shrinking by at least this much is still enough to fit our items, suggest it
                if (existingLength - shrinkBy >= requiredSize) return existingLength - shrinkBy;
            }

            // Otherwise, suggest no change
            return existingLength;
        }

        /// <summary>
        ///  Extract a page of items from an Array, given a start index and length.
        ///  If there are not enough items left, an array with the remaining items 
        ///  after 'index' will be returned.
        /// </summary>
        /// <param name="source">Source Array</param>
        /// <param name="index">Index of first item to return</param>
        /// <param name="length">Number of items to return</param>
        /// <returns>Array with up to 'length' items starting from source[index]</returns>
        public static Array Page(this Array source, int index, int length)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (length == 0) return new object[0];
            if (index < 0 || index >= source.Length) throw new ArgumentOutOfRangeException("index");
            if (length < 0) throw new ArgumentOutOfRangeException("length");

            int countToReturn = Math.Min(length, source.Length - index);

            Array result = new object[countToReturn];
            for (int i = 0; i < countToReturn; ++i)
            {
                result.SetValue(source.GetValue(i + index), i);
            }

            return result;
        }

        /// <summary>
        ///  Join is String.Join for untyped arrays.
        /// </summary>
        /// <param name="source">Array to join into a string</param>
        /// <param name="delimiter">Delimiter between items</param>
        /// <returns>String of all item.ToString values joined by delimiters</returns>
        public static string Join(this Array source, string delimiter)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < source.Length; ++i)
            {
                if (i > 0) result.Append(delimiter);
                result.Append(source.GetValue(i));
            }

            return result.ToString();
        }
    }
}
