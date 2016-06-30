// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

using Arriba.Monitoring;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Monitoring
{
    [TestClass]
    public class MonitoringEventsTests
    {
        [TestMethod]
        public void TimingHandle_Elapsed()
        {
            using (TimingHandle th = new TimingHandle())
            {
                Thread.Sleep(2);
                Assert.AreNotEqual(0.0, th.ElapsedMiliseconds);
                Assert.IsTrue(th.ElapsedMiliseconds > 1.0, "Timing handle has an elapsed time greather than 1ms");
            }
        }

        [TestMethod]
        public void TimingHandle_Dispose()
        {
            double sample1 = 0.0;

            TimingHandle th = new TimingHandle();

            sample1 = th.ElapsedMiliseconds;
            Thread.Sleep(2);
            th.Dispose();

            double after1 = th.ElapsedMiliseconds;
            Thread.Sleep(2);
            double after2 = th.ElapsedMiliseconds;

            Assert.AreNotEqual(sample1, after1);
            Assert.AreNotEqual(sample1, after2);
            Assert.AreEqual(after1, after2, "Timing handle stopped after disposing");
        }

        [TestMethod]
        public void MonitorEventTimeStampSet()
        {
            DateTime start = DateTime.UtcNow;

            var consumer = new TestMonitorEventConsumer(MonitorEventLevel.All, MonitorEventOpCode.All);

            DateTime eventTime = DateTime.MinValue;
            bool callbackRaised = false;

            consumer.OnEventAction = (e) =>
                {
                    eventTime = e.TimeStamp;
                    callbackRaised = true;
                };

            using (EventPublisher.AddConsumer(consumer))
            {
                EventPublisher.Default.Raise(name: "Foo");

                DateTime end = DateTime.UtcNow;

                Assert.IsTrue(callbackRaised, "Callback raised");
                Assert.IsTrue(eventTime >= start, "Timestamp after start of test (Start: {0}, TimeStamp: {1})", start, eventTime);
                Assert.IsTrue(eventTime <= end, "Timestamp before end of test (End: {0}, Timestamp: {1})", end, eventTime);
            }
        }
    }
}
