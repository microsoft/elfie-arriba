// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Extensions;
using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Indexing
{
    /// <summary>
    ///  DefaultWordSplitter splits text into alphanumeric words and retains
    ///  dotted word groups. It's designed to provide fast, good results for
    ///  general text. Columns may provide alternative splitters which work
    ///  better for specific types of content.
    /// </summary>
    public class DefaultWordSplitter : IWordSplitter
    {
        public void Split(byte[] text, Range withinRange, RangeSet result)
        {
            if (result == null) throw new ArgumentNullException("result");

            if (text == null) return;

            bool inGroup = false;
            int dottedStart = -1;
            int alphaStart = -1;

            int end = withinRange.Index + withinRange.Length;
            int i;
            for (i = withinRange.Index; i < end; ++i)
            {
                byte c = text[i];

                bool isDigit = (c >= UTF8.Zero && c <= UTF8.Nine);
                bool isUpper = (c >= UTF8.A && c <= UTF8.Z);
                bool isLower = (c >= UTF8.a && c <= UTF8.z);
                bool isAlphaNumeric = (isDigit || isUpper || isLower);

                if (!inGroup)
                {
                    // If we're not in a dotted group and we see an alpha character, start all machines.
                    if (isAlphaNumeric)
                    {
                        inGroup = true;
                        dottedStart = i;
                        alphaStart = i;
                    }
                }
                else
                {
                    if (!isAlphaNumeric)
                    {
                        // Non-alpha - capture in progress alpha word.
                        result.Add(alphaStart, i - alphaStart);

                        if (c != UTF8.Period)
                        {
                            // We've left the group. Capture dotted group also if unique.
                            if (alphaStart != dottedStart) result.Add(dottedStart, i - dottedStart);
                            inGroup = false;
                        }
                        else
                        {
                            // Just a new dotted string part. Pre-start the next alpha word at the next character
                            alphaStart = i + 1;
                        }
                    }
                }
            }

            // Include in progress alpha and dotted words
            if (inGroup)
            {
                result.Add(alphaStart, i - alphaStart);
                if (alphaStart != dottedStart) result.Add(dottedStart, i - dottedStart);
            }
        }
    }
}
