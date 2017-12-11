// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace XForm.Data
{
    /// <summary>
    ///  Allocator provides simple just-in-time allocation for XForm components.
    /// </summary>
    public static class Allocator
    {
        /// <summary>
        ///  Allocate the given array to at least the required size if it isn't already big enough.
        ///  This method ensures allocations only happen once we know the array is needed, and as
        ///  long as the size stays the same across pages, that the allocation only happens once.
        ///  
        ///  The array may be longer than minimumSize after this call, so users of the array have to
        ///  pass along the 'valid length' with the array.
        /// </summary>
        /// <typeparam name="T">Type of array elements</typeparam>
        /// <param name="array">Array reference to ensure is the minimum size</param>
        /// <param name="minimumSize">Minimum size Array will be after the call. It may be larger.</param>
        public static void AllocateToSize<T>(ref T[] array, int minimumSize)
        {
            if (array == null || array.Length < minimumSize) array = new T[minimumSize];
        }

        /// <summary>
        ///  Generically create an array of a given type and desired size.
        /// </summary>
        /// <param name="elementType">Type of Array elements</param>
        /// <param name="length">Length of array to create</param>
        /// <returns>Strongly typed array of length 'length' with elements of type 'elementType'</returns>
        public static Array AllocateArray(Type elementType, int length)
        {
            ConstructorInfo ctor = elementType.MakeArrayType().GetConstructor(new Type[] { typeof(int) });
            return (Array)ctor.Invoke(new object[] { length });
        }
    }
}
