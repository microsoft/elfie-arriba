// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Monitoring
{
    /// <summary>
    /// Event monitor scope. 
    /// </summary>
    public class MonitorEventScope : IDisposable
    {
        private TimingHandle _timing;

        internal MonitorEventScope(MonitorEventEntry start)
        {
            _timing = new TimingHandle();
            this.Start = start;
        }

        /// <summary>
        /// Gets the start event of the scope.
        /// </summary>
        public MonitorEventEntry Start { get; private set; }

        /// <summary>
        /// Gets the end event of the scope. 
        /// </summary>
        public MonitorEventEntry Stop { get; private set; }

        /// <summary>
        /// Gets the current runtime of the scope in milliseconds. 
        /// </summary>
        public double CurrentRuntime
        {
            get
            {
                return _timing.ElapsedMiliseconds;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes and ends of the event scope. 
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _timing.Dispose();

            var endEvent = this.Start.Clone();
            endEvent.OpCode = MonitorEventOpCode.Stop;
            endEvent.RuntimeMilliseconds = _timing.ElapsedMiliseconds;
            this.Stop = endEvent;

            EventPublisher.Raise(endEvent);
        }
    }
}
