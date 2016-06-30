// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Model.Strings
{
    public static class AlphanumericSplitter
    {
        /// <summary>
        ///  Split a string into alphanumeric words without allocation by using
        ///  the same PartialArray&lt;int&gt; in a loop. When traversing over the
        ///  returned String8Set, check if each first letter IsAlphaNumeric to 
        ///  decide whether to include it.
        /// </summary>
        /// <param name="value">String8 to split</param>
        /// <param name="buffer">PartialArray to contain split positions [allows reuse without allocations]</param>
        /// <returns>String8Set containing value split at alpha-to-non-alpha boundaries</returns>
        public static String8Set Split(String8 value, ref PartialArray<int> buffer)
        {
            // Reset the buffer for our use
            buffer.Clear();

            if (!value.IsEmpty())
            {
                // Add the beginning as a part
                buffer.Add(0);

                bool inWord = IsAlphaNumeric(value[0]);

                for (int i = 1; i < value.Length; ++i)
                {
                    bool charIsAlpha = IsAlphaNumeric(value[i]);

                    if (inWord != charIsAlpha)
                    {
                        // Add a boundary at each alpha to non-alpha transition
                        buffer.Add(i);
                        inWord = charIsAlpha;
                    }
                }

                // Add the remaining part of the string
                buffer.Add(value.Length);
            }

            return new String8Set(value, 0, buffer);
        }

        /// <summary>
        ///  Return whether the given UTF8 byte is alphanumeric [ASCII alphabet only].
        /// </summary>
        /// <param name="c">Byte to check</param>
        /// <returns>True if alphanumeric, False otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlphaNumeric(byte c)
        {
            // One Subtract and Compare per group, per character
            // Since byte subtraction can wrap and byte is unsigned, if 'c' is before the first value, it'll wrap and become too large to match comparison.
            return ((byte)(c - UTF8.a) < UTF8.AlphabetLength)
                || ((byte)(c - UTF8.A) < UTF8.AlphabetLength)
                || ((byte)(c - UTF8.Zero) < UTF8.DigitsLength);
        }
    }
}
