// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Indexing
{
    /// <summary>
    ///  HtmlWordSplitter is designed to split Html, following the inner
    ///  splitter rules but only for plain text content (not tags or entities).
    /// </summary>
    public class HtmlWordSplitter : IWordSplitter
    {
        protected IWordSplitter InnerSplitter;

        public HtmlWordSplitter(IWordSplitter innerSplitter)
        {
            this.InnerSplitter = innerSplitter;
        }

        public void Split(byte[] text, Range withinRange, RangeSet result)
        {
            if (result == null) throw new ArgumentNullException("result");

            if (text == null) return;

            bool inTag = false;
            bool inEntity = false;
            int lastPlainTextStart = withinRange.Index;

            int end = withinRange.Index + withinRange.Length;
            int i;
            for (i = withinRange.Index; i < end; ++i)
            {
                byte c = text[i];

                if (inTag)
                {
                    // If in a tag, look only for the end of the tag. ('>')
                    if (c == UTF8.GreaterThan)
                    {
                        inTag = false;

                        // Mark the start point of literal content as the next character
                        lastPlainTextStart = i + 1;
                    }
                }
                else if (inEntity)
                {
                    // If in an entity, look only for the end of the entity (';')
                    if (c == UTF8.Semicolon)
                    {
                        inEntity = false;

                        // Mark the start point of literal content as the next character
                        lastPlainTextStart = i + 1;
                    }
                }
                else
                {
                    // If in plain text, look for a tag start ('<') or entity start ('&')
                    if (c == UTF8.LessThan)
                    {
                        inTag = true;

                        // If either found, split plain text since the last start point
                        this.InnerSplitter.Split(text, new Range(lastPlainTextStart, i - lastPlainTextStart), result);
                    }
                    else if (c == UTF8.Amperstand)
                    {
                        inEntity = true;

                        // If either found, split plain text since the last start point
                        this.InnerSplitter.Split(text, new Range(lastPlainTextStart, i - lastPlainTextStart), result);
                    }
                }
            }

            // If we ended in plain text, split text since the last start point
            if (!inTag && !inEntity && lastPlainTextStart < end)
            {
                this.InnerSplitter.Split(text, new Range(lastPlainTextStart, end - lastPlainTextStart), result);
            }
        }
    }
}
