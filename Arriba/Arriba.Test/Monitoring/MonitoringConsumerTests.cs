// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arriba.Monitoring;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Monitoring
{
    [TestClass]
    public class MonitoringConsumerTests
    {
        [TestMethod]
        public void MonitorSingleEvent()
        {
            var consumer = new TestMonitorEventConsumer(MonitorEventLevel.All, MonitorEventOpCode.All);

            bool wasCalled = false;

            consumer.OnEventAction = (e) =>
            {
                Assert.AreEqual(e.OpCode, MonitorEventOpCode.Stop);
                Assert.AreEqual(e.Level, MonitorEventLevel.Information);
                Assert.AreEqual("Test1", e.Name);
                wasCalled = true;
            };

            using (EventPublisher.AddConsumer(consumer))
            {
                EventPublisher.Default.Raise(MonitorEventLevel.Information, MonitorEventOpCode.Stop, name: "Test1");
            }

            Assert.IsTrue(wasCalled);
        }

        [TestMethod]
        public void MonitorDispose()
        {
            var consumer = new TestMonitorEventConsumer(MonitorEventLevel.All, MonitorEventOpCode.All);

            bool disposeCalled = false;
            bool eventCalled = false;

            consumer.OnDisposeAction = () => { disposeCalled = true; };
            consumer.OnEventAction = (e) => { eventCalled = true; };

            using (EventPublisher.AddConsumer(consumer))
            {
                EventPublisher.Default.Raise(MonitorEventLevel.Information);
                Assert.IsTrue(eventCalled);
                Assert.IsFalse(disposeCalled);

                eventCalled = false;
            }

            Assert.IsTrue(disposeCalled);

            EventPublisher.Default.Raise(MonitorEventLevel.Information);
            Assert.IsFalse(eventCalled);
        }

        [TestMethod]
        public void Monitor2Consumers()
        {
            var consumer = new TestMonitorEventConsumer(MonitorEventLevel.All, MonitorEventOpCode.All);

            int callCount = 0;

            consumer.OnEventAction = (e) =>
            {
                Assert.AreEqual(e.OpCode, MonitorEventOpCode.Stop);
                Assert.AreEqual(e.Level, MonitorEventLevel.Information);
                Assert.AreEqual("Test1", e.Name);
                callCount++;
            };

            var consumer2 = new TestMonitorEventConsumer(MonitorEventLevel.All, MonitorEventOpCode.All);

            consumer2.OnEventAction = (e) =>
            {
                Assert.AreEqual(e.OpCode, MonitorEventOpCode.Stop);
                Assert.AreEqual(e.Level, MonitorEventLevel.Information);
                Assert.AreEqual("Test1", e.Name);
                callCount++;
            };

            using (EventPublisher.AddConsumer(consumer))
            {
                using (EventPublisher.AddConsumer(consumer2))
                {
                    EventPublisher.Default.Raise(MonitorEventLevel.Information, MonitorEventOpCode.Stop, name: "Test1");
                }
            }

            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public void MonitorMarkFiltering()
        {
            var consumer = new TestMonitorEventConsumer(MonitorEventLevel.All, MonitorEventOpCode.Start);

            bool eventCalled = false;
            consumer.OnEventAction = (e) => { eventCalled = true; };

            using (EventPublisher.AddConsumer(consumer))
            {
                EventPublisher.Default.Raise(MonitorEventLevel.Information, MonitorEventOpCode.Stop);
            }

            Assert.IsFalse(eventCalled);
        }

        [TestMethod]
        public void MonitorLevelFiltering()
        {
            var consumer = new TestMonitorEventConsumer(MonitorEventLevel.Error, MonitorEventOpCode.Start);

            bool eventCalled = false;
            consumer.OnEventAction = (e) => { eventCalled = true; };

            using (EventPublisher.AddConsumer(consumer))
            {
                EventPublisher.Default.Raise(MonitorEventLevel.Error, MonitorEventOpCode.Mark);
                EventPublisher.Default.Raise(MonitorEventLevel.Warning, MonitorEventOpCode.Start);
                Assert.IsFalse(eventCalled, "Event should not be raised to consumer");
                EventPublisher.Default.Raise(MonitorEventLevel.Error, MonitorEventOpCode.Start);
                Assert.IsTrue(eventCalled, "Event should be raised to consumer");
            }
        }

        [TestMethod]
        public void MonitorLevelChange()
        {
            var consumer = new TestMonitorEventConsumer(MonitorEventLevel.Error, MonitorEventOpCode.All);

            bool eventCalled = false;
            consumer.OnEventAction = (e) => { eventCalled = true; };

            using (EventPublisher.AddConsumer(consumer))
            {
                EventPublisher.Default.Raise(MonitorEventLevel.Information, MonitorEventOpCode.Mark);
                Assert.IsFalse(eventCalled, "Event should not be raised to consumer");
                consumer.ChangeEventLevel(MonitorEventLevel.All, MonitorEventOpCode.All);

                EventPublisher.Default.Raise(MonitorEventLevel.Information, MonitorEventOpCode.Mark);
                Assert.IsTrue(eventCalled, "Event should be raised to consumer");
            }
        }
    }

    public class TestMonitorEventConsumer : IMonitorEventConsumer
    {
        public TestMonitorEventConsumer(MonitorEventLevel flags, MonitorEventOpCode opCode, Action<MonitorEventEntry> onEvent = null, Action onDispose = null)
        {
            this.NotifyOnEventFlags = flags;
            this.NotifyOnOpCodeFlags = opCode;
            this.OnEventAction = onEvent;
            this.OnDisposeAction = onDispose;
        }

        public MonitorEventLevel NotifyOnEventFlags
        {
            get;
            private set;
        }

        public MonitorEventOpCode NotifyOnOpCodeFlags
        {
            get;
            private set;
        }

        public event EventHandler OnNotifyLevelChange;

        public Action<MonitorEventEntry> OnEventAction
        {
            get;
            set;
        }

        public Action OnDisposeAction
        {
            get;
            set;
        }

        public void ChangeEventLevel(MonitorEventLevel flags, MonitorEventOpCode opCode)
        {
            this.NotifyOnEventFlags = flags;
            this.NotifyOnOpCodeFlags = opCode;

            var evt = this.OnNotifyLevelChange;

            if (evt != null)
            {
                evt(this, EventArgs.Empty);
            }
        }

        public void OnEvent(MonitorEventEntry arribaEvent)
        {
            if (OnEventAction != null)
            {
                this.OnEventAction(arribaEvent);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (this.OnDisposeAction != null)
            {
                this.OnDisposeAction();
            }
        }
    }
}
