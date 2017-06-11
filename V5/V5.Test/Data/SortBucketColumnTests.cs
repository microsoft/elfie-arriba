using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            for (int i = 0; i < buckets.Length; ++i) buckets[i] = 2  * i;

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
        public void Eytzinger()
        {
            long[] items = new long[16];
            for(int i = 0; i < items.Length; ++i)
            {
                items[i] = i;
            }

            long[] result = SortBucketColumn<long>.EytzingerSort(items);
        }
    }
}
