// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Structures;

namespace Arriba.Indexing
{
    /// <summary>
    ///  IWordSplitter is used to break text (in ByteBlocks) into individual
    ///  words for indexing. Columns may include any IWordSplitter to allow
    ///  custom splitting appropriate to the type of content being indexed.
    /// </summary>
    public interface IWordSplitter
    {
        /// <summary>
        ///  Add all words found in the requested range of the text to
        ///  the result RangeSet.
        /// </summary>
        /// <param name="text">byte[] of UTF8 content to split</param>
        /// <param name="withinRange">Range within text to split</param>
        /// <param name="result">RangeSet to add found words to</param>
        void Split(byte[] text, Range withinRange, RangeSet result);
    }
}
