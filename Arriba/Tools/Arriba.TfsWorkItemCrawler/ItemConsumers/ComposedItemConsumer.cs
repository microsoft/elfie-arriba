using Arriba.Structures;
using System.Collections.Generic;
using Arriba.Model.Column;
using Arriba.Model.Security;
using System;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    public class ComposedItemConsumer : IItemConsumer
    {
        private IEnumerable<IItemConsumer> InnerConsumers;

        public ComposedItemConsumer(params IItemConsumer[] innerConsumers)
        {
            this.InnerConsumers = innerConsumers;
        }

        public void CreateTable(IList<ColumnDetails> columns, SecurityPermissions permissions)
        {
            foreach (IItemConsumer consumer in this.InnerConsumers)
            {
                consumer.CreateTable(columns, permissions);
            }
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
