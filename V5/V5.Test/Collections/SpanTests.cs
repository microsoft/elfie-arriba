using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

            // Verify constructor validation
            Verify.Exception<ArgumentNullException>(() => new Span<int>(null));
            Verify.Exception<ArgumentNullException>(() => new Span<int>(null, 0, 0));
            Verify.Exception<ArgumentOutOfRangeException>(() => new Span<int>(new int[2], -1, 0));
            Verify.Exception<ArgumentOutOfRangeException>(() => new Span<int>(new int[2], 0, -1));
            Verify.Exception<ArgumentOutOfRangeException>(() => new Span<int>(new int[2], 1, 2));

            // Verify length set validation
            Span<int> slice = new Span<int>(new int[] { 0, 1, 2, 3 }, 1, 2);
            Assert.AreEqual(2, slice.Length);
            slice.Length = 3;
            Assert.AreEqual(3, slice.Length);
            Verify.Exception<ArgumentOutOfRangeException>(() => { slice.Length = 4; });
        }

        private static void Span_Basics<T>(T[] array, T sample)
        {
            int index;
            T previous;

            // Make a Span of the full array
            Span<T> span = new Span<T>(array);

            // Verify the length and items are the same
            Assert.AreEqual(array.Length, span.Capacity);
            Assert.AreEqual(array.Length, span.Length);
            for (int i = 0; i < span.Length; ++i)
            {
                Assert.AreEqual(array[i], span[i]);
            }

            // Verify enumerating works
            index = 0;
            foreach(T value in span)
            {
                Assert.AreEqual(array[index++], value);
            }
            Assert.AreEqual(index, span.Length);

            // Remaining tests only work if the array is non-empty
            if (array.Length == 0) return;

            // Verify setting values works
            previous = span[0];
            span[0] = sample;
            Assert.AreEqual(sample, span[0]);
            Assert.AreEqual(sample, array[0]);

            span[0] = previous;
            Assert.AreEqual(previous, span[0]);
            Assert.AreEqual(previous, array[0]);

            // Make a Span of part of the array
            Span<T> slice = new Span<T>(array, 1, array.Length - 1);

            // Verify items are equal (offset by index)
            Assert.AreEqual(array.Length - 1, slice.Capacity);
            Assert.AreEqual(array.Length - 1, slice.Length);
            for (int i = 0; i < slice.Length; ++i)
            {
                Assert.AreEqual(array[i + 1], slice[i]);
            }

            // Verify enumerating works
            index = 0;
            foreach (T value in slice)
            {
                Assert.AreEqual(array[++index], value);
            }
            Assert.AreEqual(index, slice.Length);

            // Verify setting values works
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
