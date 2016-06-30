// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Serialization.Csv
{
    /// <summary>
    /// Structure representing a cell range within a row. 
    /// </summary>
    internal struct CsvCellRange
    {
        /// <summary>
        /// Start offset of the cell. 
        /// </summary>
        public int Start;

        /// <summary>
        /// Length of the cell range. 
        /// </summary>
        public int Length;

        /// <summary>
        /// Value indicating whether the cell containes double quotes. 
        /// </summary>
        public bool ContainsQuotes;

        /// <summary>
        /// Value indiciating whether the cell is a virtual row number cell. 
        /// </summary>
        public bool IsRowNumberVirtualCell;
    }
}
