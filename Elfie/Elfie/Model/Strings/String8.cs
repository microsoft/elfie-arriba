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
        public byte[] Array { get; private set; }
        public int Index { get; private set; }
        public int Length { get; private set; }

        public String8(byte[] array, int index, int length)
        {
            Array = array;
            Index = index;
            Length = length;
        }

        public static String8 Empty = new String8(null, 0, 0);
        private static String8 s_true8 = String8.Convert("true", new byte[4]);
        private static String8 s_false8 = String8.Convert("false", new byte[5]);

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
        ///  Returns whether this is an empty (length zero) String8
        /// </summary>
        /// <returns>True if empty string, False otherwise</returns>
        public bool IsEmpty()
        {
            return Length == 0;
        }

        /// <summary>
        ///  Returns whether all characters in this String8 are ASCII.
        ///  Used to determine whether ASCII algorithms can be used on it.
        /// </summary>
        /// <returns>True if all ASCII, false otherwise</returns>
        public bool IsAscii()
        {
            for (int i = 0; i < Length; ++i)
            {
                if (Array[Index + i] >= 0x80) return false;
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
                return Array[Index + index];
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
            // Verify index non-negative
            if (index < 0 || index > Length) throw new ArgumentOutOfRangeException("index");

            // If index is zero, return the same instance
            if (index == 0) return this;

            // Build a substring tied to the same buffer
            return new String8(Array, Index + index, Length - index);
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
            if (index < 0) throw new ArgumentOutOfRangeException("index");
            if (length < 0 || index + length > Length) throw new ArgumentOutOfRangeException("length");

            // Build a substring tied to the same buffer
            return new String8(Array, Index + index, length);
        }

        /// <summary>
        ///  Return the first index at which the passed character appears in this string.
        /// </summary>
        /// <param name="c">Character to find</param>
        /// <param name="startIndex">First index at which to check</param>
        /// <returns>Index of first occurrence of character or -1 if not found</returns>
        public int IndexOf(byte c, int startIndex = 0)
        {
            int end = Index + Length;
            for (int i = Index + startIndex; i < end; ++i)
            {
                if (Array[i] == c) return i - Index;
            }

            return -1;
        }

        /// <summary>
        ///  Return the portion of the String8 before the first occurrence of 'c',
        ///  or the whole value if 'b' is not in the String8.
        /// </summary>
        /// <param name="value">String8 to scan</param>
        /// <param name="c">Byte to find</param>
        /// <returns>value before first occurrence of 'c', or all of value if 'c' not found</returns>
        public String8 BeforeFirst(byte c)
        {
            if (this.IsEmpty()) return this;
            int index = this.IndexOf(c);
            if (index < 0) return this;
            return this.Substring(0, index);
        }

        /// <summary>
        ///  Return the portion of the String8 after the first occurrence of 'c',
        ///  or the whole value if 'b' is not in the String8.
        /// </summary>
        /// <param name="value">String8 to scan</param>
        /// <param name="c">Byte to find</param>
        /// <returns>value after first occurrence of 'c', or all of value if 'c' not found</returns>
        public String8 AfterFirst(byte c)
        {
            if (this.IsEmpty()) return this;
            int index = this.IndexOf(c);
            if (index < 0) return this;
            return this.Substring(index + 1);
        }

        /// <summary>
        ///  Split this String8 on the first occurrence of splitter.
        /// </summary>
        /// <param name="splitter">UTF-8 byte to split on</param>
        /// <param name="beforeSplitter">Value before first splitter, or String8.Empty if no splitter</param>
        /// <param name="afterSplitter">Value after first splitter, or String8.Empty if no splitter</param>
        /// <returns>True if splitter found, False if no splitters found</returns>
        public bool SplitOnFirst(byte splitter, out String8 beforeSplitter, out String8 afterSplitter)
        {
            int index = this.IndexOf(splitter);
            if (index >= 0)
            {
                beforeSplitter = this.Substring(0, index);
                afterSplitter = this.Substring(index + 1);
                return true;
            }
            else
            {
                beforeSplitter = String8.Empty;
                afterSplitter = String8.Empty;
                return false;
            }
        }

        /// <summary>
        ///  Return the last index at which the passed character appears in this string.
        /// </summary>
        /// <param name="c">Character to find</param>
        /// <param name="startIndex">First index at which to check</param>
        /// <returns>Index of last occurrence of character or -1 if not found</returns>
        public int LastIndexOf(byte c, int startIndex = -1)
        {
            if (startIndex == -1) startIndex = Length - 1;

            for (int i = startIndex; i >= 0; --i)
            {
                if (Array[Index + i] == c) return i;
            }

            return -1;
        }

        /// <summary>
        ///  Return this string with leading and trailing whitespace removed
        /// </summary>
        /// <returns>String8 without surrounding whitespace</returns>
        public String8 Trim()
        {
            int startIndex = Index;
            for (; startIndex < Index + Length; ++startIndex)
            {
                if (!IsWhiteSpace(Array[startIndex])) break;
            }

            int endIndex = Index + Length - 1;
            for (; endIndex > startIndex; --endIndex)
            {
                if (!IsWhiteSpace(Array[endIndex])) break;
            }

            return new String8(Array, startIndex, endIndex - startIndex + 1);
        }

        private static bool IsWhiteSpace(byte c)
        {
            // See System.Char.IsWhiteSpaceLatin1
            return (c == UTF8.Space || (c >= 0x9 && c <= 0xd) || c == 0xA0 || c == 0x85);
        }

        /// <summary>
        ///  Return this string with all trailing 'c's removed.
        /// </summary>
        /// <param name="c">Character to trim</param>
        /// <returns>String8 without any trailing copies of c</returns>
        public String8 TrimEnd(byte c)
        {
            int index = Index + Length - 1;
            for (; index >= Index; --index)
            {
                if (Array[index] != c) break;
            }

            return new String8(Array, Index, index - Index + 1);
        }

        /// <summary>
        ///  Return whether this string ends with the given character.
        /// </summary>
        /// <param name="c">Character to check for</param>
        /// <returns>True if string ends with character, false otherwise</returns>
        public bool StartsWith(byte c)
        {
            if (Length == 0) return false;
            return (Array[Index] == c);
        }

        /// <summary>
        ///  Return whether this string ends with the given character.
        /// </summary>
        /// <param name="c">Character to check for</param>
        /// <returns>True if string ends with character, false otherwise</returns>
        public bool EndsWith(byte c)
        {
            if (Length == 0) return false;
            return (Array[Index + Length - 1] == c);
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
        ///  Return whether this string starts with the given other string.
        /// </summary>
        /// <param name="other">The potential prefix to this string</param>
        /// <param name="ignoreCase">True for case insensitive comparison, False otherwise</param>
        /// <returns></returns>
        public bool EndsWith(String8 other, bool ignoreCase = false)
        {
            if (other.Length > this.Length) return false;
            return other.CompareTo(this.Substring(this.Length - other.Length), ignoreCase) == 0;
        }

        /// <summary>
        ///  Make this String8 uppercase with invariant rules (ASCII characters only). 
        ///  This version changes the existing value in place; make a copy if you
        ///  need to preserve the original casing.
        /// </summary>
        public void ToUpperInvariant()
        {
            if (this.Length <= 0) return;

            int end = this.Index + this.Length;
            for (int i = this.Index; i < end; ++i)
            {
                byte c = this.Array[i];
                if ((byte)(c - UTF8.a) < UTF8.AlphabetLength)
                {
                    this.Array[i] = (byte)(c - UTF8.ToUpperSubtract);
                }
            }
        }

        /// <summary>
        ///  Make this String8 lowercase with invariant rules (ASCII characters only). 
        ///  This version changes the existing value in place; make a copy if you
        ///  need to preserve the original casing.
        /// </summary>
        public void ToLowerInvariant()
        {
            if (this.Length <= 0) return;

            int end = this.Index + this.Length;
            for (int i = this.Index; i < end; ++i)
            {
                byte c = this.Array[i];
                if ((byte)(c - UTF8.A) < UTF8.AlphabetLength)
                {
                    this.Array[i] = (byte)(c + UTF8.ToUpperSubtract);
                }
            }
        }
        #endregion

        #region Type Conversions
        public bool TryToBoolean(out bool result)
        {
            result = false;
            if (IsEmpty()) return false;

            if (this.CompareTo("true", true) == 0)
            {
                result = true;
                return true;
            }
            else if (this.CompareTo("false", true) == 0)
            {
                result = false;
                return true;
            }
            else if (this.CompareTo("1", false) == 0)
            {
                result = true;
                return true;
            }
            else if (this.CompareTo("0", false) == 0)
            {
                result = false;
                return true;
            }

            return false;
        }

        private static byte ParseDigit(byte digit, ref bool valid)
        {
            byte value = (byte)(digit - UTF8.Zero);
            valid &= value <= 9;
            return value;
        }

        private ulong ParseWithCutoff(ulong cutoff, ref bool valid)
        {
            ulong value = 0;

            if (Length == 20 && cutoff == ulong.MaxValue)
            {
                // Max Length: Parse all but last digit
                value = 10 * this.Substring(0, this.Length - 1).ParseWithCutoff(ulong.MaxValue / 10, ref valid);

                // Limit Last digit so sum is <= ulong.MaxValue
                value += this.Substring(this.Length - 1).ParseWithCutoff(ulong.MaxValue - value, ref valid);

                if (!valid) value = 0;
                return value;
            }

            // Validate non-empty and not too long to convert
            valid &= Length > 0 && Length < 20;

            // Stop early if too long or short
            if (!valid) return 0;

            // Convert the digits
            // NOTE: Don't need to check digits valid, because even 19 255 digits won't overflow
            int end = Index + Length;
            for (int i = Index; i < end; ++i)
            {
                value = (10 * value) + ParseDigit(Array[i], ref valid);
            }

            // Validate under cutoff
            valid &= value <= cutoff;

            // Always return zero when invalid
            if (!valid) value = 0;

            return value;
        }

        private long Negate(ulong value, ref bool valid)
        {
            // Validate value is non-zero
            valid &= (value != 0);
            if (!valid) return 0;

            // Decrement to ensure in range, then cast
            long inRange = (long)(value - 1);

            // Negate and undo the decrement
            return -inRange - 1;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the byte representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToByte(out byte result)
        {
            bool valid = true;
            result = (byte)ParseWithCutoff(byte.MaxValue, ref valid);
            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the sbyte representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToSByte(out sbyte result)
        {
            bool valid = true;

            if (this.StartsWith(UTF8.Dash))
            {
                // Negative: Parse after dash, negate safely, cast and return
                result = (sbyte)Negate(this.Substring(1).ParseWithCutoff(-sbyte.MinValue, ref valid), ref valid);
            }
            else
            {
                result = (sbyte)ParseWithCutoff((int)sbyte.MaxValue, ref valid);
            }

            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the ushort representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToUShort(out ushort result)
        {
            bool valid = true;
            result = (ushort)ParseWithCutoff(ushort.MaxValue, ref valid);
            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the short representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToShort(out short result)
        {
            bool valid = true;

            if (this.StartsWith(UTF8.Dash))
            {
                // Negative: Parse after dash, negate safely, cast and return
                result = (short)Negate(this.Substring(1).ParseWithCutoff(-short.MinValue, ref valid), ref valid);
            }
            else
            {
                result = (short)ParseWithCutoff((int)short.MaxValue, ref valid);
            }

            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the int representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToInteger(out int result)
        {
            bool valid = true;

            if (this.StartsWith(UTF8.Dash))
            {
                // Negative: Parse after dash, negate safely, cast and return
                result = (int)Negate(this.Substring(1).ParseWithCutoff(((ulong)int.MaxValue) + 1, ref valid), ref valid);
            }
            else
            {
                result = (int)ParseWithCutoff(int.MaxValue, ref valid);
            }

            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the uint representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToUInt(out uint result)
        {
            bool valid = true;
            result = (uint)ParseWithCutoff(uint.MaxValue, ref valid);
            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the long representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToLong(out long result)
        {
            bool valid = true;

            if (this.StartsWith(UTF8.Dash))
            {
                // Negative: Parse after dash, negate safely, cast and return
                result = Negate(this.Substring(1).ParseWithCutoff(((ulong)long.MaxValue) + 1, ref valid), ref valid);
            }
            else
            {
                // Positive: Parse and Convert normally
                result = (long)ParseWithCutoff(long.MaxValue, ref valid);
            }

            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a numeric value to the ulong representation, if in range.
        /// </summary>
        /// <param name="result">Numeric value found, if in range, otherwise zero</param>
        /// <returns>True if valid, False otherwise</returns>
        public bool TryToULong(out ulong result)
        {
            bool valid = true;
            result = ParseWithCutoff(ulong.MaxValue, ref valid);
            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a floating point value to the double representation, if valid.
        ///  *Only handles decimal format for now (no base+exponent form).*
        /// </summary>
        /// <param name="result">Numeric value found, if valid number, otherwise zero.</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool TryToDouble(out double result)
        {
            bool valid = true;
            result = 0.0;
            if (this.Length == 0) return false;

            bool negative = StartsWith(UTF8.Dash);
            int decimalPointIndex = IndexOf(UTF8.Period);
            if (decimalPointIndex == -1) decimalPointIndex = this.Length;

            // Parse the part after the decimal point, scale, and add to the result
            if (decimalPointIndex != this.Length)
            {
                String8 fractionalPart = this.Substring(decimalPointIndex + 1);
                result = (double)fractionalPart.ParseWithCutoff(ulong.MaxValue, ref valid) / Math.Pow(10, fractionalPart.Length);
            }

            // Parse the whole number part (without the minus sign, if any) and add to the result
            int firstDigitIndex = (negative ? 1 : 0);
            if (decimalPointIndex - firstDigitIndex > 0)
            {
                String8 wholePart = this.Substring(firstDigitIndex, decimalPointIndex - firstDigitIndex);
                result += wholePart.ParseWithCutoff(ulong.MaxValue, ref valid);
            }

            // Negate the result if a minus sign was found
            if (negative) result = -result;

            return valid;
        }

        /// <summary>
        ///  Convert a String8 with a floating point value to the double representation, if valid.
        /// </summary>
        /// <param name="result">Numeric value found, if valid number, otherwise zero.</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool TryToFloat(out float result)
        {
            double inner;
            bool valid = TryToDouble(out inner);
            result = (float)inner;
            return valid;
        }

        /// <summary>
        ///  Convert a boolean to a String8 value.
        /// </summary>
        /// <param name="value">Boolean to convert</param>
        /// <returns>String8 for boolean: "True" or "False"</returns>
        public static String8 FromBoolean(bool value)
        {
            return (value ? s_true8 : s_false8);
        }

        /// <summary>
        ///  Convert an integer into the equivalent String8 representation, using the provided buffer.
        ///  Buffer must be at least 11 bytes long to handle all values.
        /// </summary>
        /// <param name="value">Integer value to convert</param>
        /// <param name="buffer">byte[] for conversion (at least length 21 for all values)</param>
        /// <returns>String8 representation of integer value</returns>
        public static String8 FromInteger(int value, byte[] buffer)
        {
            return FromNumber(value, buffer, 0, 1);
        }

        /// <summary>
        ///  Convert an integer into the equivalent String8 representation, using the provided buffer.
        ///  Buffer must be at least 11 bytes long to handle all values.
        /// </summary>
        /// <param name="value">Integer value to convert</param>
        /// <param name="buffer">byte[] for conversion (at least length 21 for all values)</param>
        /// <param name="index">Index within byte[] at which to being writing</param>
        /// <param name="minimumDigits">Minimum integer length (leading zeros written if needed)</param>
        /// <returns>String8 representation of integer value</returns>
        public static String8 FromInteger(int value, byte[] buffer, int index, int minimumDigits = 1)
        {
            return FromNumber(value, buffer, index, minimumDigits);
        }

        /// <summary>
        ///  Convert a number into the equivalent String8 representation, using the provided buffer.
        ///  Buffer must be at least 11 bytes long to handle all values.
        /// </summary>
        /// <param name="value">long value to convert</param>
        /// <param name="buffer">byte[] for conversion (at least length 11 for all values)</param>
        /// <param name="index">Index within byte[] at which to being writing</param>
        /// <param name="minimumDigits">Minimum integer length (leading zeros written if needed)</param>
        /// <returns>String8 representation of integer value</returns>
        public static String8 FromNumber(long value, byte[] buffer, int index = 0, int minimumDigits = 1)
        {
            if (value >= 0)
            {
                return FromNumber((ulong)value, false, buffer, index, minimumDigits);
            }
            else
            {
                return FromNumber((ulong)(-value), true, buffer, index, minimumDigits);
            }
        }

        /// <summary>
        ///  Convert a floating point number into the equivalent String8 representation, using the provided buffer.
        ///  Buffer must be at least 11 bytes long to handle all values.
        /// </summary>
        /// <param name="value">ulong value to convert</param>
        /// <param name="isNegative">True if the original value was negative</param>
        /// <param name="buffer">byte[] for conversion (at least length 11 for all values)</param>
        /// <param name="index">Index within byte[] at which to being writing</param>
        /// <param name="minimumDigits">Minimum integer length (leading zeros written if needed)</param>
        /// <returns>String8 representation of integer value</returns>
        public static String8 FromNumber(ulong value, bool isNegative, byte[] buffer, int index = 0, int minimumDigits = 1)
        {
            ulong valueLeft = value;

            // Determine how many digits in value
            int digits = 1;
            ulong scale = 10;
            while (valueLeft >= scale && digits < 20)
            {
                digits++;
                scale *= 10;
            }

            // Enforce a digit minimum, if passed
            if (digits < minimumDigits) digits = minimumDigits;

            // Validate buffer is long enough
            int requiredLength = digits;
            if (isNegative) requiredLength++;
            if (buffer.Length - index < requiredLength) throw new ArgumentException("String8.FromNumber requires an 21 byte buffer for number conversion.");

            // Write minus sign if negative
            int digitStartIndex = index;
            if (isNegative)
            {
                buffer[index] = (byte)'-';
                digitStartIndex++;
            }

            // Write digits right to left
            for (int j = digitStartIndex + digits - 1; j >= digitStartIndex; --j)
            {
                ulong digit = valueLeft % 10;
                buffer[j] = (byte)(UTF8.Zero + (byte)digit);
                valueLeft /= 10;
            }

            return new String8(buffer, index, requiredLength);
        }

        /// <summary>
        ///  Convert a number into the equivalent String8 representation, using the provided buffer.
        /// </summary>
        /// <param name="value">double value to convert</param>
        /// <param name="isNegative">True if the original value was negative</param>
        /// <param name="buffer">byte[] for conversion (at least length 21 for all values)</param>
        /// <param name="index">Index within byte[] at which to being writing</param>
        /// <param name="minimumDigits">Minimum integer length (leading zeros written if needed)</param>
        /// <returns>String8 representation of integer value</returns>
        public static String8 FromNumber(double value, byte[] buffer, int index = 0, int minimumDigits = 1)
        {
            if (value >= 0)
            {
                return FromNumber(value, false, buffer, index, minimumDigits);
            }
            else
            {
                return FromNumber(-value, true, buffer, index, minimumDigits);
            }
        }

        /// <summary>
        ///  Convert a number into the equivalent String8 representation, using the provided buffer.
        /// </summary>
        /// <param name="value">ulong value to convert</param>
        /// <param name="isNegative">True if the original value was negative</param>
        /// <param name="buffer">byte[] for conversion (at least length 21 for all values)</param>
        /// <param name="index">Index within byte[] at which to being writing</param>
        /// <param name="minimumDigits">Minimum integer length (leading zeros written if needed)</param>
        /// <returns>String8 representation of integer value</returns>
        private static String8 FromNumber(double value, bool isNegative, byte[] buffer, int index = 0, int minimumDigits = 1)
        {
            // Split the whole and fractional parts of the number
            ulong wholePart = (ulong)value;
            double fractionalPart = (value - (double)wholePart);

            // Write out the whole part of the number
            String8 result = FromNumber(wholePart, isNegative, buffer, index, minimumDigits);

            // Write out the fractional part, if any, up to a maximum overall digits of precision (16).
            int digitsOfPrecisionLeft = 16 - result.Length;
            if (digitsOfPrecisionLeft > 0)
            {
                ulong scaledWholePart = (ulong)(fractionalPart * Math.Pow(10, digitsOfPrecisionLeft));
                
                // Trim trailing zeros
                while(scaledWholePart > 0 && (scaledWholePart % 10 == 0))
                {
                    scaledWholePart /= 10;
                    digitsOfPrecisionLeft--;
                }

                // If there's a value left, write the digits
                if (scaledWholePart > 0)
                {
                    if (index + result.Length + 1 + digitsOfPrecisionLeft >= buffer.Length) throw new ArgumentException("String8.FromNumber requires up to a 21 byte buffer per number conversion.");
                    buffer[index + result.Length] = UTF8.Period;
                    String8 fractionalPart8 = FromNumber(scaledWholePart, false, buffer, index + result.Length + 1, digitsOfPrecisionLeft);

                    result = new String8(result.Array, result.Index, result.Length + 1 + fractionalPart8.Length);
                }
            }

            return result;
        }

        /// <summary>
        ///  Convert a UTC DateTime into an ISO-8601 format string [yyyy-MM-ddThh:mm:ssZ],
        ///  without allocation.
        /// </summary>
        /// <param name="value">UTC DateTime to convert</param>
        /// <param name="index">Index at which to convert</param>
        /// <param name="buffer">byte[] at least 20 bytes long to convert into</param>
        /// <returns>Converted DateTime</returns>
        public static String8 FromDateTime(DateTime value, byte[] buffer, int index = 0)
        {
            if (buffer.Length + index < 20) throw new ArgumentException("String8.FromDateTime requires a 20 byte buffer for conversion.");

            int length = 10;

            // yyyy-MM-dd
            FromInteger(value.Year, buffer, index + 0, 4);
            buffer[index + 4] = UTF8.Dash;
            FromInteger(value.Month, buffer, index + 5, 2);
            buffer[index + 7] = UTF8.Dash;
            FromInteger(value.Day, buffer, index + 8, 2);

            // Thh:mm:ssZ
            if (value.TimeOfDay > TimeSpan.Zero)
            {
                length = 20;

                buffer[index + 10] = UTF8.T;
                FromInteger(value.Hour, buffer, index + 11, 2);
                buffer[index + 13] = UTF8.Colon;
                FromInteger(value.Minute, buffer, index + 14, 2);
                buffer[index + 16] = UTF8.Colon;
                FromInteger(value.Second, buffer, index + 17, 2);
                buffer[index + 19] = UTF8.Z;
            }

            return new String8(buffer, index, length);
        }

        /// <summary>
        ///  Convert a String8 with an ISO-8601 UTC DateTime into the DateTime value.
        ///  [yyyy-MM-dd] or [yyyy-MM-ddThh:mm:ssZ]
        /// </summary>
        /// <param name="result">UTC DateTime corresponding to string, if it was a valid DateTime</param>
        /// <returns>True if an integer was found, False otherwise.</returns>
        public bool TryToDateTime(out DateTime result)
        {
            result = DateTime.MinValue;
            if (this.IsEmpty()) return false;

            // Look for ISO 8601 Format [yyyy-MM-dd] or [yyyy-MM-ddThh:mm:ssZ]
            if (TryToDateTimeAsIso8601(out result)) return true;

            // Look for US format [MM/dd/yyyy] or [MM/dd/yyyy hh:mm:ssZ]
            if (TryToDateTimeAsUs(out result)) return true;

            return false;
        }

        /// <summary>
        ///  Convert a String8 to a DateTime with specific indices.
        /// </summary>
        /// <param name="result">UTC DateTime converted, or DateTime.MinValue if invalid</param>
        /// <param name="yearIndex">Index of four-digit year in string</param>
        /// <param name="monthIndex">Index of two-digit month in string</param>
        /// <param name="dayIndex">Index of two-digit day in string</param>
        /// <returns>True if valid DateTime, False otherwise</returns>
        public bool TryToDateTimeExact(out DateTime result, int yearIndex, int monthIndex, int dayIndex)
        {
            result = DateTime.MinValue;

            uint year, month, day;

            // Parse the date numbers
            if (!this.Substring(yearIndex, 4).TryToUInt(out year)) return false;
            if (!this.Substring(monthIndex, 2).TryToUInt(out month)) return false;
            if (!this.Substring(dayIndex, 2).TryToUInt(out day)) return false;

            // Validate the date number ranges (no month-specific day validation)
            if (month < 1 || month > 12) return false;
            if (day < 1 || day > 31) return false;

            // Construct DateTime to avoid failures due to days being out of range (leap year and month length)
            result = new DateTime((int)year, (int)month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (day > 1) result = result.AddDays(day - 1);

            // Return false for invalid leap days
            if (result.Month != month) return false;

            return true;
        }

        /// <summary>
        ///  Convert a String8 to a DateTime with specific indices.
        /// </summary>
        /// <param name="result">UTC DateTime converted, or DateTime.MinValue if invalid</param>
        /// <param name="yearIndex">Index of four-digit year in string</param>
        /// <param name="monthIndex">Index of two-digit month in string</param>
        /// <param name="dayIndex">Index of two-digit day in string</param>
        /// <returns>True if valid DateTime, False otherwise</returns>
        public bool TryToDateTimeExact(out DateTime result, int yearIndex, int monthIndex, int dayIndex, int hourIndex, int minuteIndex, int secondIndex)
        {
            // Convert the Date first
            if (!TryToDateTimeExact(out result, yearIndex, monthIndex, dayIndex)) return false;

            uint hour, minute, second;

            // Parse the time numbers
            if (!this.Substring(hourIndex, 2).TryToUInt(out hour)) return false;
            if (!this.Substring(minuteIndex, 2).TryToUInt(out minute)) return false;
            if (!this.Substring(secondIndex, 2).TryToUInt(out second)) return false;

            // Validate the time number ranges
            if (hour > 23) return false;
            if (minute > 59) return false;
            if (second > 59) return false;

            // Add the time part
            result = result.Add(new TimeSpan((int)hour, (int)minute, (int)second));

            return true;
        }

        private bool TryToDateTimeAsIso8601(out DateTime result)
        {
            result = DateTime.MinValue;

            // Formats are [yyyy-MM-dd] (length 10) or [yyyy-MM-ddThh:mm:ssZ] (length 19/20) or [yyyy-MM-ddThh:mm:ss.0000000]
            //              0123456789                  01234567890123456789
            bool hasTimePart = (Length >= 19 && Length <= 27);
            if (Length != 10 && !hasTimePart) return false;

            // Validate date part separators
            if (Array[Index + 4] != UTF8.Dash) return false;
            if (Array[Index + 7] != UTF8.Dash) return false;

            // If there's no time part, convert now that format is validated
            if (!hasTimePart) return TryToDateTimeExact(out result, 0, 5, 8);

            // Validate time part separators and suffix
            if (Array[Index + 10] != UTF8.T && Array[Index + 10] != UTF8.Space) return false;
            if (Array[Index + 13] != UTF8.Colon) return false;
            if (Array[Index + 16] != UTF8.Colon) return false;
            if (Length >= 20 && Array[Index + 19] != UTF8.Z && Array[Index + 19] != UTF8.Period) return false;

            // Convert with time part
            bool success = TryToDateTimeExact(out result, 0, 5, 8, 11, 14, 17);

            // Parse partial seconds
            if (Length > 20)
            {
                uint partialSeconds;
                if (!this.Substring(20).TryToUInt(out partialSeconds)) return false;

                double asSecondPart = (double)partialSeconds / (double)(Math.Pow(10, Length - 20));
                result = result.AddSeconds(asSecondPart);
            }

            return success;
        }

        private bool TryToDateTimeAsUs(out DateTime result)
        {
            result = DateTime.MinValue;

            // Formats are [MM/dd/yyyy] (length 10) or [MM/dd/yyyy hh:mm:ss] (length 19/20)
            //              0123456789                  01234567890123456789
            bool hasTimePart = (Length == 19 || Length == 20);
            if (Length != 10 && !hasTimePart) return false;

            // Validate date part separators
            if (Array[Index + 2] != UTF8.Slash) return false;
            if (Array[Index + 5] != UTF8.Slash) return false;

            // If there's no time part, convert now that format is validated
            if (!hasTimePart) return TryToDateTimeExact(out result, 6, 0, 3);

            // Validate time part separators and suffix
            if (Array[Index + 10] != UTF8.Space) return false;
            if (Array[Index + 13] != UTF8.Colon) return false;
            if (Array[Index + 16] != UTF8.Colon) return false;
            if (Length == 20 && Array[Index + 19] != UTF8.Z) return false;

            // Convert with time part
            return TryToDateTimeExact(out result, 6, 0, 3, 11, 14, 17);
        }

        /// <summary>
        ///  Convert a TimeSpan into DDD.HH:MM:SS.mmm format.
        /// </summary>
        /// <param name="value">UTC TimEspan to convert</param>
        /// <param name="index">Index at which to convert</param>
        /// <param name="buffer">byte[] at least 20 bytes long to convert into</param>
        /// <returns>Converted TimeSpan</returns>
        public static String8 FromTimeSpan(TimeSpan value, byte[] buffer, int index = 0)
        {
            if (buffer.Length + index < 21) throw new ArgumentException("String8.FromTimeSpan requires a 21 byte buffer for conversion.");

            int next = index;
            String8 part;

            // Days.
            if (value.Days != 0)
            {
                part = FromInteger(value.Days, buffer, next);
                next += part.Length;

                buffer[next++] = UTF8.Period;
            }

            // Hours:Minutes:Seconds
            FromInteger(value.Hours, buffer, next, 2);
            next += 2;

            buffer[next++] = UTF8.Colon;
            FromInteger(value.Minutes, buffer, next, 2);
            next += 2;

            buffer[next++] = UTF8.Colon;
            FromInteger(value.Seconds, buffer, next, 2);
            next += 2;

            // .Milliseconds
            if (value.Milliseconds != 0)
            {
                buffer[next++] = UTF8.Period;
                part = FromInteger(value.Milliseconds, buffer, next);
                next += part.Length;
            }

            return new String8(buffer, index, next - index);
        }
        
        /// <summary>
        ///  Convert a String8 to a TimeSpan.
        /// </summary>
        /// <remarks>
        ///   Ex: 7.12:30:59.999
        /// </remarks>
        /// <param name="result">TimeSpan converted, or TimeSpan.Zero if invalid</param>
        /// <returns>True if valid TimeSpan, False otherwise</returns>
        public bool TryToTimeSpan(out TimeSpan result)
        {
            const string TimeSpanMinValue = "-10675199.02:48:05.4775808";

            result = TimeSpan.Zero;
            if (this.IsEmpty()) return false;

            // If the TimeSpan is negative, parse the rest and negate
            bool isNegative = this.StartsWith(UTF8.Dash);
            if(isNegative)
            {
                // Handle TimeSpan.MinValue separately since it will overflow as a positive value
                if(this.Length == TimeSpanMinValue.Length && this.Equals(TimeSpanMinValue))
                {
                    result = TimeSpan.MinValue;
                    return true;
                }

                bool succeeded = this.Substring(1).TryToTimeSpan(out result);
                result = -result;
                return succeeded;
            }

            uint days = 0, hours = 0, minutes = 0, seconds = 0, ticks = 0;

            // Find the first colon (hour:minute)
            int hourIndex = this.IndexOf(UTF8.Colon) - 2;

            // If this is a days-only TimeSpan, try to parse it
            if (hourIndex < 0)
            {
                if (!this.TryToUInt(out days)) return false;
            }
            else
            {
                // Use length to infer components which must be present
                bool hasDays = (hourIndex > 0);
                bool hasSeconds = (Length - hourIndex > 5);                 // HH:MMx
                bool hasPartialSeconds = (Length - hourIndex > 8);          // HH:MM:SSx

                // Validate separators
                if (hasDays && this[hourIndex - 1] != UTF8.Period) return false;
                if (hasSeconds && this[hourIndex + 5] != UTF8.Colon) return false;
                if (hasPartialSeconds && this[hourIndex + 8] != UTF8.Period) return false;

                // Parse Days, Hours, Minutes, Seconds
                if (hasDays && !this.Substring(0, hourIndex - 1).TryToUInt(out days)) return false;
                if (!this.Substring(hourIndex, 2).TryToUInt(out hours)) return false;
                if (!this.Substring(hourIndex + 3, 2).TryToUInt(out minutes)) return false;
                if (hasSeconds && !this.Substring(hourIndex + 6, 2).TryToUInt(out seconds)) return false;

                // Parse Partial Seconds and normalize to ticks (0.1s -> 100k ticks, ticks are millionths of a second)
                if (hasPartialSeconds)
                {
                    String8 partialSeconds = this.Substring(hourIndex + 9);
                    if (partialSeconds.Length > 7) return false;
                    if (!partialSeconds.TryToUInt(out ticks)) return false;
                    if (partialSeconds.Length < 7) ticks *= (uint)Math.Pow(10, 7 - partialSeconds.Length);
                }
            }

            // Validate number ranges
            if (days > 10675199) return false;
            if (hours > 23) return false;
            if (minutes > 59) return false;
            if (seconds > 59) return false;

            // Construct the TimeSpan
            result = new TimeSpan((int)days, (int)hours, (int)minutes, (int)seconds).Add(new TimeSpan(ticks));
            return true;
        }

        /// <summary>
        ///  Parse a "friendly" TimeSpan value, like 7d, 24h, 5m, 30s.
        /// </summary>
        /// <param name="result">TimeSpan equivalent of value</param>
        /// <returns>True if valid TimeSpan, False otherwise</returns>
        public bool TryToTimeSpanFriendly(out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (this.IsEmpty()) return false;

            // Try to Parse as a "normal" format TimeSpan
            if (TryToTimeSpan(out result)) return true;

            // Find the portion of the value which is the number part
            bool allDigits = true;
            int numberPrefixLength = (this.StartsWith(UTF8.Dash) ? 1 : 0);
            for (; numberPrefixLength < this.Length; ++numberPrefixLength)
            {
                ParseDigit(this[numberPrefixLength], ref allDigits);
                if (!allDigits) break;
            }

            // Verify there is a suffix
            if (numberPrefixLength == Length) return false;

            // Parse the number part
            int numberPartValue;
            if (!this.Substring(0, numberPrefixLength).TryToInteger(out numberPartValue)) return false;
            String8 suffix = this.Substring(numberPrefixLength);
            suffix.ToLowerInvariant();

            if(suffix.Equals("ms"))
            {
                result = TimeSpan.FromMilliseconds(numberPartValue);
                return true;
            }
            else if (suffix.Equals("s"))
            {
                result = TimeSpan.FromSeconds(numberPartValue);
                return true;
            }
            else if (suffix.Equals("m"))
            {
                result = TimeSpan.FromMinutes(numberPartValue);
                return true;
            }
            else if (suffix.Equals("h"))
            {
                result = TimeSpan.FromHours(numberPartValue);
                return true;
            }
            else if (suffix.Equals("d"))
            {
                result = TimeSpan.FromDays(numberPartValue);
                return true;
            }

            return false;
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
            int thisLength = Length;
            int otherLength = other.Length;
            int commonLength = Math.Min(thisLength, otherLength);

            for (int i = 0; i < commonLength; ++i)
            {
                byte tC = Array[Index + i];

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
            if (other.Index == Index && other.Array == Array && other.Length == Length) return 0;

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
            return Length.CompareTo(other.Length);
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
            if (other.Index == Index && other.Array == Array && other.Length == Length) return 0;

            // Next, compare up to the length both strings are
            int cmp = (ignoreCase ? CompareToCommonLengthOrdinalIgnoreCase(other) : CompareToCommonLength(other));
            if (cmp != 0) return cmp;

            // If all bytes are equal and this is not longer than 'other', this is a prefix
            if (Length <= other.Length) return 0;

            // Otherwise (other is shorter), this sorts after other
            return 1;
        }

        private int CompareToCommonLength(String8 other)
        {
            // Otherwise, compare the common length
            int thisLength = Length;
            int otherLength = other.Length;
            int commonLength = Math.Min(thisLength, otherLength);

            int i = 0;

            // Compare four-at-a-time while strings are long enough
            for (; i < commonLength - 3; i += 4)
            {
                int c1 = Array[Index + i].CompareTo(other.Array[other.Index + i]);
                if (c1 != 0) return c1;

                int c2 = Array[Index + i + 1].CompareTo(other.Array[other.Index + i + 1]);
                if (c2 != 0) return c2;

                int c3 = Array[Index + i + 2].CompareTo(other.Array[other.Index + i + 2]);
                if (c3 != 0) return c3;

                int c4 = Array[Index + i + 3].CompareTo(other.Array[other.Index + i + 3]);
                if (c4 != 0) return c4;
            }

            // Compare the end one-at-a-time
            for (; i < commonLength; ++i)
            {
                int cmp = Array[Index + i].CompareTo(other.Array[other.Index + i]);
                if (cmp != 0) return cmp;
            }

            return 0;
        }

        private int CompareToCommonLengthOrdinalIgnoreCase(String8 other)
        {
            // ISSUE: This is the same as .NET for ASCII strings. Not sure what OrdinalIgnoreCase
            // does for non-ASCII. This will just be an ordinal comparison of those.

            // Otherwise, compare the common length
            int thisLength = Length;
            int otherLength = other.Length;
            int commonLength = Math.Min(thisLength, otherLength);

            int i = 0;

            // Compare four-at-a-time while strings are long enough
            for (; i < commonLength - 3; i += 4)
            {
                int c1 = CompareOrdinalIgnoreCase(Array[Index + i], other.Array[other.Index + i]);
                if (c1 != 0) return c1;

                int c2 = CompareOrdinalIgnoreCase(Array[Index + i + 1], other.Array[other.Index + i + 1]);
                if (c2 != 0) return c2;

                int c3 = CompareOrdinalIgnoreCase(Array[Index + i + 2], other.Array[other.Index + i + 2]);
                if (c3 != 0) return c3;

                int c4 = CompareOrdinalIgnoreCase(Array[Index + i + 3], other.Array[other.Index + i + 3]);
                if (c4 != 0) return c4;
            }

            // Compare the end one-at-a-time
            for (; i < commonLength; ++i)
            {
                int cmp = CompareOrdinalIgnoreCase(Array[Index + i], other.Array[other.Index + i]);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAlphaNumeric(byte value)
        {
            return ((byte)(value - UTF8.a) < UTF8.AlphabetLength)
                || ((byte)(value - UTF8.A) < UTF8.AlphabetLength)
                || ((byte)(value - UTF8.Zero) < 10);
        }

        /// <summary>
        ///  Find multiple matches of 'value' within this text, starting at start index.
        ///  Stop at the end of the string or when the results array is full.
        ///  
        ///  If countFound is less than results.Length, use results[countFound - 1] + 1 as the next startIndex.
        /// </summary>
        /// <param name="value">Value to find</param>
        /// <param name="startIndex">Index to start searching from</param>
        /// <param name="ignoreCase">True to compare case-insensitively, False otherwise</param>
        /// <param name="results">Array to write results (index of each match) to</param>
        /// <returns>Number of matches found.</returns>
        public int IndexOfAll(String8 value, int startIndex, bool ignoreCase, int[] results)
        {
            int countFound = 0;

            // Find a batch of matches
            if (ignoreCase)
            {
                while (true)
                {
                    int foundIndex = this.IndexOfOrdinalIgnoreCase(value, startIndex);
                    if (foundIndex == -1) break;

                    results[countFound++] = foundIndex;
                    startIndex = foundIndex + 1;
                    if (countFound == results.Length) break;
                }
            }
            else
            {
                while (true)
                {
                    int foundIndex = this.IndexOf(value, startIndex);
                    if (foundIndex == -1) break;

                    results[countFound++] = foundIndex;
                    startIndex = foundIndex + 1;
                    if (countFound == results.Length) break;
                }
            }

            return countFound;
        }

        /// <summary>
        ///  Return the first index at which the passed string appears in this string.
        /// </summary>
        /// <param name="value">Value to find</param>
        /// <param name="startIndex">First index at which to check</param>
        /// <returns>Index of first occurrence of value or -1 if not found</returns>
        public int IndexOf(String8 value, int startIndex = 0)
        {
            if (value.IsEmpty()) return -1;

            int length = value.Length;

            int end = Index + Length - value.Length + 1;
            for (int start = Index + startIndex; start < end; ++start)
            {
                int i = 0;
                for (; i < length; ++i)
                {
                    if (Array[start + i] != value.Array[value.Index + i]) break;
                }

                if (i == length) return start - Index;
            }

            return -1;
        }

        /// <summary>
        ///  Return the index at which this string contains 'other' using case-insensitive comparison,
        ///  or -1 if it was not found.
        /// </summary>
        /// <param name="other">Value to find within this String</param>
        /// <param name="startIndex">Index from which to search</param>
        /// <returns>Index of first instance of value in this String or -1 if not found</returns>
        public int IndexOfOrdinalIgnoreCase(String8 other, int startIndex = 0)
        {
            int otherLength = other.Length;
            int end = this.Length - otherLength + 1;

            for (int matchStart = startIndex; matchStart < end; ++matchStart)
            {
                int i = 0;
                for (; i < otherLength; ++i)
                {
                    int cmp = CompareOrdinalIgnoreCase(Array[Index + matchStart + i], other.Array[other.Index + i]);
                    if (cmp != 0) break;
                }

                // Match found
                if (i == otherLength) return matchStart;
            }

            // No matches found
            return -1;
        }

        /// <summary>
        ///  Return the index at which this string contains 'other' using case-insensitive comparison,
        ///  with non-alphanumeric character beforehand, or -1 if not found.
        /// </summary>
        /// <param name="other">Value to find within this String</param>
        /// <param name="startIndex">Index from which to search</param>
        /// <returns>Index of first instance of value in this String or -1 if not found</returns>
        public int Contains(String8 other, int startIndex = 0)
        {
            while (true)
            {
                // Find the next occurrence of 'other'
                int foundAtIndex = IndexOfOrdinalIgnoreCase(other, startIndex);

                // If not found, there are no matches
                if (foundAtIndex == -1) return -1;

                // If there's a boundary right before and after, this is a match
                int before = foundAtIndex - 1;
                int after = foundAtIndex + other.Length;
                if ((before < 0 || !IsAlphaNumeric(Array[Index + before]))) return foundAtIndex;

                // Otherwise, keep looking
                startIndex = foundAtIndex + 1;
            }
        }

        /// <summary>
        ///  Return the index at which this string contains 'other' using case-insensitive comparison,
        ///  with non-alphanumeric surrounding characters, or -1 if not found.
        /// </summary>
        /// <param name="other">Value to find within this String</param>
        /// <param name="startIndex">Index from which to search</param>
        /// <returns>Index of first instance of value in this String or -1 if not found</returns>
        public int ContainsExact(String8 other, int startIndex = 0)
        {
            while (true)
            {
                // Find the next occurrence of 'other'
                int foundAtIndex = IndexOfOrdinalIgnoreCase(other, startIndex);

                // If not found, there are no matches
                if (foundAtIndex == -1) return -1;

                // If there's a boundary right before and after, this is a match
                int before = foundAtIndex - 1;
                int after = foundAtIndex + other.Length;
                if ((before < 0 || !IsAlphaNumeric(Array[Index + before])) && (after >= Length || !IsAlphaNumeric(Array[Index + after]))) return foundAtIndex;

                // Otherwise, keep looking
                startIndex = foundAtIndex + 1;
            }
        }
        #endregion

        #region Output
        /// <summary>
        ///  Move this String8 backwards in the byte array by the specified number of bytes.
        ///  Used to un-escape values escaped with extra characters.
        /// </summary>
        /// <param name="byteCount">Number of byte to shift this string</param>
        /// <returns>A String8 reference to this String8 value in the new shifted position</returns>
        public String8 ShiftBack(int byteCount)
        {
            if (byteCount <= 0 || this.Index < byteCount) throw new ArgumentOutOfRangeException("byteCount");
            System.Buffer.BlockCopy(Array, Index, Array, Index - byteCount, Length);
            return new String8(Array, Index - byteCount, Length);
        }

        /// <summary>
        ///  Write this value to the target byte[] at the given index as UTF8.
        /// </summary>
        /// <param name="buffer">byte[] to write to</param>
        /// <param name="index">Index at which to write</param>
        /// <returns>Byte count written</returns>
        public int WriteTo(byte[] buffer, int index)
        {
            if (Length <= 0) return 0;
            if (index + Length > buffer.Length) throw new ArgumentException(String.Format(Resources.BufferTooSmall, index + Length, buffer.Length));
            System.Buffer.BlockCopy(Array, Index, buffer, index, Length);
            return Length;
        }

        /// <summary>
        ///  Write this value to the target TextWriter.
        /// </summary>
        /// <param name="writer">TextWriter to write to</param>
        /// <returns>Character count written</returns>
        public int WriteTo(TextWriter writer)
        {
            if (Length <= 0) return 0;

            int length = Length;
            int end = Index + length;

            for (int index = Index; index < end; ++index)
            {
                byte current = Array[index];
                if (current < 0x80)
                {
                    // Single Byte characters - write one-by-one
                    writer.Write((char)Array[index]);
                }
                else
                {
                    // Multi-byte - make the UTF8 decoder convert the rest
                    string stringSuffix = Encoding.UTF8.GetString(Array, index, end - index);
                    writer.Write(stringSuffix);

                    length = (index - Index) + stringSuffix.Length;
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
            if (Length <= 0) return 0;

            stream.Write(Array, Index, Length);
            return Length;
        }
        #endregion

        #region Object Overrides
        public static bool operator ==(String8 left, String8 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(String8 left, String8 right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object o)
        {
            if (o is String8)
            {
                return this.Equals((String8)o);
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

        public bool Equals(String8 other)
        {
            if (this.Length != other.Length) return false;
            if (this.Array == other.Array && this.Index == other.Index) return true;

            int offset = other.Index - this.Index;
            int end = this.Index + this.Length;
            for (int i = this.Index; i < end; ++i)
            {
                if (this.Array[i] != other.Array[i + offset]) return false;
            }

            return true;
        }

        public bool Equals(string other)
        {
            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            // Jenkins One-at-a-Time Hash. https://en.wikipedia.org/wiki/Jenkins_hash_function
            uint hash = 0;

            for (int i = 0; i < Length; i++)
            {
                hash += Array[Index + i];
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
            return Encoding.UTF8.GetString(Array, Index, Length);
        }
        #endregion

        #region IBinarySerializable
        public void WriteBinary(BinaryWriter w)
        {
            w.Write(Length);
            if (Length > 0) w.Write(Array, Index, Length);
        }

        public void ReadBinary(BinaryReader r)
        {
            Length = r.ReadArrayLength(1);
            if (Length > 0) Array = r.ReadBytes(Length);
            Index = 0;
        }
        #endregion
    }
}
