// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace XForm.Core
{
    public class ParallelRunner
    {
        public static int ParallelCount = Environment.ProcessorCount;

        public static void Run(int startIndexInclusive, int endIndexExclusive, Action<int, int> method)
        {
            if (ParallelCount == 1)
            {
                method(startIndexInclusive, endIndexExclusive);
            }

            int segmentLength = ParallelLengthPart(endIndexExclusive - startIndexInclusive, ParallelCount);

            Parallel.For(0, ParallelCount, (i) =>
            {
                int threadStartIndex = startIndexInclusive + i * segmentLength;
                int threadEndIndex = (i == ParallelCount - 1 ? endIndexExclusive : threadStartIndex + segmentLength);

                method(threadStartIndex, threadEndIndex);
            });
        }

        public static int ParallelLengthPart(int totalCount, int parallelCount)
        {
            // Divide the count by the number of threads, but make each part (except the last) an even multiple of 64
            int portionLength = totalCount / parallelCount;
            if ((portionLength & 63) != 0) portionLength = 64 + portionLength & ~63;
            return portionLength;
        }
    }
}
