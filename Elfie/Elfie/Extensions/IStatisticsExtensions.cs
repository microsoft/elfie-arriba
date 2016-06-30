// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace Microsoft.CodeAnalysis.Elfie.Extensions
{
    public static class IStatisticsExtensions
    {
        /// <summary>
        ///  Write the count and memory size of an item implementing IStatistics in a compact,
        ///  human readable form.
        /// </summary>
        /// <param name="item">Item implementing IStatistics to report</param>
        /// <param name="itemNamePlural">The name of the entity the count represents, if it should be logged</param>
        /// <returns></returns>
        public static string ToStatisticsString(this IStatistics item, string itemNamePlural = null)
        {
            if (String.IsNullOrEmpty(itemNamePlural))
            {
                return String.Format("{0:n0}, [{1}]", item.Count, item.Bytes.SizeString());
            }
            else
            {
                return String.Format("{0:n0} {1}, [{2}]", item.Count, itemNamePlural, item.Bytes.SizeString());
            }
        }
    }
}
