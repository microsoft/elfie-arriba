using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace V5.Test
{
    [TestClass]
    public class ArraySearchTests
    {
        [TestMethod]
        public void ArraySearch_BinarySearch()
        {
            long[] buckets = new long[] { -1, 10, 20, 30, 50, 100, 1000 };
            Assert.AreEqual(1, ArraySearch.BucketIndex(buckets, 11));
            Assert.AreEqual(1, ArraySearch.BucketIndex(buckets, 10));
            Assert.AreEqual(-1, ArraySearch.BucketIndex(buckets, -2));
            Assert.AreEqual(0, ArraySearch.BucketIndex(buckets, -1));
            Assert.AreEqual(2, ArraySearch.BucketIndex(buckets, 20));
            Assert.AreEqual(5, ArraySearch.BucketIndex(buckets, 999));
            Assert.AreEqual(6, ArraySearch.BucketIndex(buckets, 1000));
            Assert.AreEqual(6, ArraySearch.BucketIndex(buckets, 1001));
        }
    }
}
