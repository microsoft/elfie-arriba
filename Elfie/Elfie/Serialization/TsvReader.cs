// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  TsvReader is a high performance reader base class for TSV format.
    ///  See BaseTabularReader comment for details and usage.
    /// </summary>
    public class TsvReader : BaseTabularReader
    {
        /// <summary>
        ///  Construct a TsvReader to read the given TSV file.
        /// </summary>
        /// <param name="tsvFilePath">File Path to TSV file to read</param>
        /// <param name="hasHeaderRow">True to read the first row as column names, False not to pre-read anything</param>
        public TsvReader(string tsvFilePath, bool hasHeaderRow = true) :
            base(tsvFilePath, hasHeaderRow)
        { }

        /// <summary>
        ///  Construct a TsvReader to read the given stream.
        /// </summary>
        /// <param name="strean">Stream to read</param>
        /// <param name="hasHeaderRow">True to read the first row as column names, False not to pre-read anything</param>
        public TsvReader(Stream stream, bool hasHeaderRow = true) :
            base(stream, hasHeaderRow)
        { }

        protected override String8Set SplitCells(String8 row, PartialArray<int> cellPositionArray)
        {
            // Remove trailing '\r' to handle '\r\n' and '\n' line endings uniformly
            if (row.EndsWith(UTF8.CR)) row = row.Substring(0, row.Length - 1);
            return row.Split(UTF8.Tab, cellPositionArray);
        }

        protected override String8Set SplitRows(String8 block, PartialArray<int> rowPositionArray)
        {
            return block.Split(UTF8.Newline, rowPositionArray);
        }
    }
}
