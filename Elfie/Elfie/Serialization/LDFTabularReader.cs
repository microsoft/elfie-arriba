using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    public class LDFTabularReader : BaseTabularReader
    {
        private String8Block _columnNamesBlock;

        private String8Set _blockLines;
        private PartialArray<int> _linePositionArray;
        private int _nextRowFirstLine;

        public LDFTabularReader(string filePath) : this(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        { }

        public LDFTabularReader(Stream stream) : base(stream, false)
        {
            ReadColumns(stream);
        }

        private void ReadColumns(Stream stream)
        {
            // Allocate a fixed array to hold split lines
            _linePositionArray = new PartialArray<int>(1024);

            // Allocate a block to hold copies of unique column names
            _columnNamesBlock = new String8Block();

            // Walk the whole LDF by line looking for every unique column name found
            Dictionary<String8, int> columnsFound = new Dictionary<String8, int>();
            byte[] buffer = new byte[64 * 1024];
            int lengthRead = stream.Read(buffer, 0, buffer.Length);

            while(true)
            {
                // Read a block from the file
                String8 block = new String8(buffer, 0, lengthRead);

                // Split the block into lines
                String8Set lines = block.Split(UTF8.Newline, _linePositionArray);

                // Read and track all column names down to the second-to-last line
                for(int i = 0; i < lines.Count - 1; ++i)
                {
                    ReadColumnLine(lines[i], columnsFound);
                }

                String8 lastLine = lines[lines.Count - 1];

                // If we ran out of file, read the last line and stop
                if (lengthRead < buffer.Length)
                {
                    ReadColumnLine(lastLine, columnsFound);
                    break;
                }

                // If this was one big line, double the buffer to read more
                if(lines.Count == 1)
                {
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    System.Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                    buffer = newBuffer;
                }

                // Save the last line and read another block
                System.Buffer.BlockCopy(buffer, 0, buffer, lastLine._index, lastLine.Length);
                lengthRead = lastLine.Length + stream.Read(buffer, lastLine.Length, buffer.Length - lastLine.Length);
            }
        }

        private void ReadColumnLine(String8 line, Dictionary<String8, int> columnsFound)
        {
            // Skip empty and continuation lines
            if (line.Length == 0 || line[0] == UTF8.CR || line[0] == UTF8.Space) return;

            // Find the column name part of the line
            String8 columnName = line.BeforeFirst(UTF8.Colon);

            // If we haven't seen this column name before, add it to our collection
            if (!columnName.IsEmpty() && !columnsFound.ContainsKey(columnName))
            {
                int columnIndex = columnsFound.Count;
                columnsFound[_columnNamesBlock.GetCopy(columnName)] = columnIndex;

                string columnNameString = columnName.ToString();
                _columnHeadingsList.Add(columnNameString);
                _columnHeadings[columnNameString] = columnIndex;
            }
        }

        protected String8Set Split(String8 block, PartialArray<int> rowPositionArray, PartialArray<int> cellPositionArray)
        {
            // Split every line
            block.Split(UTF8.Newline, cellPositionArray);

            // Identify row boundaries (a completely empty line [\n\n or \r\n\r\n] is a new 'row')
            rowPositionArray.Clear();

            for (int i = 1; i < cellPositionArray.Count; ++i)
            {
                int difference = cellPositionArray[i] - cellPositionArray[i - 1];

                if (difference == 1)
                {
                    // \n\n is a row boundary
                    rowPositionArray.Add(cellPositionArray[i]);
                }
                else if (difference == 2)
                {
                    // \n\r\n is a row boundary
                    if (block[cellPositionArray[i] - 1] == UTF8.CR)
                    {
                        rowPositionArray.Add(cellPositionArray[i]);
                    }
                }
            }

            return new String8Set(block, 1, rowPositionArray);
        }

        protected override void SplitValues(String8Set rowCells, String8TabularValue[] rowValues)
        {
            base.SplitValues(rowCells, rowValues);
        }

        protected override String8Set SplitCells(String8 row, PartialArray<int> cellPositionArray)
        {
            // Identify cell boundaries (a line starting with space is a continuation of the previous value)
            cellPositionArray.Clear();

            int rowIndexInBlock = row._index - _blockReference._index;
            int rowEndInBlock = rowIndexInBlock + row.Length;

            //String8Set lines = new String8Set(_blockReference, 1, cellPositionArray);
            //int currentLineIndex = _nextRowFirstLine;
            //for (; currentLineIndex < lines.Count; ++currentLineIndex)
            //{
            //    String8 line = lines[currentLineIndex];

            //    // If line is outside row, stop
            //    if (line._index > rowEndInBlock) break;

            //    switch(line[0])
            //    {
            //        case UTF8.Space:
            //            // Multi-line attribute - unwrap
            //            case UTF8
            //    }
            //}


            // Walk lines after the last row identifying value boundaries (a line continues the previous value if it starts with space)
            //int shiftBackAmount = 0;
            int currentLineIndex = _nextRowFirstLine + 1;
            for(; currentLineIndex < _linePositionArray.Count; ++currentLineIndex)
            {
                int lineStartIndex = _linePositionArray[currentLineIndex];

                // If this line is after the row, stop
                if (lineStartIndex >= rowEndInBlock) break;

                // If the line starts with a space, shift it back to make it contiguous
                if (_blockReference[lineStartIndex] != UTF8.Space)
                {
                    // Convert the block index to a row-relative index
                    cellPositionArray.Add(lineStartIndex - rowIndexInBlock);
                }
                //else
                //{
                //    shiftBackAmount += 2;
                //    if (_blockReference[lineStartIndex - 2] == UTF8.CR) shiftBackAmount++;

                //    _blockReference.Substring(lineStartIndex, _linePositionArray[currentLineIndex + 1] - lineStartIndex).ShiftBack(shiftBackAmount);
                //}
            }

            // Track the line which starts the next row
            _nextRowFirstLine = currentLineIndex;

            return new String8Set(row, 1, cellPositionArray);
        }

        protected override String8Set SplitRows(String8 block, PartialArray<int> rowPositionArray)
        {
            // LDF files are line-based. We want to split the entire block on newlines and then
            // reconstruct the logical row and cell boundaries by looking at the lines.

            // Split the entire block into lines
            _blockLines = block.Split(UTF8.Newline, _linePositionArray);

            // Track lines read so far, so each row and cell read can process by line
            _nextRowFirstLine = 0;

            // Identify row boundaries (a completely empty line [\n\n or \r\n\r\n] is a new 'row')
            rowPositionArray.Clear();
            rowPositionArray.Add(0);

            for (int i = 0; i < _blockLines.Count; ++i)
            {
                String8 line = _blockLines[i];

                if(line.Length == 0)
                {
                    // \n\n is a row boundary
                    rowPositionArray.Add(_linePositionArray[i]);
                }
                else if (line.Length == 1 && line[0] == UTF8.CR)
                {
                    // \n\r\n is a row boundary
                    rowPositionArray.Add(_linePositionArray[i]);
                }
            }

            return new String8Set(block, 1, rowPositionArray);
        }
    }
}
