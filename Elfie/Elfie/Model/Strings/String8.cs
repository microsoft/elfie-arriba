// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis.Elfie.Extensions;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;
using Microsoft.CodeAnalysis.Elfie.Serialization;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    /// <summary>
    ///  String8 represents a UTF8-encoded string within a shared, externally
    ///  allocated byte[].
    /// 
    ///  string value = "something";
    ///  String8 converted = String8.Convert(value, new byte[String8.GetLength(value)]);
    /// </summary>
    public struct String8 : IComparable<String8>, IComparable<string>, IBinarySerializable, IWriteableString
    {
        internal byte[] _buffer;
        internal int _index;
        internal int _length;

        public String8(byte[] buffer, int index, int length)
        {
            _buffer = buffer;
            _index = index;
            _length = length;
        }

        public static String8 Empty = new String8(null, 0, 0);

        #region Conversion
        /// <summary>
        ///  Convert a string to a String8. Requires caller to pass the
        ///  destination array to make allocations clear in code. Reuse
        ///  arrays in loops for much better performance. To find needed
        ///  array size, call String8.GetLength(value).
        /// </summary>
        /// <param name="value">string to convert</param>
        /// <param name="buffer">byte[] into which to convert, at least String8.GetLength(value) long</param>
        /// <param name="index">Start index in byte[] into which to copy value</param>
        /// <returns>String8 pointing to the value copied into buffer</returns>
        public static String8 Convert(string value, byte[] buffer, int index = 0)
        {
            int paddedLength = GetLength(value);
            int end = index + paddedLength;
            if (end > buffer.Length) throw new ArgumentOutOfRangeException(String.Format(Resources.BufferTooSmall, buffer.Length, paddedLength));

            if (!String.IsNullOrEmpty(value))
            {
                Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, index);
            }

            return new String8(buffer, index, paddedLength);
        }

        /// <summary>
        ///  Convert a substring of a string into a String8. Required caller to
        ///  pass destination array to make allocations clear in code. Reuse
        ///  arrays in loops for much better performance. To find needed array
        ///  size, call String8.GetLength(value)
        /// </summary>
        /// <param name="value">Value to convert substring of</param>
        /// <param name="charIndex">Index of value substring to convert</param>
        /// <param name="charLength">Length of value substring to convert</param>
        /// <param name="buffer">byte[] into which to write converted value</param>
        /// <param name="index">Start index in byte[] into which to copy value</param>
        /// <returns>String8 pointing to converted value in buffer</returns>
        public static String8 Convert(string value, int charIndex, int charLength, byte[] buffer, int index = 0)
        {
            // Convert substring; can't tell length in advance (no GetByteCount overload)
            int lengthWritten = Encoding.UTF8.GetBytes(value, charIndex, charLength, buffer, index);
            return new String8(buffer, index, lengthWritten);
        }

        /// <summary>
        ///  Split this String8 into a String8Set with the given delimiter.
        ///  Requires the caller to pass an int[] to contain the split positions.
        ///  Reuse the array in a loop for much better performance. Call
        ///  String8Set.GetLength to determine the array length required.
        /// </summary>
        /// <param name="delimiter">Delimiter on which to split</param>
        /// <param name="positionArray">int[] to contain split positions</param>
        /// <returns>String8Set containing the String8 split on the delimiter</returns>
        public String8Set Split(byte delimiter, int[] positionArray)
        {
            return String8Set.Split(this, delimiter, positionArray);
        }

        /// <summary>
        ///  Split this String8 into a String8Set with the given delimiter.
        ///  Requires the caller to pass an int[] to contain the split positions.
        ///  Reuse the array in a loop for much better performance. Call
        ///  String8Set.GetLength to determine the array length required.
        /// </summary>
        /// <param name="delimiter">Delimiter on which to split</param>
        /// <param name="positionArray">int[] to contain split positions</param>
        /// <returns>String8Set containing the String8 split on the delimiter</returns>
        public String8Set Split(char delimiter, int[] positionArray)
        {
            return String8Set.Split(this, (byte)delimiter, positionArray);
        }

        /// <summary>
        ///  Split this String8 into a String8Set with the given delimiter.
        ///  Reuses the PartialArray to contain positions to avoid allocation.
        /// </summary>
        /// <param name="delimiter">Delimiter on which to split</param>
        /// <param name="positions">PartialArray&lt;int&gt; to contain split positions</param>
        /// <returns>String8Set containing the String8 split on the delimiter</returns>
        public String8Set Split(byte delimiter, PartialArray<int> positions)
        {
            return String8Set.Split(this, delimiter, positions);
        }

        /// <summary>
        ///  Split this String8 into a String8Set on the given delimiter,
        ///  but only outside double-quoted sections. Used for CSV parsing.
        /// </summary>
        /// <param name="delimiter">Delimiter on which to split</param>
        /// <param name="positions">PartialArray&lt;int&gt; to contain split positions</param>
        /// <returns>String8Set containing the String8 split on the delimiter</returns>
        public String8Set SplitOutsideQuotes(byte delimiter, PartialArray<int> positions)
        {
            return String8Set.SplitOutsideQuotes(this, delimiter, positions);
        }

        /// <summary>
        ///  Split this String8, which is a row in a CSV, into cells, and
        ///  unescape cell values. The buffer will be overwritten with the
        ///  cell values shifted to remove wrapping quotes and escaped quotes
        ///  and trailing nulls are written at the end.
        /// </summary>
        /// <param name="positions">PartialArray&lt;int&gt; to contain split positions</param>
        /// <returns>String8Set containing the String8 split into unescaped CSV cells</returns>
        public String8Set SplitAndDecodeCsvCells(PartialArray<int> positions)
        {
            return String8Set.SplitAndDecodeCsvCells(this, positions);
        }

        /// <summary>
        ///  Get the length in bytes required to convert value.
        /// </summary>
        /// <param name="value">string to measure</param>
        /// <returns>Number of bytes needed to store value in a String8 byte[]</returns>
        public static int GetLength(string value)
        {
            if (String.IsNullOrEmpty(value)) return 0;
            return Encoding.UTF8.GetByteCount(value);
        }

        /// <summary>
        ///  Get the length in bytes required to convert value.Substring(index, length).
        /// </summary>
        /// <param name="value">string to measure</param>
        /// <param name="index">Index of value substring to measure</param>
        /// <param name="length">Length to value substring to measure</param>
        /// <returns>Number of bytes needed to store value.Substring(index, length) in a String8 byte[]</returns>
        public static int GetLength(string value, int index, int length)
        {
            if (String.IsNullOrEmpty(value) || length == 0) return 0;

            // Issue: No UTF8.GetByteCount overload takes string index and length.
            // Overestimate with full length minus one byte per excluded character.
            return Encoding.UTF8.GetByteCount(value) - length;
        }
        #endregion

        #region Basic Properties
        /// <summary>
        ///  Get the length in bytes of this String8 value. This is not the
        ///  same as the character length of the string value if non-ASCII
        ///  characters are part of the value.
        /// </summary>
        public int Length
        {
            get { return _length; }
        }

        /// <summary>
        ///  Returns whether this is an empty (length zero) String8
        /// </summary>
        /// <returns>True if empty string, False otherwise</returns>
        public bool IsEmpty()
        {
            return _length == 0;
        }

        /// <summary>
        ///  Returns whether all characters in this String8 are ASCII.
        ///  Used to determine whether ASCII algorithms can be used on it.
        /// </summary>
        /// <returns>True if all ASCII, false otherwise</returns>
        public bool IsAscii()
        {
            for (int i = 0; i < _length; ++i)
            {
                if (_buffer[_index + i] >= 0x80) return false;
            }

            return true;
        }

        /// <summary>
        ///  Byte accessor
        /// </summary>
        /// <param name="index">Index of byte to get</param>
        /// <returns>UTF8 byte at index</returns>
        public byte this[int index]
        {
            get
            {
                return _buffer[_index + index];
            }
        }
        #endregion

        #region Basic Methods
        /// <summary>
        ///  Return a String8 for the value of this string after the given index.
        /// </summary>
        /// <param name="index">Index from which to include characters</param>
        /// <returns>String8 with the value of this string starting at index</returns>
        public String8 Substring(int index)
        {
            // Length Default: Return rest of string
            int length = _length - index;

            // Verify in bounds
            if (length < 0) throw new ArgumentOutOfRangeException("length");

            // Build a substring tied to the same buffer
            return new String8(_buffer, _index + index, length);
        }

        /// <summary>
        ///  Return a String8 with the value of this string from the given index,
        ///  for the given length.
        /// </summary>
        /// <param name="index">First index to include in substring</param>
        /// <param name="length">Number of bytes to include in substring</param>
        /// <returns>String8 with the value of this string starting at index for length bytes</returns>
        public String8 Substring(int index, int length)
        {
            // Verify in bounds
            if (index + length > _length) throw new ArgumentOutOfRangeException("length");

            // Build a substring tied to the same buffer
            return new String8(_buffer, _index + index, length);
        }

        /// <summary>
        ///  Return the first index at which the passed character appears in this string.
        /// </summary>
        /// <param name="c">Character to find</param>
        /// <param name="startIndex">First index at which to check</param>
        /// <returns>Index of first occurrence of character or -1 if not found</returns>
        public int IndexOf(byte c, int startIndex = 0)
        {
            for (int i = startIndex; i < _length; ++i)
            {
                if (_buffer[_index + i] == c) return i;
            }

            return -1;
        }

        /// <summary>
        ///  Return the last index at which the passed character appears in this string.
        /// </summary>
        /// <param name="c">Character to find</param>
        /// <param name="startIndex">First index at which to check</param>
        /// <returns>Index of last occurrence of character or -1 if not found</returns>
        public int LastIndexOf(byte c, int startIndex = -1)
        {
            if (startIndex == -1) startIndex = _length - 1;

            for (int i = startIndex; i >= 0; --i)
            {
                if (_buffer[_index + i] == c) return i;
            }

            return -1;
        }

        /// <summary>
        ///  Return whether this string ends with the given character.
        /// </summary>
        /// <param name="c">Character to check for</param>
        /// <returns>True if string ends with character, false otherwise</returns>
        public bool EndsWith(byte c)
        {
            if (_length == 0) return false;
            return (_buffer[_index + _length - 1] == c);
        }

        /// <summary>
        ///  Return whether this string starts with the given other string.
        /// </summary>
        /// <param name="other">The potential prefix to this string</param>
        /// <param name="ignoreCase">True for case insensitive comparison, False otherwise</param>
        /// <returns></returns>
        public bool StartsWith(String8 other, bool ignoreCase = false)
        {
            return other.CompareAsPrefixTo(this, ignoreCase) == 0;
        }

        /// <summary>
        ///  Make this String8 uppercase with invariant rules (ASCII characters only). 
        ///  This version changes the existing value in place; make a copy if you
        ///  need to preserve the original casing.
        /// </summary>
        public void ToUpperInvariant()
        {
            if (this._length <= 0) return;

            int end = this._index + this._length;
            for(int i = this._index; i < end; ++i)
            {
                byte c = this._buffer[i];
                if((byte)(c - UTF8.a) < UTF8.AlphabetLength)
                {
                    this._buffer[i] = (byte)(c - UTF8.ToUpperSubtract);
                }
            }
        }
        #endregion

        #region Type Conversions
        /// <summary>
        ///  Convert a String8 with a non-negative integer [digits only] to the numeric value.
        /// </summary>
        /// <returns>Numeric Value or -1 if not an integer</returns>
        public int ToInteger()
        {
            if (IsEmpty()) return -1;

            long value = 0;
            int end = _index + _length;
            for (int i = _index; i < end; ++i)
            {
                int digitValue = this._buffer[i] - UTF8.Zero;
                if (digitValue < 0 || digitValue > 9) return -1;

                value *= 10;
                value += digitValue;
                if (value > int.MaxValue) return -1;
            }

            return (int)value;
        }

        public static String8 FromInteger(int value, byte[] buffer)
        {
            if (buffer.Length < 11) throw new ArgumentException("String8.FromInteger requires an 11 byte buffer for integer conversion.");

            int i = 0;
            long valueLeft = value;

            // Write minus sign if negative
            if (valueLeft < 0)
            {
                valueLeft = -valueLeft;
                buffer[i++] = (byte)'-';
            }

            // Determine how many digits in value
            int digits = 1;
            int scale = 10;
            while (valueLeft >= scale && digits < 10)
            {
                digits++;
                scale *= 10;
            }

            // Write digits right to left
            for (int j = i + digits - 1; j >= i; --j)
            {
                long digit = valueLeft % 10;
                buffer[j] = (byte)(UTF8.Zero + (byte)digit);
                valueLeft /= 10;
            }

            return new String8(buffer, 0, i + digits);
        }
        #endregion

        #region IComparable
        /// <summary>
        ///  Compare this String8 to a .NET string. Will not allocate if the
        ///  other string is ASCII only.
        /// </summary>
        /// <param name="other">string to compare to</param>
        /// <returns>Negative if this String8 sorts earlier, zero if equal, positive if this String8 sorts later</returns>
        public int CompareTo(string other)
        {
            return CompareTo(other, false);
        }

        /// <summary>
        ///  Compare this String8 to a .NET string. Will not allocate if the
        ///  other string is ASCII only.
        /// </summary>
        /// <param name="other">string to compare to</param>
        /// <param name="ignoreCase">True for OrdinalIgnoreCase comparison, False for Ordinal comparison</param>
        /// <returns>Negative if this String8 sorts earlier, zero if equal, positive if this String8 sorts later</returns>
        public int CompareTo(string other, bool ignoreCase)
        {
            int thisLength = _length;
            int otherLength = other.Length;
            int commonLength = Math.Min(thisLength, otherLength);

            for (int i = 0; i < commonLength; ++i)
            {
                byte tC = _buffer[_index + i];

                char oC = other[i];
                if ((ushort)oC < 0x80)
                {
                    int cmp = (ignoreCase ? CompareOrdinalIgnoreCase(tC, (byte)oC) : tC.CompareTo((byte)oC));
                    if (cmp != 0) return cmp;
                }
                else
                {
                    // Multi-byte strings - fall back
                    return String.Compare(this.ToString(), other, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                }
            }

            return thisLength.CompareTo(otherLength);
        }

        /// <summary>
        ///  Compare this String8 to another one in Case Insensitive stable order.
        ///  This compare *does not* return zero for strings which differ only in casing.
        ///  Strings are sorted case insensitive, then values differing only by case are
        ///  sorted case sensitive.
        ///  
        ///  This sort is needed to produce a list in case-insensitive order (for search)
        ///  which is also consistent (to minimize diffs).
        /// </summary>
        /// <param name="other">String8 to compare to</param>
        /// <returns>Negative if this String8 sorts earlier, zero if equal, positive if later</returns>
        public int CompareCaseInsensitiveStableTo(String8 other)
        {
            // Compare case insensitive and sort according to those rules
            int cmp = this.CompareTo(other, true);
            if (cmp != 0) return cmp;

            // For ties (casing differences of the same word, sort in case sensitive order)
            return this.CompareTo(other, false);
        }

        /// <summary>
        ///  Compare this String8 to another one. Returns which String8 sorts earlier (ordinal comparison).
        /// </summary>
        /// <param name="other">String8 to compare to</param>
        /// <returns>Negative if this String8 sorts earlier, zero if equal, positive if this String8 sorts later</returns>
        public int CompareTo(String8 other)
        {
            return CompareTo(other, false);
        }

        /// <summary>
        ///  Compare this String8 to another one. Returns which String8 sorts earlier..
        /// </summary>
        /// <param name="other">String8 to compare to</param>
        /// <param name="ignoreCase">True for OrdinalIgnoreCase comparison, False for Ordinal comparison</param>
        /// <returns>Negative if this String8 sorts earlier, zero if equal, positive if this String8 sorts later</returns>
        public int CompareTo(String8 other, bool ignoreCase)
        {
            // If String8s point to the same thing, return the same
            if (other._index == _index && other._buffer == _buffer && other._length == _length) return 0;

            // If one or the other is empty, the non-empty one is greater
            if (this.IsEmpty())
            {
                if (other.IsEmpty()) return 0;
                return -1;
            }
            else if (other.IsEmpty())
            {
                return 1;
            }

            // Next, compare up to the length both strings are
            int cmp = (ignoreCase ? CompareToCommonLengthOrdinalIgnoreCase(other) : CompareToCommonLength(other));
            if (cmp != 0) return cmp;

            // If all bytes are equal, the longer one is later
            return _length.CompareTo(other._length);
        }

        /// <summary>
        ///  Compare this String8 as a prefix to another String8 (ordinal comparison).
        ///  You must call prefix.CompareTo(longerValue), not the other way around.
        /// </summary>
        /// <param name="other">String8 to compare to</param>
        /// <param name="ignoreCase">True for OrdinalIgnoreCase comparison, False for Ordinal comparison</param>
        /// <returns>0 if this is a prefix of other, negative if this sorts earlier 
        /// than the prefix of other, positive if this sorts later</returns>
        public int CompareAsPrefixTo(String8 other, bool ignoreCase = false)
        {
            // Empty is a prefix for everything. If other is empty, this sorts after other.
            if (this.IsEmpty()) return 0;
            if (other.IsEmpty()) return 1;

            // If String8s point to the same thing, return the same
            if (other._index == _index && other._buffer == _buffer && other._length == _length) return 0;

            // Next, compare up to the length both strings are
            int cmp = (ignoreCase ? CompareToCommonLengthOrdinalIgnoreCase(other) : CompareToCommonLength(other));
            if (cmp != 0) return cmp;

            // If all bytes are equal and this is not longer than 'other', this is a prefix
            if (_length <= other._length) return 0;

            // Otherwise (other is shorter), this sorts after other
            return 1;
        }

        private int CompareToCommonLength(String8 other)
        {
            // Otherwise, compare the common length
            int thisLength = _length;
            int otherLength = other._length;
            int commonLength = Math.Min(thisLength, otherLength);

            int i = 0;

            // Compare four-at-a-time while strings are long enough
            for (; i < commonLength - 3; i += 4)
            {
                int c1 = _buffer[_index + i].CompareTo(other._buffer[other._index + i]);
                if (c1 != 0) return c1;

                int c2 = _buffer[_index + i + 1].CompareTo(other._buffer[other._index + i + 1]);
                if (c2 != 0) return c2;

                int c3 = _buffer[_index + i + 2].CompareTo(other._buffer[other._index + i + 2]);
                if (c3 != 0) return c3;

                int c4 = _buffer[_index + i + 3].CompareTo(other._buffer[other._index + i + 3]);
                if (c4 != 0) return c4;
            }

            // Compare the end one-at-a-time
            for (; i < commonLength; ++i)
            {
                int cmp = _buffer[_index + i].CompareTo(other._buffer[other._index + i]);
                if (cmp != 0) return cmp;
            }

            return 0;
        }

        private int CompareToCommonLengthOrdinalIgnoreCase(String8 other)
        {
            // ISSUE: This is the same as .NET for ASCII strings. Not sure what OrdinalIgnoreCase
            // does for non-ASCII. This will just be an ordinal comparison of those.

            // Otherwise, compare the common length
            int thisLength = _length;
            int otherLength = other._length;
            int commonLength = Math.Min(thisLength, otherLength);

            int i = 0;

            // Compare four-at-a-time while strings are long enough
            for (; i < commonLength - 3; i += 4)
            {
                int c1 = CompareOrdinalIgnoreCase(_buffer[_index + i], other._buffer[other._index + i]);
                if (c1 != 0) return c1;

                int c2 = CompareOrdinalIgnoreCase(_buffer[_index + i + 1], other._buffer[other._index + i + 1]);
                if (c2 != 0) return c2;

                int c3 = CompareOrdinalIgnoreCase(_buffer[_index + i + 2], other._buffer[other._index + i + 2]);
                if (c3 != 0) return c3;

                int c4 = CompareOrdinalIgnoreCase(_buffer[_index + i + 3], other._buffer[other._index + i + 3]);
                if (c4 != 0) return c4;
            }

            // Compare the end one-at-a-time
            for (; i < commonLength; ++i)
            {
                int cmp = CompareOrdinalIgnoreCase(_buffer[_index + i], other._buffer[other._index + i]);
                if (cmp != 0) return cmp;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CompareOrdinalIgnoreCase(byte left, byte right)
        {
            // Make uppercase (ASCII) - like String.CompareOrdinalIgnoreCaseHelper
            // Only one compare per character
            // Values before 'a' (all uppercase and non-alpha values) will wrap around and be greater than length.
            // Values after 'z' (after alphabet and multi-byte) will remain larger than AlphabetLength.
            if ((byte)(left - UTF8.a) < UTF8.AlphabetLength) left -= UTF8.ToUpperSubtract;
            if ((byte)(right - UTF8.a) < UTF8.AlphabetLength) right -= UTF8.ToUpperSubtract;

            // Negative if left earlier, zero if equal, positive if left later
            return left - right;
        }
        #endregion

        #region Output
        /// <summary>
        ///  Write this value to the target byte[] at the given index as UTF8.
        /// </summary>
        /// <param name="buffer">byte[] to write to</param>
        /// <param name="index">Index at which to write</param>
        /// <returns>Byte count written</returns>
        public int WriteTo(byte[] buffer, int index)
        {
            if (_length <= 0) return 0;
            if (index + _length > buffer.Length) throw new ArgumentException(String.Format(Resources.BufferTooSmall, index + _length, buffer.Length));
            System.Buffer.BlockCopy(_buffer, _index, buffer, index, _length);
            return _length;
        }

        /// <summary>
        ///  Write this value to the target TextWriter.
        /// </summary>
        /// <param name="writer">TextWriter to write to</param>
        /// <returns>Character count written</returns>
        public int WriteTo(TextWriter writer)
        {
            if (_length <= 0) return 0;

            int length = _length;
            int end = _index + length;

            for (int index = _index; index < end; ++index)
            {
                byte current = _buffer[index];
                if (current < 0x80)
                {
                    // Single Byte characters - write one-by-one
                    writer.Write((char)_buffer[index]);
                }
                else
                {
                    // Multi-byte - make the UTF8 decoder convert the rest
                    string stringSuffix = Encoding.UTF8.GetString(_buffer, index, end - index);
                    writer.Write(stringSuffix);

                    length = (index - _index) + stringSuffix.Length;
                    break;
                }
            }

            return length;
        }

        /// <summary>
        ///  Write this value to the target Stream as UTF8.
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        /// <returns>Byte count written</returns>
        public int WriteTo(Stream stream)
        {
            if (_length <= 0) return 0;

            stream.Write(_buffer, _index, _length);
            return _length;
        }
        #endregion

        #region Object Overrides
        public static bool operator ==(String8 left, String8 right)
        {
            return left.CompareTo(right) == 0;
        }

        public static bool operator !=(String8 left, String8 right)
        {
            return left.CompareTo(right) != 0;
        }

        public override bool Equals(object o)
        {
            if (o is String8)
            {
                return this.CompareTo((String8)o) == 0;
            }
            else if (o is string)
            {
                return this.CompareTo((string)o) == 0;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            // Jenkins One-at-a-Time Hash. https://en.wikipedia.org/wiki/Jenkins_hash_function
            uint hash = 0;

            for (int i = 0; i < _length; i++)
            {
                hash += _buffer[_index + i];
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }

            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);

            return unchecked((int)hash);
        }

        public override string ToString()
        {
            if (this.IsEmpty()) return String.Empty;
            return Encoding.UTF8.GetString(_buffer, _index, _length);
        }
        #endregion

        #region IBinarySerializable
        public void WriteBinary(BinaryWriter w)
        {
            w.Write(_length);
            if (_length > 0) w.Write(_buffer, _index, _length);
        }

        public void ReadBinary(BinaryReader r)
        {
            _length = r.ReadArrayLength(1);
            if (_length > 0) _buffer = r.ReadBytes(_length);
            _index = 0;
        }
        #endregion
    }
}
