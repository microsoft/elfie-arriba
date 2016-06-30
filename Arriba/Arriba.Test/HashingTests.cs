// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test
{
    [TestClass]
    public class HashingTests
    {
        [TestMethod]
        public unsafe void Murmur3_Basic()
        {
            HashSet<ulong> values = new HashSet<ulong>();

            // Integers: Verify hashing, verify hashes all unique for a large set
            for (ulong i = 0; i < 100000; ++i)
            {
                ulong partHash = Hashing.MurmurHash3(i, 0);
                Assert.IsFalse(values.Contains(partHash));
                values.Add(partHash);
            }

            // String and Guid -> String hashes: Verify values same as a copy
            CompareHashAndCopy((ByteBlock)"Reasonably Long Sample Value");
            CompareHashAndCopy((ByteBlock)Guid.NewGuid().ToString());

            // Verify hash is really using last bytes correctly (pointer math bugs)
            EnsureHashesDifferent(
                (ByteBlock)"9a6953f0-2db7-41ac-aea8-6e9defdbfa50",
                (ByteBlock)"9a6953f0-2db7-41ac-aea8-6e9defdbfa51"
            );

            // Guid: Verify hashes work, different values different
            Guid g1 = Guid.NewGuid();
            ulong g1Hash = Hashing.MurmurHash3(g1, 0);
            ulong g1HashCopy = Hashing.MurmurHash3(g1, 0);
            Assert.AreEqual(g1Hash, g1HashCopy);

            Guid g2 = Guid.NewGuid();
            ulong g2Hash = Hashing.MurmurHash3(g2, 0);
            Assert.AreNotEqual(g1Hash, g2Hash);

            // DateTime: Verify hashes work, different values different
            DateTime d1 = DateTime.UtcNow;
            ulong d1Hash = Hashing.MurmurHash3(d1, 0);
            ulong d1HashCopy = Hashing.MurmurHash3(d1, 0);
            Assert.AreEqual(d1Hash, d1HashCopy);

            DateTime d2 = d1.AddMilliseconds(5);
            ulong d2Hash = Hashing.MurmurHash3(d2, 0);
            Assert.AreNotEqual(d1Hash, d2Hash);

            // TimeSpan: Verify hashes work, different values different
            TimeSpan t1 = TimeSpan.FromMilliseconds(567);
            ulong t1Hash = Hashing.MurmurHash3(t1, 0);
            ulong t1HashCopy = Hashing.MurmurHash3(t1, 0);
            Assert.AreEqual(t1Hash, t1HashCopy);

            TimeSpan t2 = TimeSpan.FromMinutes(1);
            ulong t2Hash = Hashing.MurmurHash3(t2, 0);
            Assert.AreNotEqual(t1Hash, t2Hash);
        }

        private unsafe void CompareHashAndCopy(ByteBlock value)
        {
            ulong originalHash = Hash(value);

            int length = value.Length;
            byte[] copy = new byte[length];
            value.CopyTo(copy);

            ulong copyHash;
            fixed (byte* key = copy)
            {
                copyHash = Hashing.MurmurHash3(key, length, 0);
            }

            Assert.AreEqual(originalHash, copyHash, String.Format("Hash of '{0}' didn't match a copy of itself.", value));
        }

        private unsafe void EnsureHashesDifferent(ByteBlock left, ByteBlock right)
        {
            ulong leftHash = Hash(left);
            ulong rightHash = Hash(right);
            Assert.AreNotEqual(leftHash, rightHash, String.Format("Hash of '{0}' equals hash of '{1}', which was unexpected.", left, right));
        }

        private unsafe ulong Hash(ByteBlock value)
        {
            fixed (byte* key = value.Array)
            {
                return Hashing.MurmurHash3(key + value.Index, value.Length, 0);
            }
        }
    }
}
