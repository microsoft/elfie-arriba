// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Arriba.Monitoring
{
    public abstract class BufferedEventConsumer : IMonitorEventConsumer
    {
        public event EventHandler OnNotifyLevelChange;

        private BufferBlock<MonitorEventEntry> _buffer;
        private ActionBlock<MonitorEventEntry> _action;

        protected BufferedEventConsumer(bool asyncCallback)
        {
            _buffer = new BufferBlock<MonitorEventEntry>();

            if (asyncCallback)
            {
                _action = new ActionBlock<MonitorEventEntry>(new Func<MonitorEventEntry, Task>(this.OnBufferedEventAsync));
            }
            else
            {
                _action = new ActionBlock<MonitorEventEntry>(new Action<MonitorEventEntry>(this.OnBufferedEvent));
            }

            _buffer.LinkTo(_action, new DataflowLinkOptions() { PropagateCompletion = true });
        }

        public abstract MonitorEventLevel NotifyOnEventFlags
        {
            get;
        }

        public abstract MonitorEventOpCode NotifyOnOpCodeFlags
        {
            get;
        }

        public void OnEvent(MonitorEventEntry e)
        {
            _buffer.Post(e);
        }

        protected virtual Task OnBufferedEventAsync(MonitorEventEntry e)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnBufferedEvent(MonitorEventEntry e)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _buffer.Complete();
            _action.Completion.Wait();
        }
    }
}
