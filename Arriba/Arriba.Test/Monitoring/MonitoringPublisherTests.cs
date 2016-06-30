// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Arriba.Monitoring;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Monitoring
{
    [TestClass]
    public class MonitoringPublisherTests
    {
        [TestMethod]
        public void MonitorPublisherEventScope()
        {
            var c = new TestMonitorEventConsumer(MonitorEventLevel.Information, MonitorEventOpCode.All);

            List<MonitorEventEntry> items = new List<MonitorEventEntry>();

            c.OnEventAction = (e) =>
            {
                items.Add(e);
            };

            using (EventPublisher.AddConsumer(c))
            {
                using (EventPublisher.Default.RaiseScope(MonitorEventLevel.Information, name: "foo"))
                {
                    Thread.Sleep(2);
                }
            }

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(MonitorEventOpCode.Start, items[0].OpCode);
            Assert.AreEqual(0.0, items[0].RuntimeMilliseconds);
            Assert.AreEqual(MonitorEventOpCode.Stop, items[1].OpCode);
            Assert.AreNotEqual(0.0, items[1].RuntimeMilliseconds);
        }

        [TestMethod]
        public void MonitorPublisherScopedEventSource()
        {
            var c = new TestMonitorEventConsumer(MonitorEventLevel.All, MonitorEventOpCode.All);

            List<MonitorEventEntry> items = new List<MonitorEventEntry>();
            bool eventRaised = false;

            c.OnEventAction = (e) =>
            {
                Assert.AreEqual("MySource", e.Source);
                Assert.AreEqual("test", e.Name);
                eventRaised = true;
            };

            using (EventPublisher.AddConsumer(c))
            {
                var source = EventPublisher.CreateEventSource("MySource");

                source.Raise(name: "test");
            }

            Assert.IsTrue(eventRaised);
        }
    }
}
