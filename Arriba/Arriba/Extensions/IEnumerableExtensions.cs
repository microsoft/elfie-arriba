// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Arriba.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        ///  Return the items in 'source' in pages of size 'size'.
        /// </summary>
        /// <typeparam name="T">Type of item collection</typeparam>
        /// <param name="source">Source of items to page</param>
        /// <param name="size">Item count desired per page</param>
        /// <returns>Pages of size 'size' from 'source' in the original order, followed by a final page of any remaining items.</returns>
        public static IEnumerable<IList<T>> Page<T>(this IEnumerable<T> source, int size)
        {
            List<T> page = new List<T>();

            foreach (T item in source)
            {
                page.Add(item);

                if (page.Count == size)
                {
                    yield return page;
                    page = new List<T>();
                }
            }

            if (page.Count > 0)
            {
                yield return page;
            }
        }
    }
}
