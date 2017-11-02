// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    /// <summary>
    ///  String8Set represents a String8 which has been split into segments with
    ///  positions tracked via an externally provided buffer. It is used to contain
    ///  sets of String8s together or to support string splitting without allocation.
    /// </summary>
    public struct String8Set : IStatistics, IBinarySerializable
    {
        private String8 _content;
        private PartialArray<int> _partPositions;
        private int _delimiterWidth;

        internal String8Set(String8 content, int delimiterWidth, PartialArray<int> partPositions)
        {
            _content = content;
            _partPositions = partPositions;
            _delimiterWidth = delimiterWidth;
        }

        public static String8Set Empty = new String8Set(String8.Empty, 0, null);

        /// <summary>
        ///  Return the complete value which was split.
        /// </summary>
        public String8 Value
        {
            get { return _content; }
        }

        /// <summary>
        ///  Return the given part of the split string.
        /// </summary>
        /// <param name="index">0-based index of part to return</param>
        /// <returns>String8 for the index'th part of the split value</returns>
        public String8 this[int index]
        {
            get
            {
                if (_partPositions == null) throw new ArgumentOutOfRangeException("index");
                int innerIndex = _partPositions[index];
                return _content.Substring(innerIndex, _partPositions[index + 1] - innerIndex - _delimiterWidth);
            }
        }

        /// <summary>
        ///  ToString override returning the full value which was split
        /// </summary>
        /// <returns>The full value which was split</returns>
        public override string ToString()
        {
            return _content.ToString();
        }

        /// <summary>
        ///  Join the values in this String8Set with the given delimiter.
        ///  The buffer length required is the sum of the part length and
        ///  the delimiters. If the value split had delimiters, this.Content.Length
        ///  is enough.
        /// </summary>
        /// <param name="delimiter"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public String8 Join(byte delimiter, byte[] buffer)
        {
            if (delimiter >= 128) throw new ArgumentException(String.Format(Resources.UnableToSupportMultibyteCharacter, delimiter));

            int lengthRequired = _content.Length + (Count - 1) * (1 - _delimiterWidth);
            if (buffer.Length < lengthRequired) throw new ArgumentOutOfRangeException("buffer");

            int currentPosition = 0;
            for (int i = 0; i < Count; ++i)
            {
                if (i != 0) buffer[currentPosition++] = delimiter;
                currentPosition += this[i].WriteTo(buffer, currentPosition);
            }

            return new String8(buffer, 0, currentPosition);
        }

        /// <summary>
        ///  Split a string on a given delimiter into a provided byte[]. Used
        ///  to split strings without allocation when a large byte[] is created
        ///  and reused for many strings.
        /// </summary>
        /// <param name="value">String8 value to split</param>
        /// <param name="delimiter">Delimiter to split on</param>
        /// <param name="positionArray">int[] to contain split positions, of at least length String8Set.SplitRequiredLength</param>
        /// <returns>String8Set containing split value</returns>
        public static String8Set Split(String8 value, char delimiter, int[] positionArray)
        {
            return Split(value, (byte)delimiter, new PartialArray<int>(positionArray));
        }

        /// <summary>
        ///  Split a string on a given delimiter into a provided byte[]. Used
        ///  to split strings without allocation when a large byte[] is created
        ///  and reused for many strings.
        /// </summary>
        /// <param name="value">String8 value to split</param>
        /// <param name="delimiter">Delimiter to split on</param>
        /// <param name="positionArray">int[] to contain split positions, of at least length String8Set.SplitRequiredLength</param>
        /// <returns>String8Set containing split value</returns>
        public static String8Set Split(String8 value, byte delimiter, int[] positionArray)
        {
            return Split(value, delimiter, new PartialArray<int>(positionArray));
        }

        /// <summary>
        ///  Split a string on a given delimiter into a provided byte[]. Used
        ///  to split strings without allocation when a large byte[] is created
        ///  and reused for many strings.
        /// </summary>
        /// <param name="value">String8 value to split</param>
        /// <param name="delimiter">Delimiter to split on</param>
        /// <param name="positions">PartialArray&lt;int&gt; to contain split positions</param>
        /// <returns>String8Set containing split value</returns>
        public static String8Set Split(String8 value, byte delimiter, PartialArray<int> positions)
        {
            // Ensure the delimiter is single byte
            if (delimiter >= 128) throw new ArgumentException(String.Format(Resources.UnableToSupportMultibyteCharacter, delimiter));

            if (value.IsEmpty()) return String8Set.Empty;

            // Clear any previous values in the array
            positions.Clear();

            // Record each delimiter position
            positions.Add(0);

            // Get the String8 array directly and loop from index to (index + length)
            // 3x faster than String8[index].
            byte[] array = value.Array;
            int end = value.Index + value.Length;
            for (int i = value.Index; i < end; ++i)
            {
                if (array[i] == delimiter)
                {
                    // Next start position is after this delimiter
                    positions.Add(i - value.Index + 1);
                }
            }

            positions.Add(value.Length + 1);

            return new String8Set(value, 1, positions);
        }

        /// <summary>
        ///  Return the int[] length required for a buffer to split 'value'
        ///  by 'delimiter'. This may be an overestimate to perform better.
        ///  Used by callers to allocate a safe byte[] for String8Set.Split. 
        /// </summary>
        /// <param name="value">Value to Split</param>
        /// <param name="delimiter">Delimiter to Split by</param>
        /// <returns>Length of byte[] required to safely contain value</returns>
        public static int GetLength(String8 value, char delimiter)
        {
            return GetLength(value, (byte)delimiter);
        }
        /// <summary>
        ///  Return the int[] length required for a buffer to split 'value'
        ///  by 'delimiter'. This may be an overestimate to perform better.
        ///  Used by callers to allocate a safe byte[] for String8Set.Split. 
        /// </summary>
        /// <param name="value">Value to Split</param>
        /// <param name="delimiter">Delimiter to Split by</param>
        /// <returns>Length of byte[] required to safely contain value</returns>
        public static int GetLength(String8 value, byte delimiter)
        {
            // Ensure the delimiter is single byte
            if (delimiter >= 128) throw new ArgumentException(String.Format(Resources.UnableToSupportMultibyteCharacter, delimiter));

            if (value.IsEmpty()) return 1;

            // There are N+1 parts for N delimiters, plus one sentinel at the end
            int partCount = 2;
            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] == delimiter) partCount++;
            }

            return partCount;
        }

        #region CSV Support
        /// <summary>
        ///  Split a string on a given delimiter only outside matching double quotes.
        ///  Used to split CSV content where the delimiters are ignored within quotes.
        /// </summary>
        /// <param name="value">String8 value to split</param>
        /// <param name="delimiter">Delimiter to split on</param>
        /// <param name="positions">PartialArray&lt;int&gt; to contain split positions</param>
        /// <returns>String8Set containing split value</returns>
        public static String8Set SplitOutsideQuotes(String8 value, byte delimiter, PartialArray<int> positions)
        {
            if (value.IsEmpty()) return String8Set.Empty;

            // Clear any previous values in the array
            positions.Clear();

            // The first part always begins at the start of the string
            positions.Add(0);

            byte[] array = value.Array;
            int i = value.Index;
            int end = i + value.Length;

            // Walk the string. Find and mark delimiters outside of quotes only
            while (i < end)
            {
                // Outside Quotes
                for (; i < end; ++i)
                {
                    // If a quote is found, we're now inside quotes
                    if (array[i] == UTF8.Quote)
                    {
                        i++;
                        break;
                    }

                    // If a delimiter is found, add another split position
                    if (array[i] == delimiter)
                    {
                        positions.Add(i - value.Index + 1);
                    }
                }

                // Inside Quotes
                for (; i < end; ++i)
                {
                    // If a quote was found, we're now outside quotes
                    if (array[i] == UTF8.Quote)
                    {
                        i++;
                        break;
                    }
                }
            }

            // The last part always ends at the end of the string
            positions.Add(value.Length + 1);

            return new String8Set(value, 1, positions);
        }

        /// <summary>
        ///  Split a CSV row into cells. This method splits and unencodes quoted values together.
        ///  It changes the underlying buffer in the process.
        /// </summary>
        /// <param name="row">String8 containing a CSV row</param>
        /// <param name="positions">PartialArray&lt;int&gt; to contain split positions</param>
        /// <returns>String8Set containing unencoded cell values</returns>
        public static String8Set SplitAndDecodeCsvCells(String8 row, PartialArray<int> positions)
        {
            // If row is empty, return empty set
            if (row.IsEmpty()) return String8Set.Empty;

            // Clear any previous values in the array
            positions.Clear();

            // The first part always begins at the start of the (shifted) string
            positions.Add(0);

            byte[] array = row.Array;
            int i = row.Index;
            int end = i + row.Length;

            // We're shifting values in the string to overwrite quotes around cells
            // and doubled quotes. copyTo is where we've written to in the unescaped
            // string.
            int copyTo = i;

            // Walk each cell, handling quoted and unquoted cells.
            while (i < end)
            {
                bool inQuote = (array[i] == UTF8.Quote);

                if (!inQuote)
                {
                    // Unquoted cell. Copy until next comma.
                    for (; i < end; ++i, ++copyTo)
                    {
                        // Copy everything as-is (no unescaping)
                        array[copyTo] = array[i];

                        // If a delimiter is found, add another split position
                        if (array[i] == UTF8.Comma)
                        {
                            positions.Add(copyTo - row.Index + 1);
                            i++; copyTo++;
                            break;
                        }
                    }
                }
                else
                {
                    // Quoted cell.

                    // Overwrite opening quote
                    i++;

                    // Look for end quote (undoubled quote)
                    for (; i < end; ++i, ++copyTo)
                    {
                        if (array[i] != UTF8.Quote)
                        {
                            // Copy everything that wasn't an escaped quote
                            array[copyTo] = array[i];
                        }
                        else
                        {
                            // Quote found. End of cell, escaped quote, or unescaped quote (error)?
                            i++;

                            // End of cell [end of line]
                            if (i == end) break;

                            if (array[i] == UTF8.Comma)
                            {
                                // End of cell [comma]. Copy comma, end of cell.
                                positions.Add(copyTo - row.Index + 1);
                                array[copyTo] = array[i];
                                i++; copyTo++;
                                break;
                            }
                            else if (array[i] == UTF8.Quote)
                            {
                                // Escaped quote. Copy the second quote, continue cell.
                                array[copyTo] = array[i];
                            }
                            else
                            {
                                // Unescaped quote. Abort; caller will see incomplete row and can throw
                                return new String8Set(row, 1, positions);
                            }
                        }
                    }
                }
            }

            // The last part always ends at the end of the (shifted) string
            positions.Add(copyTo - row.Index + 1);

            // Overwrite duplicate values left from shifting to make bugs clearer
            for (; copyTo < end; ++copyTo)
            {
                array[copyTo] = UTF8.Null;
            }

            return new String8Set(row, 1, positions);
        }
        #endregion

        #region IBinarySerializable
        public void WriteBinary(BinaryWriter w)
        {
            w.Write(_delimiterWidth);
            _content.WriteBinary(w);
            _partPositions.WriteBinary(w);
        }

        public void ReadBinary(BinaryReader r)
        {
            _delimiterWidth = r.ReadInt32();

            _content = new String8();
            _content.ReadBinary(r);

            _partPositions = new PartialArray<int>();
            _partPositions.ReadBinary(r);
        }
        #endregion

        #region IStatistics
        /// <summary>
        ///  Get the number of strings contained within this String8Set.
        /// </summary>
        /// <returns>Count of String8s in this String8Set</returns>
        public int Count
        {
            get
            {
                if (_partPositions == default(PartialArray<int>)) return 0;
                return Math.Max(0, _partPositions.Count - 1);
            }
        }

        public long Bytes
        {
            get
            {
                if (_partPositions == default(PartialArray<int>)) return _content.Length;
                return _content.Length + 4 * _partPositions.Count;
            }
        }
        #endregion
    }
}
