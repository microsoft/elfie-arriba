using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace V5.Test.Serialization
{
    [TestClass]
    public class BinarySerializerTests
    {
        private const string SamplePath = "Sample";

        [TestMethod]
        public void BinarySerializer_Basic()
        {
            // Verify integer round trip (over buffer boundary)
            VerifyRoundTrip(Enumerable.Range(0, 50000).ToArray());

            // Verify empty array handling
            VerifyRoundTrip(new ushort[0]);

            // Verify boolean array handling (Marshal.SizeOf<T> isn't right)
            bool[] boolArray = new bool[] { true, true, false, false, true, false };
            VerifyRoundTrip(boolArray);

            // Verify ulong round trip (type handling)
            ulong[] ulongArray = new ulong[] { ulong.MinValue, ulong.MaxValue, 10, 20, 30 };
            VerifyRoundTrip(ulongArray);

            // Verify byte[] round trip (short-circuit byte[])
            byte[] byteArray = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };
            VerifyRoundTrip(byteArray);

            // Test partial array writing (offset handling)
            BinarySerializer.Write(SamplePath, ulongArray, 1, 3);
            ulong[] ulongSlice = BinarySerializer.Read<ulong>(SamplePath);
            Assert.AreEqual(3, ulongSlice.Length);
            Assert.AreEqual(ulong.MaxValue, ulongSlice[0]);
            Assert.AreEqual((ulong)10, ulongSlice[1]);
            Assert.AreEqual((ulong)20, ulongSlice[2]);
        }

        private static void VerifyRoundTrip<T>(T[] values)
        {
            BinarySerializer.Write(SamplePath, values);
            T[] reloaded = BinarySerializer.Read<T>(SamplePath);

            Assert.AreEqual(values.Length, reloaded.Length);
            for (int i = 0; i < values.Length; ++i)
            {
                Assert.AreEqual(values[i], reloaded[i]);
            }
        }
    }
}
