// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Arriba.Monitoring
{
    public class ConsoleEventConsumer : BufferedEventConsumer
    {
        public ConsoleEventConsumer() : base(true)
        { }

        public override MonitorEventLevel NotifyOnEventFlags
        {
            get
            {
                return MonitorEventLevel.Error | MonitorEventLevel.Information | MonitorEventLevel.Warning;
            }
        }

        public override MonitorEventOpCode NotifyOnOpCodeFlags
        {
            get
            {
                return MonitorEventOpCode.Stop | MonitorEventOpCode.Mark;
            }
        }

        protected override async Task OnBufferedEventAsync(MonitorEventEntry e)
        {
            string message = String.Format("({0}:{1})[{2}:{3}]: {4}", e.Level, e.OpCode, e.Source, e.Name, e.Detail);

            if (e.OpCode == MonitorEventOpCode.Stop)
            {
                message += String.Format("({0:0.00}ms),", e.RuntimeMilliseconds);
            }

            await Console.Out.WriteLineAsync(message);
        }
    }
}
