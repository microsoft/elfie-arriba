// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

namespace XForm
{
    /// <summary>
    ///  Hashing is a MurmurHash3 implementation. It's based on Arriba\Hashing.cs.
    /// </summary>
    public class Hashing
    {
        public static unsafe ulong Hash(byte key, uint seed)
        {
            return Hash((byte*)&key, 1, seed);
        }

        public static unsafe ulong Hash(sbyte key, uint seed)
        {
            return Hash((byte*)&key, 1, seed);
        }

        public static unsafe ulong Hash(short key, uint seed)
        {
            return Hash((byte*)&key, 2, seed);
        }

        public static unsafe ulong Hash(ushort key, uint seed)
        {
            return Hash((byte*)&key, 2, seed);
        }

        public static unsafe ulong Hash(int key, uint seed)
        {
            return Hash((byte*)&key, 4, seed);
        }

        public static unsafe ulong Hash(uint key, uint seed)
        {
            return Hash((byte*)&key, 4, seed);
        }

        public static unsafe ulong Hash(long key, uint seed)
        {
            return Hash((byte*)&key, 8, seed);
        }

        public static unsafe ulong Hash(ulong key, uint seed)
        {
            return Hash((byte*)&key, 8, seed);
        }

        public static unsafe ulong Hash(float key, uint seed)
        {
            return Hash((byte*)&key, 4, seed);
        }

        public static unsafe ulong Hash(double key, uint seed)
        {
            return Hash((byte*)&key, 8, seed);
        }

        public static unsafe ulong Hash(Guid key, uint seed)
        {
            return Hash((byte*)&key, sizeof(Guid), seed);
        }

        public static unsafe ulong Hash(DateTime key, uint seed)
        {
            return Hash((byte*)&key, sizeof(DateTime), seed);
        }

        public static unsafe ulong Hash(TimeSpan key, uint seed)
        {
            return Hash((byte*)&key, sizeof(TimeSpan), seed);
        }

        public static unsafe ulong Hash(String8 key, uint seed)
        {
            fixed (byte* array = key.Array)
            {
                return Hash(&array[key.Index], key.Length, seed);
            }
        }

        public static uint Murmur3(uint value, uint seed)
        {
            uint h = seed;

            uint k = value;
            k *= 0xcc9e2d51;
            k = (k << 15) | (k >> 17);
            k *= 0x1b873593;
            h ^= k;
            h = (h << 13) | (h >> 19);
            h = (h * 5) + 0xe6546b64;

            h ^= 4;
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return h;
        }

        private const uint M = 0x5bd1e995;
        private const int R = 24;

        public static uint Murmur2(uint value, uint seed)
        {
            uint h = seed ^ 4;

            uint k = value;
            k *= M;
            k ^= k >> R;
            k *= M;

            h *= M;
            h ^= k;

            h ^= h >> 13;
            h *= M;
            h ^= h >> 15;

            return h;
        }

        /// <summary>
        ///  Murmur3 Hash; see http://en.wikipedia.org/wiki/MurmurHash or http://code.google.com/p/smhasher/wiki/MurmurHash3
        ///  MIT License.
        /// </summary>
        /// <param name="key">Data to be hashed</param>
        /// <param name="length">Length, in bytes, of data</param>
        /// <param name="seed">Seed value for hash</param>
        /// <returns>Hash of key</returns>
        public static unsafe ulong Hash(byte* key, int length, uint seed)
        {
            // TODO: Native Acceleration, with AVX if possible. [Compute K1|K2 and H1|H2 in parallel?]
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
                k1 = k1 << 64;
                k1 ^= (*(ulong*)tail);
                tailLength -= 8;
                tail += 8;
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
