// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Used to raise events from external components with predefined defaults.  
    /// </summary>
    public class EventPublisherSource
    {
        private MonitorEventEntry _defaults;

        internal EventPublisherSource(MonitorEventEntry defaults)
        {
            _defaults = defaults;
        }

        /// <summary>
        /// Raises an event. 
        /// </summary>
        /// <param name="level">Event level.</param>
        /// <param name="opcode">Event opcode.</param>
        /// <param name="name">Event name.</param>
        /// <param name="user">Event User Identity.</param>
        /// <param name="detail">Event detail.</param>
        /// <param name="runtime">Runtime in milliseconds.</param>
        /// <returns>Raised event.</returns>
        public MonitorEventEntry Raise(MonitorEventLevel? level = null, MonitorEventOpCode? opcode = null, string entityType = null, string entityIdentity = null, string name = null, string user = null, string detail = null, double? runtime = null)
        {
            return EventPublisher.Raise(CreateEventFromArgs(level, opcode, entityType, entityIdentity, name, user, detail, runtime));
        }

        /// <summary>
        /// Creates an event scope to raise Start and End opcode events. 
        /// </summary>
        /// <param name="level">Event level.</param>
        /// <param name="name">Event name.</param>
        /// <param name="user">Event User Identity.</param>
        /// <param name="detail">Event detail.</param>
        /// <returns>Disposable handle.</returns>
        public MonitorEventScope RaiseScope(MonitorEventLevel? level = null, string entityType = null, string entityIdentity = null, string name = null, string user = null, string detail = null)
        {
            return EventPublisher.RaiseScope(CreateEventFromArgs(level, MonitorEventOpCode.Unknown, entityType, entityIdentity, name, user, detail, 0.0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MonitorEventEntry CreateEventFromArgs(MonitorEventLevel? level, MonitorEventOpCode? opcode, string entityType, string entityIdentity, string name, string user, string detail, double? runtime)
        {
            return new MonitorEventEntry()
            {
                TimeStamp = DateTime.UtcNow,
                Level = level ?? _defaults.Level,
                OpCode = opcode ?? _defaults.OpCode,
                Source = _defaults.Source ?? "Unknown",
                Name = name ?? _defaults.Name ?? "Unknown",
                EntityType = entityType ?? _defaults.EntityType ?? "Unknown",
                EntityIdentity = entityIdentity ?? _defaults.EntityIdentity ?? "Unknown",
                Detail = detail ?? _defaults.Detail ?? "",
                User = user ?? _defaults.User ?? "Unknown",
                RuntimeMilliseconds = runtime ?? 0.0
            };
        }
    }
}
