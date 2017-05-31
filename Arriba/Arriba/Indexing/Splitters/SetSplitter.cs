// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Indexing
{
    /// <summary>
    ///  SetSplitter splits text delimited by "; " into individual items.
    /// </summary>
    public class SetSplitter : IWordSplitter
    {
        public void Split(byte[] text, Range withinRange, RangeSet result)
        {
            if (result == null) throw new ArgumentNullException("result");
            if (text == null) return;

            int lastGroup = withinRange.Index;
            int end = withinRange.Index + withinRange.Length;
            int i;
            for (i = withinRange.Index; i < end; ++i)
            {
                byte c = text[i];

                if (c == UTF8.Semicolon)
                {
                    result.Add(lastGroup, i - lastGroup);
                    lastGroup = i + 1;
                    if (lastGroup < end && text[lastGroup] == UTF8.Space) lastGroup++;
                }
            }

            // Include the last value
            result.Add(lastGroup, i - lastGroup);
        }
    }
}
