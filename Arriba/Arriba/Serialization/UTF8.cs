// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace Arriba.Serialization
{
    /// <summary>
    ///  UTF8 contains constants and helper methods for handling UTF8 bytes and
    ///  byte arrays.
    /// </summary>
    public static class UTF8
    {
        public const byte Space = (byte)' ';
        public const byte LF = (byte)10;
        public const byte CR = (byte)13;
        public const byte DoubleQuote = (byte)34;
        public const byte Pound = (byte)35;
        public const byte Amperstand = (byte)38;
        public const byte Apostrophe = (byte)39;
        public const byte Comma = (byte)44;
        public const byte Period = (byte)46;
        public const byte Slash = (byte)47;
        public const byte Zero = (byte)48;
        public const byte Nine = (byte)57;
        public const byte Semicolon = (byte)59;
        public const byte LessThan = (byte)60;
        public const byte GreaterThan = (byte)62;
        public const byte A = (byte)65;
        public const byte Z = (byte)90;
        public const byte Backslash = (byte)92;
        public const byte a = (byte)97;
        public const byte z = (byte)122;

        public const byte IsMultiByteMask = 0x80;
        public const byte IsLowercaseMask = 0x20;

        /// <summary>
        ///  Convert a given UTF8 byte array to lowercase, in place. Only
        ///  single byte values are converted to lowercase.
        /// </summary>
        /// <param name="s">byte[] of UTF8 bytes to make lowercase</param>
        /// <param name="index">Index from which to transform array</param>
        /// <param name="length">Length of bytes to transform in array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ToLowerInvariant(byte[] s, int index, int length)
        {
            int end = index + length;

            if (s == null) throw new ArgumentNullException("s");
            if (length == 0) return;
            if (index < 0 || index >= s.Length) throw new ArgumentOutOfRangeException("index");
            if (length < 0 || end > s.Length) throw new ArgumentOutOfRangeException("length");

            fixed (byte* bArray = s)
            {
                byte* bEnd = (bArray + end);

                for (byte* bCurrent = (bArray + index); bCurrent != bEnd; ++bCurrent)
                {
                    byte c = *bCurrent;

                    if (c >= A && c <= Z)
                    {
                        *bCurrent = (byte)(c | IsLowercaseMask);
                    }
                }
            }
        }

        /// <summary>
        ///  Return the lowercase version of the given single
        ///  byte character.
        /// </summary>
        /// <param name="c">UTF8 byte to convert to lowercase</param>
        /// <returns>UTF8 byte converted to lowercase</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToLowerInvariant(byte c)
        {
            if (c >= A && c <= Z) c |= IsLowercaseMask;
            return c;
        }

        /// <summary>
        ///  Returns whether the given byte is a single byte
        ///  character.
        /// </summary>
        /// <param name="c">UTF8 byte to examine</param>
        /// <returns>True if byte is a single character, 
        /// False if part of a multi-byte character</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSingleByte(byte c)
        {
            return (c & IsMultiByteMask) == 0;
        }
    }
}
