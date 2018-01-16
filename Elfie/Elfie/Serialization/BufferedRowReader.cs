// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  BufferedRowReader reads rows from a file in blocks, given a row splitting function.
    ///  It ensures readers don't get partial rows by only returning rows before the last until
    ///  the file is fully read.
    /// </summary>
    public class BufferedRowReader : IDisposable
    {
        private Stream _stream;
        private Func<String8, PartialArray<int>, String8Set> _splitRows;

        private bool _startOfStream;
        private byte[] _buffer;
        private PartialArray<int> _rowPositionArray;
        private int _nextRowIndexInBlock;
        private String8Set _currentBlock;
        private String8 _currentRow;

        public BufferedRowReader(Stream stream, Func<String8, PartialArray<int>, String8Set> splitRows)
        {
            _stream = stream;
            _splitRows = splitRows;

            _startOfStream = true;

            long length = 64 * 1024;
            if (stream.CanSeek)
            {
                length = Math.Min(length, stream.Length + 1);
            }

            _buffer = new byte[length];
            _rowPositionArray = new PartialArray<int>(64, false);
        }

        public long BytesRead => _stream.Position;

        /// <summary>
        ///  Move the reader to the next row. This must be called before
        ///  reading the first row.
        /// </summary>
        /// <returns>True if another row exists, False if the TSV is out of content</returns>
        public String8 NextRow()
        {
            // If we're on the last row, ask for more (we don't read the last row in case it was only partially read into the buffer)
            if (_nextRowIndexInBlock >= _currentBlock.Count - 1)
            {
                NextBlock();
            }

            // If there are no more rows, return false
            if (_nextRowIndexInBlock >= _currentBlock.Count) return String8.Empty;

            // Get the next (complete) row from the current block
            _currentRow = _currentBlock[_nextRowIndexInBlock];

            _nextRowIndexInBlock++;
            return _currentRow;
        }

        /// <summary>
        ///  NextBlock is called by NextRow before reading the last row in _currentBlock.
        ///  Since the file is read in blocks, the last row is usually incomplete.
        ///  
        ///  If there's more file content, NextBlock should copy the last row to the start
        ///  of the buffer, read more content, and reset _currentBlock to the new split rows
        ///  and _nextRowIndexInBlock to zero (telling NextRow to read that row next).
        ///  
        ///  If there's no more file, the last row is complete. NextBlock must return
        ///  without changing _currentBlock or _nextRowIndexInBlock to tell NextRow it's safe
        ///  to return to the user.
        ///  
        ///  NextRow will call NextBlock *again* after the last row. NextBlock must again
        ///  not change anything to tell NextRow that there's nothing left.
        ///  
        ///  So, NextBlock must:
        ///   - Copy the last row to the start of the buffer (if not already there)
        ///   - Read more content to fill the buffer
        ///   - Split the buffer into rows
        ///   - Stop at end-of-file or when a full row was read
        ///   - Double the buffer until one of these conditions is met
        ///   
        ///   - Reset nextRowInIndexBlock *only if* a row was shifted or read
        /// </summary>
        private void NextBlock()
        {
            int bufferLengthFilledStart = 0;

            // Copy the last row to the start of the buffer (if not already there)
            if (_currentBlock.Count > 1)
            {
                String8 lastRow = _currentBlock[_currentBlock.Count - 1];
                lastRow.WriteTo(_buffer, 0);
                bufferLengthFilledStart = lastRow.Length;

                // Reset the next row to read (since we shifted a row)
                _nextRowIndexInBlock = 0;
            }

            int bufferLengthFilled = bufferLengthFilledStart;

            while (true)
            {
                // Read more content to fill the buffer
                bufferLengthFilled += _stream.Read(_buffer, bufferLengthFilled, _buffer.Length - bufferLengthFilled);

                String8 block = new String8(_buffer, 0, bufferLengthFilled);

                // Strip leading UTF8 BOM, if found, on first block
                if (_startOfStream)
                {
                    _startOfStream = false;
                    if (block.Length >= 3 && block[0] == 0xEF && block[1] == 0xBB && block[2] == 0xBF) block = block.Substring(3);
                }

                // Split the buffer into rows
                _currentBlock = _splitRows(block, _rowPositionArray);

                // Stop at end-of-file (read didn't fill buffer)
                if (bufferLengthFilled < _buffer.Length) break;

                // Stop when a full row was read (split found at least two parts)
                if (_currentBlock.Count > 1) break;

                // Otherwise, double the buffer (until a full row or end of file)
                byte[] newBuffer = new byte[_buffer.Length * 2];
                _buffer.CopyTo(newBuffer, 0);
                _buffer = newBuffer;
            }

            // If we read new content, reset the next row to read
            if (bufferLengthFilled > bufferLengthFilledStart) _nextRowIndexInBlock = 0;
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}
