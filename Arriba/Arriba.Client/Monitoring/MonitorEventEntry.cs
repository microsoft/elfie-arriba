// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Event opcode enumeration.
    /// </summary>
    [Flags]
    public enum MonitorEventOpCode : short
    {
        Unknown = 0x0,

        Mark = 0x1,
        Start = 0x2,
        Stop = 0x4,

        All = Mark | Start | Stop
    }

    /// <summary>
    /// Event level enumeration. 
    /// </summary>
    [Flags]
    public enum MonitorEventLevel : short
    {
        Unknown = 0x0,

        Verbose = 0x1,
        Information = 0x2,
        Warning = 0x4,
        Error = 0x8,

        All = Verbose | Information | Warning | Error
    }

    /// <summary>
    /// Raised event. 
    /// </summary>
    public class MonitorEventEntry
    {
        public MonitorEventEntry()
        {
            this.Level = MonitorEventLevel.Information;
            this.OpCode = MonitorEventOpCode.Mark;
        }

        /// <summary>
        /// Gets or sets the timestamp of the event. 
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the opcode of the event. 
        /// </summary>
        public MonitorEventOpCode OpCode { get; set; }

        /// <summary>
        /// Gets or sets the event level. 
        /// </summary>
        public MonitorEventLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the target type.
        /// </summary>
        /// <remarks>
        /// This denotes the logical database entity the event targets, e.g. "Table" or "Metadata" 
        /// </remarks>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the target entity identity.
        /// </summary>
        /// <remarks>
        /// This denotes the unique identity of the event target within the type scope. e.g. "MyTable" 
        /// </remarks>
        public string EntityIdentity { get; set; }

        /// <summary>
        /// Gets or sets the event name. 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user identity for the event (if any). 
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the detail message for the event. 
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// Gets or sets the source of the event.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the runtime (execution time) of the event. 
        /// </summary>
        public double RuntimeMilliseconds { get; set; }

        internal MonitorEventEntry Clone()
        {
            return new MonitorEventEntry()
            {
                TimeStamp = this.TimeStamp,
                OpCode = this.OpCode,
                Level = this.Level,
                Name = this.Name,
                User = this.User,
                Detail = this.Detail,
                Source = this.Source,
                EntityType = this.EntityType,
                EntityIdentity = this.EntityIdentity,
                RuntimeMilliseconds = this.RuntimeMilliseconds
            };
        }
    }
}
