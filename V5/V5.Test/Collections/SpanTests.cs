using Microsoft.VisualStudio.TestTools.UnitTesting;
using V5.Collections;

namespace V5.Test.Collections
{
    [TestClass]
    public class SpanTests
    {
        [TestMethod]
        public void Span_Basics()
        {
            Span_Basics(new int[] { 0, 1, 2, 3 }, 10);
            Span_Basics(new int[0], 10);

            Span_Basics(new byte[] { 0, 1, 2, 3 }, (byte)10);
            Span_Basics(new bool[] { true, false, true, false }, false);
        }

        private static void Span_Basics<T>(T[] array, T sample)
        {
            T previous;

            // Make a Span of the full array
            Span<T> span = new Span<T>(array);

            // Verify the length and items are the same
            Assert.AreEqual(array.Length, span.Length);
            for (int i = 0; i < span.Length; ++i)
            {
                Assert.AreEqual(array[i], span[i]);
            }

            // Verify setting values works
            if (array.Length > 0)
            {
                previous = span[0];
                span[0] = sample;
                Assert.AreEqual(sample, span[0]);
                Assert.AreEqual(sample, array[0]);

                span[0] = previous;
                Assert.AreEqual(previous, span[0]);
                Assert.AreEqual(previous, array[0]);
            }

            // Make a Span of part of the array
            Span<T> slice = new Span<T>(array, 1, array.Length - 1);

            // Verify items are equal (offset by index)
            Assert.AreEqual(array.Length - 1, slice.Length);
            for (int i = 0; i < slice.Length; ++i)
            {
                Assert.AreEqual(array[i + 1], slice[i]);
            }

            // Verify setting values works
            if (array.Length > 1)
            {
                previous = slice[0];
                slice[0] = sample;
                Assert.AreEqual(sample, slice[0]);
                Assert.AreEqual(sample, array[1]);

                slice[0] = previous;
                Assert.AreEqual(previous, slice[0]);
                Assert.AreEqual(previous, array[1]);
            }
        }
    }
}
