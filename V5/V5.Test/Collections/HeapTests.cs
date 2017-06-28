using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using V5.Collections;

namespace V5.Test.Collections
{
    [TestClass]
    public class HeapTests
    {
        [TestMethod]
        public void Heap_Basics()
        {
            Heap<int> heap = new Heap<int>((left, right) => left.CompareTo(right), new Span<int>(new int[10], 0, 0), 5);
            Assert.AreEqual(0, heap.Length);

            // Verify insert on empty heap
            Assert.IsTrue(heap.Push(1));
            Assert.AreEqual(1, heap.Length);
            Assert.AreEqual(1, heap.Peek());

            // Verify larger value stays below
            Assert.IsTrue(heap.Push(3));
            Assert.AreEqual(2, heap.Length);
            Assert.AreEqual(1, heap.Peek());

            // Verify smaller value replaces root
            Assert.IsTrue(heap.Push(0));
            Assert.AreEqual(3, heap.Length);
            Assert.AreEqual(0, heap.Peek());

            // Verify larger value stays below
            Assert.IsTrue(heap.Push(4));
            Assert.AreEqual(4, heap.Length);
            Assert.AreEqual(0, heap.Peek());

            // Verify smaller values moves up, but not to root
            Assert.IsTrue(heap.Push(2));
            Assert.AreEqual(5, heap.Length);
            Assert.AreEqual(0, heap.Peek());

            // Verify insert above limit fails
            Assert.IsFalse(heap.Push(10));
            Assert.AreEqual(5, heap.Length);
            Assert.AreEqual(0, heap.Peek());

            // Verify items are popped in ascending order
            Assert.AreEqual("0, 1, 2, 3, 4", PopAll(heap));
        }

        private static string PopAll(Heap<int> heap)
        {
            StringBuilder result = new StringBuilder();

            while (heap.Length > 0)
            {
                int lengthBefore = heap.Length;

                if (result.Length > 0) result.Append(", ");
                result.Append(heap.Pop());

                Assert.AreEqual(lengthBefore - 1, heap.Length);
            }

            return result.ToString();
        }
    }
}
