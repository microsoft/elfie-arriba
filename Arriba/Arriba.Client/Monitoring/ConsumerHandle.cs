// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Wrapper class for management of event consumers. 
    /// </summary>
    internal class ConsumerHandle : IDisposable
    {
        public int flagKey;
        public IMonitorEventConsumer Consumer;

        // Stores a lookup of the the Flag to list lookup for which this consumer is placed in 
        // the EventPublisher class. This exists to the consumer handle can remove the wrapped
        // consumer for the EventPublisher, without the need to scan all flag entry sets. 
        private Queue<List<IMonitorEventConsumer>> _flagEntries = new Queue<List<IMonitorEventConsumer>>();
        private Action _disposeCallback;

        public ConsumerHandle(IMonitorEventConsumer consumer, Action disposeCallback)
        {
            this.Consumer = consumer;
            this.Consumer.OnNotifyLevelChange += this.OnConsumerFlagsChanged;
            _disposeCallback = disposeCallback;
            this.ReadFlags();
        }

        private void OnConsumerFlagsChanged(object sender, EventArgs e)
        {
            this.ReadFlags();
        }

        private void ReadFlags()
        {
            var newFlags = EventPublisher.CombineFlags((short)this.Consumer.NotifyOnOpCodeFlags, (short)this.Consumer.NotifyOnEventFlags);

            if (newFlags != this.flagKey)
            {
                this.flagKey = newFlags;

                this.ClearSubscriptions();

                // Build new entries  
                foreach (var keyPair in EventPublisher.FlagToConsumerLookup)
                {
                    if ((this.flagKey & keyPair.Key) == keyPair.Key)
                    {
                        keyPair.Value.Add(this.Consumer);
                        _flagEntries.Enqueue(keyPair.Value);
                    }
                }
            }
        }

        private void ClearSubscriptions()
        {
            // Clear old entires
            while (_flagEntries.Count > 0)
            {
                var list = _flagEntries.Dequeue();

                lock (list)
                {
                    list.Remove(this.Consumer);
                }
            }
        }

        public void Dispose()
        {
            Consumer.OnNotifyLevelChange -= this.OnConsumerFlagsChanged;
            this.ClearSubscriptions();
            this.Consumer.Dispose();

            if (_disposeCallback != null)
            {
                _disposeCallback();
            }
        }
    }
}
