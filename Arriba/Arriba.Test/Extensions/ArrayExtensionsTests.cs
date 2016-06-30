// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Extensions
{
    [TestClass]
    public class ArrayExtensionsTests
    {
        [TestMethod]
        public void ArrayExtensions_RecommendedSize()
        {
            int mediumSize = 1024;
            int largeSize = ushort.MaxValue;

            // Never recommends less than the minimum
            Assert.AreEqual(ArrayExtensions.MinimumSize, ArrayExtensions.RecommendedSize(ArrayExtensions.MinimumSize, 0, int.MaxValue));

            // Doesn't recommend an increase less than the growth amount
            Assert.AreEqual(ArrayExtensions.MinimumSize + ArrayExtensions.MinimumGrowthAmount, ArrayExtensions.RecommendedSize(ArrayExtensions.MinimumSize, ArrayExtensions.MinimumSize + 1, int.MaxValue));

            // Recommends an increase of the growth factor for larger values
            Assert.AreEqual(largeSize * (1 + ArrayExtensions.GrowthFactor), ArrayExtensions.RecommendedSize(largeSize, largeSize + 1, int.MaxValue));

            // Doesn't recommend a shrink for decreases less than the growth factor
            Assert.AreEqual(mediumSize, ArrayExtensions.RecommendedSize(mediumSize, (int)(mediumSize * (1 - ArrayExtensions.GrowthFactor) + 1), ushort.MaxValue));

            // Doesn't recommend a shrink below the minimum size
            Assert.AreEqual(1024, ArrayExtensions.RecommendedSize(1024, 1020, ushort.MaxValue));

            // Doesn't recommend increasing and then decreasing
            Assert.AreEqual(ArrayExtensions.MinimumSize + ArrayExtensions.MinimumGrowthAmount, ArrayExtensions.RecommendedSize(ArrayExtensions.MinimumSize + ArrayExtensions.MinimumGrowthAmount, ArrayExtensions.MinimumSize + 1, int.MaxValue));
        }

        [TestMethod]
        public void ArrayExtensions_RecommendedSize_GrowCount()
        {
            int allocationCount = 1;
            int size = ArrayExtensions.MinimumSize;

            for (int desiredSize = size + 1; size < ushort.MaxValue; ++desiredSize)
            {
                int recommendedSize = ArrayExtensions.RecommendedSize(size, desiredSize, ushort.MaxValue);
                if (recommendedSize != size)
                {
                    allocationCount++;
                    size = recommendedSize;
                }
            }

            Assert.AreEqual(40, allocationCount, "Unexpected number of resizes to fully grow partition arrays.");
        }

        [TestMethod]
        public void ArrayExtensions_Resize()
        {
            int[] array = new int[ArrayExtensions.MinimumSize];
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = 2 * i;
            }

            // Doesn't shrink below minimum size
            ArrayExtensions.Resize(ref array, 0, ushort.MaxValue);
            Assert.AreEqual(ArrayExtensions.MinimumSize, array.Length);

            // Does expand
            ArrayExtensions.Resize(ref array, 2 * ArrayExtensions.MinimumSize, ushort.MaxValue);
            Assert.AreEqual(ArrayExtensions.MinimumSize * 2, array.Length);

            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = 2 * i;
            }

            // Does shrink
            ArrayExtensions.Resize(ref array, ArrayExtensions.MinimumSize, ushort.MaxValue);
            Assert.AreEqual(ArrayExtensions.MinimumSize, array.Length);

            // Verify values preserved
            for (int i = 0; i < array.Length; ++i)
            {
                Assert.AreEqual(2 * i, array[i]);
            }
        }

        [TestMethod]
        public void ArrayExtensions_Page()
        {
            int[] sample = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.AreEqual("0, 1, 2, 3, 4", sample.Page(0, 5).Join(", "));
            Assert.AreEqual("5, 6, 7, 8, 9", sample.Page(5, 5).Join(", "));
            Assert.AreEqual("10", sample.Page(10, 5).Join(", "));
        }

        [TestMethod]
        public void ArrayExtensions_Null()
        {
            Verify.Exception<ArgumentNullException>(
                () => ((Array)null).Page(0, 1)
                );
        }

        [TestMethod]
        public void ArrayExtensions_LengthNegative()
        {
            Verify.Exception<ArgumentOutOfRangeException>(
                () => (new int[0]).Page(0, -1)
                );
        }

        [TestMethod]
        public void ArrayExtensions_IndexTooHigh()
        {
            Verify.Exception<ArgumentOutOfRangeException>(
                () => (new int[0]).Page(0, 1)
                );
        }

        [TestMethod]
        public void ArrayExtensions_IndexNegative()
        {
            Verify.Exception<ArgumentOutOfRangeException>(
                () => (new int[1]).Page(-1, 1)
                );
        }
    }
}
