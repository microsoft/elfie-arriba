// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  IStatistics reports the item count and total size in bytes of something.
    ///  It's implemented by data structures to provide simple rollups of total
    ///  size.
    /// </summary>
    public interface IStatistics
    {
        /// <summary>
        ///  Returns the count of items in the given data structure. If a structure
        ///  contains many types of items, this may be the "primary" one.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///  Return the total size in bytes of the structure. This should map to the
        ///  memory usage when loaded and the serialized size on disk of the structure.
        /// </summary>
        long Bytes { get; }
    }
}
