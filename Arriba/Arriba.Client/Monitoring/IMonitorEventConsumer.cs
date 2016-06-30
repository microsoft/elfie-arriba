// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Defines a mechanism for consumption of events.
    /// </summary>
    public interface IMonitorEventConsumer : IDisposable
    {
        /// <summary>
        /// Event raised when the consumers notification level changes.
        /// </summary>
        event EventHandler OnNotifyLevelChange;

        /// <summary>
        /// Gets the event level flags to consume.  
        /// </summary>
        MonitorEventLevel NotifyOnEventFlags { get; }

        /// <summary>
        /// Gets the event opcode level to consume. 
        /// </summary>
        MonitorEventOpCode NotifyOnOpCodeFlags { get; }

        /// <summary>
        /// Callback raised even a matching event is raised.
        /// </summary>
        /// <param name="e">Event raised.</param>
        void OnEvent(MonitorEventEntry e);
    }
}
