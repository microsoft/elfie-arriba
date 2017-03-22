using Arriba.Structures;
using System.Collections.Generic;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    public class ComposedItemConsumer : IItemConsumer
    {
        private IEnumerable<IItemConsumer> InnerConsumers;

        public ComposedItemConsumer(params IItemConsumer[] innerConsumers)
        {
            this.InnerConsumers = innerConsumers;
        }

        public void Append(DataBlock items)
        {
            foreach(IItemConsumer consumer in this.InnerConsumers)
            {
                consumer.Append(items);
            }
        }

        public void Save()
        {
            foreach (IItemConsumer consumer in this.InnerConsumers)
            {
                consumer.Save();
            }
        }

        public void Dispose()
        {
            if(this.InnerConsumers != null)
            {
                foreach(IItemConsumer consumer in this.InnerConsumers)
                {
                    consumer.Dispose();
                }

                this.InnerConsumers = null;
            }
        }
    }
}
