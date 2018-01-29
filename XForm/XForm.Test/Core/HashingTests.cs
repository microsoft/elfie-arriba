// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XForm.Test.Core
{
    [TestClass]
    public class HashingTests
    {
        [TestMethod]
        public unsafe void Hashing_Basic()
        {
            String8Block block = new String8Block();

            HashSet<ulong> values = new HashSet<ulong>();

            // Integers: Verify hashing, verify hashes all unique for a large set
            for (ulong i = 0; i < 100000; ++i)
            {
                ulong partHash = Hashing.Hash(i, 0);
                Assert.IsFalse(values.Contains(partHash));
                values.Add(partHash);
            }

            // String and Guid -> String hashes: Verify values same as a copy
            CompareHashAndCopy(block.GetCopy("Reasonably Long Sample Value"));
            CompareHashAndCopy(block.GetCopy(Guid.NewGuid().ToString()));

            // Verify hash is really using last bytes correctly (pointer math bugs)
            EnsureHashesDifferent(
                block.GetCopy("9a6953f0-2db7-41ac-aea8-6e9defdbfa50"),
                block.GetCopy("9a6953f0-2db7-41ac-aea8-6e9defdbfa51")
            );

            // Guid: Verify hashes work, different values different
            Guid g1 = Guid.NewGuid();
            ulong g1Hash = Hashing.Hash(g1, 0);
            ulong g1HashCopy = Hashing.Hash(g1, 0);
            Assert.AreEqual(g1Hash, g1HashCopy);

            Guid g2 = Guid.NewGuid();
            ulong g2Hash = Hashing.Hash(g2, 0);
            Assert.AreNotEqual(g1Hash, g2Hash);

            // DateTime: Verify hashes work, different values different
            DateTime d1 = DateTime.UtcNow;
            ulong d1Hash = Hashing.Hash(d1, 0);
            ulong d1HashCopy = Hashing.Hash(d1, 0);
            Assert.AreEqual(d1Hash, d1HashCopy);

            DateTime d2 = d1.AddMilliseconds(5);
            ulong d2Hash = Hashing.Hash(d2, 0);
            Assert.AreNotEqual(d1Hash, d2Hash);

            // TimeSpan: Verify hashes work, different values different
            TimeSpan t1 = TimeSpan.FromMilliseconds(567);
            ulong t1Hash = Hashing.Hash(t1, 0);
            ulong t1HashCopy = Hashing.Hash(t1, 0);
            Assert.AreEqual(t1Hash, t1HashCopy);

            TimeSpan t2 = TimeSpan.FromMinutes(1);
            ulong t2Hash = Hashing.Hash(t2, 0);
            Assert.AreNotEqual(t1Hash, t2Hash);
        }

        private unsafe void CompareHashAndCopy(String8 value)
        {
            ulong originalHash = Hashing.Hash(value, 0);

            int length = value.Length;
            byte[] copy = new byte[length];
            value.WriteTo(copy, 0);
            String8 copy8 = new String8(copy, 0, length);

            ulong copyHash = Hashing.Hash(copy8, 0);

            Assert.AreEqual(originalHash, copyHash, String.Format("Hash of '{0}' didn't match a copy of itself.", value));
        }

        private unsafe void EnsureHashesDifferent(String8 left, String8 right)
        {
            ulong leftHash = Hashing.Hash(left, 0);
            ulong rightHash = Hashing.Hash(right, 0);
            Assert.AreNotEqual(leftHash, rightHash, String.Format("Hash of '{0}' equals hash of '{1}', which was unexpected.", left, right));
        }
    }
}
