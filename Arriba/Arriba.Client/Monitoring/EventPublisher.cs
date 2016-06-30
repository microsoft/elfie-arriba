// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Arriba Event Publisher. 
    /// </summary>
    public static class EventPublisher
    {
        private static int s_consumerCount = 0;
        internal static readonly Dictionary<int, List<IMonitorEventConsumer>> FlagToConsumerLookup = new Dictionary<int, List<IMonitorEventConsumer>>();
        private static readonly EventPublisherSource s_defaultSource;

        static EventPublisher()
        {
            s_defaultSource = new EventPublisherSource(new MonitorEventEntry()
            {
                Source = "Engine",
                Level = MonitorEventLevel.Verbose,
                OpCode = MonitorEventOpCode.Mark,
                EntityType = "Instance",
                EntityIdentity = "0"
            });

            // Create all possible "slots" for event consumers. 
            foreach (var opCode in Enum.GetValues(typeof(MonitorEventOpCode)).Cast<short>())
            {
                foreach (var level in Enum.GetValues(typeof(MonitorEventLevel)).Cast<short>())
                {
                    var key = CombineFlags(opCode, level);
                    FlagToConsumerLookup.Add(key, new List<IMonitorEventConsumer>());
                }
            }
        }

        internal static EventPublisherSource Default
        {
            get
            {
                return s_defaultSource;
            }
        }

        /// <summary>
        /// Creates an event source for publishing events. 
        /// </summary>
        /// <param name="source">Event source name.</param>
        /// <returns>Event publisher source.</returns>
        public static EventPublisherSource CreateEventSource(string source)
        {
            return new EventPublisherSource(new MonitorEventEntry() { Source = source });
        }

        /// <summary>
        /// Creates an event source for publishing events. 
        /// </summary>
        /// <param name="defaults">Default event properties.</param>
        /// <returns>Event publisher source.</returns>
        public static EventPublisherSource CreateEventSource(MonitorEventEntry defaults)
        {
            return new EventPublisherSource(defaults);
        }

        /// <summary>
        /// Adds an event consumer. 
        /// </summary>
        /// <param name="consumer">Event consumer instance.</param>
        /// <returns>Disposable handle to unsubscribe consumer.</returns>
        public static IDisposable AddConsumer(IMonitorEventConsumer consumer)
        {
            Interlocked.Increment(ref s_consumerCount);
            return new ConsumerHandle(consumer, () => Interlocked.Decrement(ref s_consumerCount));
        }

        internal static MonitorEventEntry Raise(MonitorEventEntry monitorEvent)
        {
            if (Interlocked.CompareExchange(ref s_consumerCount, 0, 0) == 0)
            {
                return monitorEvent;
            }

            List<IMonitorEventConsumer> result;

            if (FlagToConsumerLookup.TryGetValue(CombineFlags(monitorEvent), out result))
            {
                lock (result)
                {
                    foreach (var item in result)
                    {
                        item.OnEvent(monitorEvent);
                    }
                }
            }

            return monitorEvent;
        }

        internal static MonitorEventScope RaiseScope(MonitorEventEntry startEvent)
        {
            var timingHandle = new TimingHandle();

            startEvent.OpCode = MonitorEventOpCode.Start;
            Raise(startEvent);

            return new MonitorEventScope(startEvent);
        }


        internal static void Shutdown()
        {
            FlagToConsumerLookup.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MonitorEventEntry EventFromArgs(MonitorEventLevel flags, MonitorEventOpCode opCode, string entityType, string entityIdentity, string name, string user, string detail, string source, double runtime)
        {
            return new MonitorEventEntry()
            {
                TimeStamp = DateTime.UtcNow,
                OpCode = opCode,
                Level = flags,
                Name = name,
                EntityType = entityType,
                EntityIdentity = entityIdentity,
                User = user,
                Detail = detail,
                Source = source,
                RuntimeMilliseconds = runtime
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CombineFlags(short opCode, short level)
        {
            return ((opCode << 16) | (level & 0xFFFF));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CombineFlags(MonitorEventEntry e)
        {
            return CombineFlags((short)e.OpCode, (short)e.Level);
        }
    }
}

