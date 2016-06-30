// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  StatisticsRollup aggregates count and size statistics across parts which
    ///  expose these details individually.
    /// </summary>
    public class StatisticsRollup : IStatistics
    {
        private IStatistics[] _parts;

        public StatisticsRollup(IEnumerable<IStatistics> parts)
        {
            _parts = Enumerable.ToArray(parts);
        }

        public StatisticsRollup(params IStatistics[] parts)
        {
            _parts = parts;
        }

        public long Bytes
        {
            get
            {
                long total = 0;
                foreach (IStatistics part in _parts)
                {
                    if (part != null) total += part.Bytes;
                }
                return total;
            }
        }

        public int Count
        {
            get
            {
                int total = 0;
                foreach (IStatistics part in _parts)
                {
                    if (part != null) total += part.Count;
                }
                return total;
            }
        }
    }
}
