// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

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

        public static T RoundTrip<T>(T item) where T : IBinarySerializable
        {
            long bytesWritten = 0;

            using (MemoryStream stream = new MemoryStream())
            {
                // Write the item
                BinaryWriter writer = new BinaryWriter(stream);
                item.WriteBinary(writer);
                bytesWritten = stream.Position;

                // Read it back
                stream.Seek(0, SeekOrigin.Begin);
                BinaryReader reader = new BinaryReader(stream);
                item.ReadBinary(reader);

                Assert.AreEqual(bytesWritten, stream.Position, "Reading item didn't read as many bytes as writing it wrote out.");
            }

            return item;
        }
    }
}
