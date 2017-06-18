// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Column;
using Arriba.Structures;

namespace Arriba.TfsWorkItemCrawler.ItemProviders
{
    public interface IItemProvider : IDisposable
    {
        /// <summary>
        ///  Return a list of the available columns from this source.
        /// </summary>
        /// <returns></returns>
        IList<ColumnDetails> GetColumns();

        /// <summary>
        ///  Return the Identities of all items changed within a given time range.
        /// </summary>
        /// <param name="start">Start of time range</param>
        /// <param name="end">End of time range</param>
        /// <returns>ItemIdentity [ID and ChangedDate] for all items changed within the range.</returns>
        List<ItemIdentity> GetItemsChangedBetween(DateTime start, DateTime end);

        /// <summary>
        ///  Return the requested columns for the requested items in a DataBlock
        ///  in the order they were passed. Return null when an item doesn't have
        ///  the selected column.
        /// </summary>
        /// <param name="items">Identities of items to return</param>
        /// <param name="columnNames">Names of columns to return</param>
        /// <returns>DataBlock with desired columns for desired items</returns>
        DataBlock GetItemBlock(IEnumerable<ItemIdentity> items, IEnumerable<string> columnNames);
    }
}
