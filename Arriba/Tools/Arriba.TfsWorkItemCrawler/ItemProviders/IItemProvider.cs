using Arriba.Structures;
using System;
using System.Collections.Generic;

namespace Arriba.TfsWorkItemCrawler.ItemProviders
{
    public interface IItemProvider : IDisposable
    {
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
        DataBlock GetItemBlock(IEnumerable<ItemIdentity> items, IReadOnlyList<string> columnNames);
    }
}
