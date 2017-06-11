using Microsoft.VisualStudio.TestTools.UnitTesting;
using V5.Data;

namespace V5.Test
{
    [TestClass]
    public class ArraySearchTests
    {
        [TestMethod]
        public void ArraySearch_BinarySearch()
        {
            long[] buckets = new long[256];
            for (int i = 0; i < buckets.Length; ++i) buckets[i] = 2  * i;

            Assert.AreEqual(-1, ArraySearch.BucketIndex(buckets, -1));
            for (int i = 0; i < 511; ++i)
            {
                Assert.AreEqual(i / 2, ArraySearch.BucketIndex(buckets, i));
            }
            Assert.AreEqual(255, ArraySearch.BucketIndex(buckets, 512));

            buckets = new long[] { -1, 10, 20, 30, 50, 100, 1000, 1200 };
            Assert.AreEqual(1, ArraySearch.BucketIndex(buckets, 11));
            Assert.AreEqual(1, ArraySearch.BucketIndex(buckets, 10));
            Assert.AreEqual(-1, ArraySearch.BucketIndex(buckets, -2));
            Assert.AreEqual(0, ArraySearch.BucketIndex(buckets, -1));
            Assert.AreEqual(2, ArraySearch.BucketIndex(buckets, 20));
            Assert.AreEqual(5, ArraySearch.BucketIndex(buckets, 999));
            Assert.AreEqual(6, ArraySearch.BucketIndex(buckets, 1000));
            Assert.AreEqual(6, ArraySearch.BucketIndex(buckets, 1001));
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
