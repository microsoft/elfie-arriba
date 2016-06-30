// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Search
{
    /// <summary>
    ///  ResultIndenter is a formatting class. It can reformat tab delimited
    ///  content so that columns are aligned with each other.
    /// </summary>
    internal class ResultIndenter
    {
        private string Content { get; set; }
        private List<List<Range>> Lines { get; set; }

        public ResultIndenter(string content)
        {
            this.Content = content;
            this.Lines = new List<List<Range>>();

            Parse(this.Content);
        }

        public void Parse(string content)
        {
            List<Range> currentLine = new List<Range>();
            int cellIndex = 0;

            for (int i = 0; i < content.Length; ++i)
            {
                char c = content[i];

                if (c == '\t')
                {
                    // The cell goes ends with the character before the tab
                    currentLine.Add(new Range(cellIndex, i - 1));

                    // The next cell starts with the value after the tab
                    cellIndex = i + 1;
                }
                else if (c == '\n')
                {
                    // The line ends with the character before the \r\n or \n
                    int lineEnd = (i > 0 && content[i - 1] == '\r') ? i - 2 : i - 1;
                    currentLine.Add(new Range(cellIndex, lineEnd));
                    this.Lines.Add(currentLine);

                    // The next cell starts with the value after the \n
                    cellIndex = i + 1;
                    currentLine = new List<Range>();
                }
            }

            if (currentLine.Count > 0) this.Lines.Add(currentLine);
        }

        public string WriteAligned()
        {
            List<int> longestColumnLengths = new List<int>();

            // Figure out the longest value for each column across all lines
            foreach (List<Range> line in this.Lines)
            {
                for (int columnIndex = 0; columnIndex < line.Count; ++columnIndex)
                {
                    int cellLength = line[columnIndex].Length;

                    if (longestColumnLengths.Count <= columnIndex)
                    {
                        longestColumnLengths.Add(cellLength);
                    }
                    else if (longestColumnLengths[columnIndex] < cellLength)
                    {
                        longestColumnLengths[columnIndex] = cellLength;
                    }
                }
            }

            // Rewrite the cell values with space padding so that columns are aligned
            StringBuilder result = new StringBuilder();
            foreach (List<Range> line in this.Lines)
            {
                for (int columnIndex = 0; columnIndex < line.Count; ++columnIndex)
                {
                    Range cell = line[columnIndex];

                    // Write the value
                    result.Append(this.Content.Substring(cell.Start, cell.Length));

                    // Write spaces to pad it to the longest length plus one
                    int paddingLength = 1 + longestColumnLengths[columnIndex] - cell.Length;
                    for (int p = 0; p < paddingLength; ++p)
                    {
                        result.Append(" ");
                    }
                }

                // Write newlines as in the input
                result.AppendLine();
            }

            return result.ToString();
        }
    }
}
