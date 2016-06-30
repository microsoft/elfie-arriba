// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Arriba.Serialization
{
    public static class CollectionFactory
    {
        /// <summary>
        ///  Return a List&lt;T&gt; for a passed type T.
        /// </summary>
        /// <param name="t">Type of List to create</param>
        /// <returns>An empty List of T</returns>
        public static IList BuildList(Type t)
        {
            Type listTypeToCreate = typeof(List<>).MakeGenericType(t);
            ConstructorInfo ci = listTypeToCreate.GetConstructor(new Type[0]);
            return (IList)(ci.Invoke(new object[0]));
        }

        /// <summary>
        ///  Return a T[] for a passed type T.
        /// </summary>
        /// <param name="t">Type of Array to create</param>
        /// <param name="length">Length to create</param>
        /// <returns>An empty T[]</returns>
        public static Array BuildArray(Type t, int length)
        {
            Type arrayTypeToCreate = t.MakeArrayType();
            ConstructorInfo ci = arrayTypeToCreate.GetConstructor(new Type[] { typeof(int) });
            return (Array)(ci.Invoke(new object[] { length }));
        }
    }
}
