using Arriba.Structures;
using System;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    public interface IItemConsumer : IDisposable
    {
        /// <summary>
        ///  Write the next block of new or changed items to this consumer.
        /// </summary>
        /// <param name="items">Block of items to write</param>
        void Append(DataBlock items);

        /// <summary>
        ///  Ensure changes are committed to the underlying datastore.
        /// </summary>
        void Save();
    }
}
