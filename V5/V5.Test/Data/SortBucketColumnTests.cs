using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using V5.Data;

namespace V5.Test
{
    [TestClass]
    public class SortBucketColumnNTests
    {
        [TestMethod]
        public void SortBucketColumnN_BinarySearch()
        {
            long[] buckets = new long[256];
            for (int i = 0; i < buckets.Length; ++i) buckets[i] = 2 * i;

            Assert.AreEqual(-1, SortBucketColumnN.BucketIndex(buckets, -1));
            for (int i = 0; i < 511; ++i)
            {
                Assert.AreEqual(i / 2, SortBucketColumnN.BucketIndex(buckets, i));
            }
            Assert.AreEqual(255, SortBucketColumnN.BucketIndex(buckets, 512));

            buckets = new long[] { -1, 10, 20, 30, 50, 100, 1000, 1200 };
            Assert.AreEqual(1, SortBucketColumnN.BucketIndex(buckets, 11));
            Assert.AreEqual(1, SortBucketColumnN.BucketIndex(buckets, 10));
            Assert.AreEqual(-1, SortBucketColumnN.BucketIndex(buckets, -2));
            Assert.AreEqual(0, SortBucketColumnN.BucketIndex(buckets, -1));
            Assert.AreEqual(2, SortBucketColumnN.BucketIndex(buckets, 20));
            Assert.AreEqual(5, SortBucketColumnN.BucketIndex(buckets, 999));
            Assert.AreEqual(6, SortBucketColumnN.BucketIndex(buckets, 1000));
            Assert.AreEqual(6, SortBucketColumnN.BucketIndex(buckets, 1001));
        }

        [TestMethod]
        public void SortBucketColumn_Basics()
        {
            int[] values = Enumerable.Range(0, 10000).ToArray();
            SortBucketColumn<int> sbc = SortBucketColumn<int>.Build(values, 256, new Random(5), 1);

            // Validate the min and max were properly found
            Assert.AreEqual(0, sbc.Min);
            Assert.AreEqual(9999, sbc.Max);

            // Validate the row counts add up
            Assert.AreEqual(10000, sbc.Total);

            // Validate that the items for each bucket are correctly in range
            Dictionary<int, int> countPerBucket = new Dictionary<int, int>();

            for (int i = 0; i < values.Length; ++i)
            {
                int bucketIndex = sbc.RowBucketIndex[i];
                Assert.IsTrue(sbc.Minimum[bucketIndex] <= values[i]);

                if (bucketIndex < sbc.Minimum.Length - 2)
                {
                    Assert.IsTrue(values[i] < sbc.Minimum[bucketIndex + 1]);
                }
                else
                {
                    Assert.IsTrue(values[i] <= sbc.Minimum[bucketIndex + 1]);
                }

                int countForBucket = 0;
                countPerBucket.TryGetValue(bucketIndex, out countForBucket);
                countPerBucket[bucketIndex] = countForBucket + 1;
            }

            // Verify the count per bucket is correct
            for (int i = 0; i < sbc.Minimum.Length - 1; ++i)
            {
                Assert.AreEqual(countPerBucket[i], sbc.RowCount[i]);
            }
        }
    }
}
