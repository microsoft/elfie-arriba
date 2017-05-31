// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Column;
using Arriba.Model.Security;
using Arriba.Structures;

namespace Arriba.TfsWorkItemCrawler.ItemConsumers
{
    public class ComposedItemConsumer : IItemConsumer
    {
        private IEnumerable<IItemConsumer> _innerConsumers;

        public ComposedItemConsumer(params IItemConsumer[] innerConsumers)
        {
            _innerConsumers = innerConsumers;
        }

        public void CreateTable(IList<ColumnDetails> columns, SecurityPermissions permissions)
        {
            foreach (IItemConsumer consumer in _innerConsumers)
            {
                consumer.CreateTable(columns, permissions);
            }
        }

        public void Append(DataBlock items)
        {
            foreach (IItemConsumer consumer in _innerConsumers)
            {
                consumer.Append(items);
            }
        }

        public void Save()
        {
            foreach (IItemConsumer consumer in _innerConsumers)
            {
                consumer.Save();
            }
        }

        public void Dispose()
        {
            if (_innerConsumers != null)
            {
                foreach (IItemConsumer consumer in _innerConsumers)
                {
                    consumer.Dispose();
                }

                _innerConsumers = null;
            }
        }
    }
}
