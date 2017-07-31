using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace V5
{
    // TODO: Actually test..
    // Remove
    // Serializability

    public class HashSet5<T> : IEnumerable<T> where T : IEquatable<T>
    {
        public int Count { get; private set; }
        private int HashBitsUsed;
        private T[] Values;
        private byte[] Metadata;

        public HashSet5()
        {
            this.Count = 0;
            this.Values = new T[16];
            this.Metadata = new byte[16];
            this.HashBitsUsed = HashBitsToUse(this.Metadata.Length);
        }

        public void Clear()
        {
            Array.Clear(this.Metadata, 0, this.Metadata.Length);
            Array.Clear(this.Values, 0, this.Values.Length);
        }

        private struct BucketAndSuffix
        {
            public int Bucket;
            public byte Suffix;
        }

        private BucketAndSuffix Get(T value)
        {
            // Get the full hash of the value
            uint hash = (uint)value.GetHashCode();

            // Keep only the count of bits we're using
            hash = hash & ~(uint.MaxValue >> this.HashBitsUsed);

            BucketAndSuffix result;

            // Calculate the bucket with the item (Lemire method)
            result.Bucket = (int)(((ulong)hash * (ulong)this.Metadata.Length) >> 32);

            // Get the bit suffix which will be in metadata
            result.Suffix = (byte)((hash >> (32 - this.HashBitsUsed)) & 15);

            return result;
        }

        public bool Contains(T value)
        {
            return Contains(value, Get(value));
        }

        private bool Contains(T value, BucketAndSuffix location)
        {
            // In the desired cell, a match would be the hash suffix and a 'wealth' of 15
            byte metadataMatch = (byte)(location.Suffix + 0xF0);

            // Search for the item
            for (int bucket = location.Bucket; metadataMatch >= 0x10; ++bucket, metadataMatch -= 0x10)
            {
                if (bucket == this.Metadata.Length) bucket = 0;
                if (this.Metadata[bucket] == metadataMatch && this.Values[bucket].Equals(value)) return true;
            }

            return false;
        }

        public bool Add(T value)
        {
            BucketAndSuffix location = Get(value);
            if (Contains(value, location)) return false;
            return Add(value, location);
        }

        private bool Add(T value, BucketAndSuffix location)
        {
            // Insert the item (swapping with existing items which are closer to their target bucket)
            byte metadataMatch = (byte)(location.Suffix + 0xF0);
            for (int bucket = location.Bucket; metadataMatch >= 0x10; ++bucket, metadataMatch -= 0x10)
            {
                if (bucket == this.Metadata.Length) bucket = 0;

                byte metaFound = this.Metadata[bucket];
                if(metaFound < 0x10)
                {
                    // Cell empty - place item and return
                    this.Values[bucket] = value;
                    this.Metadata[bucket] = metadataMatch;
                    this.Count++;
                    return true;
                }
                else if (metadataMatch < metaFound)
                {
                    // Put the item to place here
                    T valueMoved = this.Values[bucket];
                    this.Values[bucket] = value;
                    this.Metadata[bucket] = metadataMatch;

                    // Get the evicted item to continue finding a place for
                    value = valueMoved;
                    metadataMatch = metaFound;
                }
            }

            // If we get here, we couldn't place the item - we must expand
            Expand();

            // Add the value, re-calculating where it goes
            return Add(value);
        }

        private void Expand()
        {
            // Expand the array to 1.5x the current size up to 1M items, 1.125x the current size thereafter
            int sizeShiftAmount = (this.Metadata.Length >= 1048576 ? 3 : 1);
            int newSize = this.Metadata.Length + (this.Metadata.Length >> sizeShiftAmount);

            // Round the new size up to an even 16 item length
            if ((newSize & 15) != 0) newSize = (newSize + 16) & ~15;

            // Save the current contents
            T[] oldValues = this.Values;
            byte[] oldMetadata = this.Metadata;
            int oldHashBitsUsed = this.HashBitsUsed;

            // Allocate the larger table
            this.Values = new T[newSize];
            this.Metadata = new byte[newSize];
            this.HashBitsUsed = HashBitsToUse(newSize);
            this.Count = 0;

            // Re-add each item to the expanded table
            //if (this.HashBitsUsed != oldHashBitsUsed)
            {
                // If we need to re-hash, re-hash
                for (int i = 0; i < oldMetadata.Length; ++i)
                {
                    if (oldMetadata[i] >= 0x10)
                    {
                        Add(oldValues[i]);
                    }
                }
            }
            //else
            //{
            //    // If not, figure out new bucket from old bucket and hash suffix from before
            //    //  If [oldBucket = (topHashBits32 * oldSize) >> 32], then
            //    //      oldBucket << 32 should have (HashBitsUsed - 4) accurate high bits.

            //    ulong bucketToTopBitsMask = ~(ulong.MaxValue >> (oldHashBitsUsed - 4));
            //    int bucketShiftAmount = (oldHashBitsUsed - 4);
            //    int shiftAmount = (64 - oldHashBitsUsed);
            //    int sizeScaleShiftAmount = (oldMetadata.Length >= 1048576 ? 3 : 1);

            //    for (int i = 0; i < oldMetadata.Length; ++i)
            //    {
            //        if (oldMetadata[i] >= 0x10)
            //        {
            //            BucketAndSuffix location;

            //            // Extract the previous suffix
            //            location.Suffix = (byte)(oldMetadata[i] & 15);

            //            // Reconstitute the hash bits from the previous bucket and saved bits, in the highest bits of a 64-bit value
            //            int wealth = (oldMetadata[i] >> 4);
            //            int idealBucket = i - (15 - wealth);
            //            if (idealBucket < 0) idealBucket += oldMetadata.Length;

            // ISSUE: 'idealBucket' shift isn't right; need to figure out how many bits overlap with suffix to shift properly.
            //            uint topHashBits32 = ((((uint)(idealBucket << bucketShiftAmount) & ~(uint)15) + location.Suffix) << shiftAmount);

            //            // Multiply by (newSize / oldSize) (1.5 or 1.125) to find new bucket
            //            location.Bucket = (int)(((ulong)topHashBits32 * (ulong)newSize) >> 32);

            //            // TEMP: Validate hashing
            //            BucketAndSuffix compareLocation = Get(oldValues[i]);
            //            if (compareLocation.Bucket != location.Bucket) Debugger.Break();

            //            // Add the item
            //            Add(oldValues[i], location);
            //        }
            //    }
            //}
        }

        private static int HashBitsToUse(int tableSize)
        {
            int bitsToUse = 4;

            // For each four bits we need to find the table cell, keep four more bits of hash
            while(bitsToUse < 32)
            {
                tableSize = tableSize >> 4;
                if (tableSize == 0) break;
                bitsToUse += 4;
            }

            return bitsToUse;
        }

        public struct HashSet5Enumerator<U> : IEnumerator<U> where U : IEquatable<U>
        {
            private HashSet5<U> Set;
            private int NextBucket;

            public U Current => this.Set.Values[this.NextBucket];
            object IEnumerator.Current => this.Set.Values[this.NextBucket];

            public HashSet5Enumerator(HashSet5<U> set)
            {
                this.Set = set;
                this.NextBucket = -1;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                while(++this.NextBucket < this.Set.Metadata.Length)
                {
                    if (this.Set.Metadata[this.NextBucket] >= 0x10) return true;
                }

                return false;
            }

            public void Reset()
            {
                this.NextBucket = 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new HashSet5Enumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new HashSet5Enumerator<T>(this);
        }
    }
}
