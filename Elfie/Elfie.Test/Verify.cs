﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Elfie.Test
{
    public static class Verify
    {
        public static void Exception<T>(Action run) where T : Exception
        {
            try
            {
                run();
                Assert.Fail("Expected exception of type: '" + typeof(T).FullName + "' but no exception was thrown");
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    e = ((AggregateException)e).InnerException;
                }

                Assert.AreEqual(typeof(T), e.GetType(), "An exception was thrown but it was not of the expected type.");
            }
        }

        public static T RoundTrip<T>(T item, Action<BinaryWriter> change = null) where T : IBinarySerializable
        {
            long bytesWritten = 0;

            using (MemoryStream stream = new MemoryStream())
            {
                // Write the item
                BinaryWriter writer = new BinaryWriter(stream);
                item.WriteBinary(writer);
                bytesWritten = stream.Position;

                // Allow changes to the stream [if caller passed]
                if (change != null)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    change(writer);
                }

                // Read it back
                stream.Seek(0, SeekOrigin.Begin);
                BinaryReader reader = new BinaryReader(stream);
                item.ReadBinary(reader);

                Assert.AreEqual(bytesWritten, stream.Position, "Reading item didn't read as many bytes as writing it wrote out.");
            }

            return item;
        }

        public static void PerformanceByOperation(long opsPerSecondGoal, Func<long> actionReturningOperationCount)
        {
            // Run scenario and measure runtime
            Stopwatch w = Stopwatch.StartNew();
            long operationCount = actionReturningOperationCount();
            w.Stop();

            // Compute ops per second achieved
            double opsPerMillisecondActual = (double)operationCount / w.ElapsedMilliseconds;
            long opsPerSecondActual = (long)(opsPerMillisecondActual * (double)1000);

            // Log performance vs. Goal
            Trace.WriteLine(String.Format(
                "{0} in {1}. Rate: {2}/s. Goal: {3}/s",
                operationCount.CountString(),
                w.Elapsed.ToFriendlyString(),
                opsPerSecondActual.CountString(),
                opsPerSecondGoal.CountString()));

            // Assert performance within goal
            Assert.IsTrue(opsPerSecondActual >= opsPerSecondGoal);
        }

        public static void PerformanceByBytes(long bytesPerSecondGoal, Func<long> actionReturningBytes)
        {
            // Run scenario and measure runtime
            Stopwatch w = Stopwatch.StartNew();
            long bytesProcessed = actionReturningBytes();
            w.Stop();

            // Compute bytes per second achieved
            double bytesPerMillisecondActual = (double)bytesProcessed / w.ElapsedMilliseconds;
            long bytesPerSecondActual = (long)(bytesPerMillisecondActual * (double)1000);

            // Log performance vs. Goal
            Trace.WriteLine(String.Format(
                "{0} in {1}. Rate: {2}/s. Goal: {3}/s",
                bytesProcessed.SizeString(),
                w.Elapsed.ToFriendlyString(),
                bytesPerSecondActual.SizeString(),
                bytesPerSecondGoal.SizeString()));

            // Assert performance within goal
            Assert.IsTrue(bytesPerSecondActual >= bytesPerSecondGoal);
        }
    }
}
