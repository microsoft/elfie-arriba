// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model;

namespace Arriba.Extensions
{
    public static class IColumnExtensions
    {
        /// <summary>
        ///  If the column contains a component of a given type (ex: IndexedColumn),
        ///  this will find that component and return it, allowing callers to access
        ///  properties exposed by that time but not the IColumn interface.
        /// </summary>
        /// <typeparam name="T">Type of Column component to search for</typeparam>
        /// <param name="column">Column to traverse</param>
        /// <returns>Column of type T in the chain of 'column', or null if none could be found</returns>
        public static T FindComponent<T>(this IColumn column)
        {
            IColumn current = column;

            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }

                current = (IColumn)current.InnerColumn;
            }

            return default(T);
        }
    }
}
