// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Core
{
    public static class Sampler
    {
        /// <summary>
        ///  Choose a random sample of approximately 1/8 of the rows in an ArraySelector.
        ///  Used to quickly sample rows when sampling is appropriate.
        /// </summary>
        /// <param name="selector">ArraySelector to sample</param>
        /// <param name="r">Random instance to use</param>
        /// <param name="remapArray">Array to put remapped indices in</param>
        /// <returns>ArraySelector including approximately 1/8 of the input selector rows chosen randomly</returns>
        public static ArraySelector Eighth(ArraySelector selector, Random r, ref int[] remapArray)
        {
            if (selector.Count == 0) return selector;
            if (selector.IsSingleValue) return ArraySelector.Single((selector.Count / 8) + 1);

            // Allocate an indices array for the sampled subset
            Allocator.AllocateToSize(ref remapArray, selector.Count);
            int sampleCount = 0;

            // Choose the rows to sample
            if (selector.Indices == null)
            {
                // If no indices, loop from start index to end index
                int i = selector.StartIndexInclusive;
                while (i < selector.EndIndexExclusive)
                {
                    // Generate one random integer
                    int random = r.Next();

                    // Choose whether the next 10 rows (30 bits) are included
                    int end = Math.Min(i + 10, selector.EndIndexExclusive);
                    for (; i < end; ++i)
                    {
                        if ((random & 7) == 0) remapArray[sampleCount++] = i;
                        random = random >> 3;
                    }
                }
            }
            else
            {
                // If indices, look up the index of each row
                int i = selector.StartIndexInclusive;
                while (i < selector.EndIndexExclusive)
                {
                    // Generate one random integer
                    int random = r.Next();

                    // Choose whether the next 10 rows (30 bits) are included
                    int end = Math.Min(i + 10, selector.EndIndexExclusive);
                    for (; i < end; ++i)
                    {
                        if ((random & 7) == 0) remapArray[sampleCount++] = selector.Indices[i];
                        random = random >> 3;
                    }
                }
            }

            return ArraySelector.Map(remapArray, sampleCount);
        }
    }
}
