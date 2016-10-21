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
        public String8 Join(char delimiter, byte[] buffer)
        {
            ushort delimiterCode = (ushort)delimiter;
            if (delimiterCode >= 128) throw new ArgumentException(String.Format(Resources.UnableToSupportMultibyteCharacter, delimiter));
            byte delimiterByte = (byte)delimiterCode;

            int lengthRequired = _content.Length + (_partPositions.Count - 1) * (1 - _delimiterWidth);
            if (buffer.Length < lengthRequired) throw new ArgumentOutOfRangeException("buffer");

            int currentPosition = 0;
            for(int i = 0; i < Count; ++i)
            {
                if(i != 0) buffer[currentPosition++] = delimiterByte;
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
        public static String8Set Split(String8 value, char delimiter, PartialArray<int> positions)
        {
            // Clear any previous values in the array
            positions.Clear();

            // Get the delimiter as a byte
            ushort delimiterCode = (ushort)delimiter;
            if (delimiterCode >= 128) throw new ArgumentException(String.Format(Resources.UnableToSupportMultibyteCharacter, delimiter));
            byte delimiterByte = (byte)delimiterCode;

            // Record each delimiter position
            positions.Add(0);

            if (!value.IsEmpty())
            {
                // Get the String8 array directly and loop from index to (index + length)
                // 3x faster than String8[index].
                byte[] array = value._buffer;
                int end = value._index + value._length;
                for (int i = value._index; i < end; ++i)
                {
                    if (array[i] == delimiterByte)
                    {
                        // Next start position is after this delimiter
                        positions.Add(i - value._index + 1);
                    }
                }

                positions.Add(value.Length + 1);
            }

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
            if (value.IsEmpty()) return 1;

            // Get the delimiter as a byte
            ushort delimiterCode = (ushort)delimiter;
            if (delimiterCode >= 128) throw new ArgumentException(String.Format(Resources.UnableToSupportMultibyteCharacter, delimiter));
            byte delimiterByte = (byte)delimiterCode;

            // There are N+1 parts for N delimiters, plus one sentinel at the end
            int partCount = 2;
            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] == delimiterByte) partCount++;
            }

            return partCount;
        }

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
            // Clear any previous values in the array
            positions.Clear();

            // The first part always begins at the start of the string
            positions.Add(0);

            if (!value.IsEmpty())
            {
                byte[] array = value._buffer;
                int i = value._index;
                int end = i + value._length;

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
                            positions.Add(i - value._index + 1);
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
            }

            return new String8Set(value, 1, positions);
        }

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
                return _partPositions.Count - 1;
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
