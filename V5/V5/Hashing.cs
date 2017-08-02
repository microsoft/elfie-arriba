// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba
{
    public class Hashing
    {
        public static unsafe ulong MurmurHash3(ulong key, uint seed)
        {
            return MurmurHash3((byte*)&key, 8, seed);
        }

        public static unsafe ulong MurmurHash3(long key, uint seed)
        {
            return MurmurHash3((byte*)&key, 8, seed);
        }

        public static unsafe ulong MurmurHash3(double key, uint seed)
        {
            return MurmurHash3((byte*)&key, 8, seed);
        }

        public static unsafe ulong MurmurHash3(Guid key, uint seed)
        {
            return MurmurHash3((byte*)&key, sizeof(Guid), seed);
        }

        public static unsafe ulong MurmurHash3(DateTime key, uint seed)
        {
            return MurmurHash3((byte*)&key, sizeof(DateTime), seed);
        }

        public static unsafe ulong MurmurHash3(TimeSpan key, uint seed)
        {
            return MurmurHash3((byte*)&key, sizeof(TimeSpan), seed);
        }

        /// <summary>
        ///  Murmur3 Hash; see http://en.wikipedia.org/wiki/MurmurHash or http://code.google.com/p/smhasher/wiki/MurmurHash3
        ///  MIT License.
        /// </summary>
        /// <param name="key">Data to be hashed</param>
        /// <param name="length">Length, in bytes, of data</param>
        /// <param name="seed">Seed value for hash</param>
        /// <returns>Hash of key</returns>
        public static unsafe ulong MurmurHash3(byte* key, int length, uint seed)
        {
            if (key == null) return 0;

            ulong* data = (ulong*)key;
            int nBlocks = length / (2 * sizeof(ulong));

            ulong h1 = seed;
            ulong h2 = seed;

            ulong c1 = 0x87c37b91114253d5;
            ulong c2 = 0x4cf5ad432745937f;

            ulong k1;
            ulong k2;

            // Body
            for (int i = 0; i < nBlocks; ++i)
            {
                k1 = data[2 * i];
                k2 = data[2 * i + 1];

                k1 *= c1;
                k1 = RotateLeft(k1, 31);
                k1 *= c2;
                h1 ^= k1;

                h1 = RotateLeft(h1, 27);
                h1 += h2;
                h1 = h1 * 5 + 0x52dce729;

                k2 *= c2;
                k2 = RotateLeft(k2, 33);
                k2 *= c1;
                h2 ^= k2;

                h2 = RotateLeft(h2, 31);
                h2 += h1;
                h2 = h2 * 5 + 0x38495ab5;
            }

            // Tail
            byte* tail = (byte*)data;
            tail += nBlocks * (2 * sizeof(ulong));

            int tailLength = length & ((2 * sizeof(ulong)) - 1);

            k1 = 0;
            k2 = 0;

            while (tailLength > 8)
            {
                k1 = k1 << 8;
                k1 ^= (ulong)(*tail);
                tailLength--;
                tail++;
            }

            k1 *= c1;
            k1 = RotateLeft(k1, 31);
            k1 *= c2;
            h1 ^= k1;

            while (tailLength > 0)
            {
                k2 = k2 << 8;
                k2 ^= (ulong)(*tail);
                tailLength--;
                tail++;
            }

            k2 *= c2;
            k2 = RotateLeft(k2, 33);
            k2 *= c1;
            h2 ^= k2;

            // Finalization
            h1 ^= (ulong)length;
            h2 ^= (ulong)length;

            h1 += h2;
            h2 += h1;

            h1 = FMix(h1);
            h2 = FMix(h2);

            h1 += h2;
            h2 += h1;

            // Remainder of 128 bit hash is in h2; only returning first 64 bits.
            return h1;
        }

        private static ulong RotateLeft(ulong value, int rotateBitCount)
        {
            return (value << rotateBitCount) | (value >> (64 - rotateBitCount));
        }

        private static ulong FMix(ulong value)
        {
            value ^= value >> 33;
            value *= 0xff51afd7ed558ccd;
            value ^= value >> 33;
            value *= 0xc4ceb9fe1a85ec53;
            value ^= value >> 33;

            return value;
        }
    }
}
