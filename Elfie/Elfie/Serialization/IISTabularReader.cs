// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  IISTabularReader is a tabular reader for the IIS log format,
    ///  which has '#' comment lines, newline-delimited rows, and space
    ///  delimited values.
    /// </summary>
    public class IISTabularReader : BaseTabularReader
    {
        public IISTabularReader(string filePath) : base(filePath, false)
        {
            ReadColumns();
        }

        public IISTabularReader(Stream stream) : base(stream, false)
        {
            ReadColumns();
        }

        private bool IsCommentRow()
        {
            if (this.CurrentRowColumns == 0) return false;

            String8 firstCell = this.Current(0).ToString8();
            if (firstCell.IsEmpty()) return false;

            return firstCell[0] == UTF8.Pound;
        }

        /// <summary>
        ///  Read initial comment rows and find and log the column names from the first "#Fields:" row.
        /// </summary>
        private void ReadColumns()
        {
            while (base.NextRow())
            {
                this.RowCountRead--;

                if (!IsCommentRow())
                {
                    throw new IOException("Reader couldn't find a #Fields: header in IIS Log.");
                }

                if (this.Current(0).ToString8().Equals("#Fields:"))
                {
                    for (int i = 1; i < this.CurrentRowColumns; ++i)
                    {
                        string columnName = this.Current(i).ToString();
                        _columnHeadingsList.Add(columnName);
                        _columnHeadings[columnName] = i - 1;
                    }

                    return;
                }
            }
        }

        public override bool NextRow()
        {
            // Read rows internally, skipping comment rows
            while (base.NextRow())
            {
                if (!IsCommentRow()) return true;
                this.RowCountRead--;
            }

            // If we reached EOF, return false
            return false;
        }

        protected override String8Set SplitCells(String8 row, PartialArray<int> cellPositionArray)
        {
            // Remove trailing '\r' to handle '\r\n' and '\n' line endings uniformly
            if (row.EndsWith(UTF8.CR)) row = row.Substring(0, row.Length - 1);

            return row.Split(UTF8.Space, cellPositionArray);
        }

        protected override String8Set SplitRows(String8 block, PartialArray<int> rowPositionArray)
        {
            return block.Split(UTF8.Newline, rowPositionArray);
        }
    }
}
