// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Arriba.Structures;

namespace Arriba.Serialization
{
    /// <summary>
    ///  RssWriter provides simple writing of ByteBlock-based content to an RSS feed.
    ///  
    ///  Usage:
    ///     RssWriter w = new RssWriter(serializationContext);
    ///     w.WriteRssHeader(title, description, url, publishedDate, timetoLive);
    ///     
    ///     foreach(var item in items)
    ///     {
    ///         w.WriteItem(item.ID, item.Title, item.Description, urlBeforeId, publishedDate);
    ///     }
    ///     
    ///     w.WriteRssFooter();
    /// </summary>
    public class RssWriter
    {
        private static ByteBlock s_newline = "\r\n";
        private static ByteBlock s_header = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\r\n<rss version=\"2.0\"><channel>";
        private static ByteBlock s_footer = "</channel></rss>";
        private static ByteBlock s_lastBuildDateTag = "lastBuildDate";
        private static ByteBlock s_ttlTag = "ttl";
        private static ByteBlock s_itemTag = "item";
        private static ByteBlock s_titleTag = "title";
        private static ByteBlock s_descriptionTag = "description";
        private static ByteBlock s_linkTag = "link";
        private static ByteBlock s_guidTag = "guid";
        private static ByteBlock s_isPermalinkTrueAttribute = @"isPermaLink=""true""";
        private static ByteBlock s_pubDateTag = "pubDate";

        private Stream _stream;

        public RssWriter(ISerializationContext context)
        {
            _stream = context.Stream;
        }

        public void WriteRssHeader(ByteBlock title, ByteBlock description, ByteBlock url, DateTime publishedDate, TimeSpan timeToLive)
        {
            s_header.WriteTo(_stream);
            WriteTag(s_titleTag, title);
            WriteTag(s_descriptionTag, description);
            WriteTag(s_linkTag, url);
            WriteTag(s_lastBuildDateTag, publishedDate.ToString("r"));
            WriteTag(s_pubDateTag, publishedDate.ToString("r"));
            WriteTag(s_ttlTag, timeToLive.TotalMinutes.ToString());
            s_newline.WriteTo(_stream);
        }

        public void WriteRssFooter()
        {
            s_footer.WriteTo(_stream);
        }

        public void WriteItem(ByteBlock id, ByteBlock title, ByteBlock description, ByteBlock urlBeforeId, DateTime publishedDate)
        {
            WriteItem(id, title, (w) => w.WriteAsHtmlText(description), urlBeforeId, publishedDate);
        }

        public void WriteItem(ByteBlock id, ByteBlock title, Action<RssWriter> writeDescription, ByteBlock urlBeforeId, DateTime publishedDate)
        {
            WriteOpenTag(s_itemTag);

            // <title>[title]</title>
            WriteTag(s_titleTag, title);

            // Call method to write description arbitrarily
            WriteOpenTag(s_descriptionTag);
            writeDescription(this);
            WriteCloseTag(s_descriptionTag);

            // <link>[urlBeforeId][Id]</link>
            WriteOpenTag(s_linkTag);
            WriteAsHtmlText(urlBeforeId);
            WriteAsHtmlText(id);
            WriteCloseTag(s_linkTag);

            // <guid isPermaLink="true">GUID</guid>
            Guid itemGuid = id.GetHashAsGuid();
            ByteBlock guidString = itemGuid.ToString("D");
            WriteOpenTag(s_guidTag);
            s_isPermalinkTrueAttribute.WriteTo(_stream);
            guidString.WriteTo(_stream);
            WriteCloseTag(s_guidTag);

            // <pubDate>[publishDate]</pubDate]
            WriteTag(s_pubDateTag, publishedDate.ToString("r"));

            WriteCloseTag(s_itemTag);
            s_newline.WriteTo(_stream);
        }

        public void WriteTag(ByteBlock tagName, ByteBlock innerText)
        {
            WriteOpenTag(tagName);
            WriteAsHtmlText(innerText);
            WriteCloseTag(tagName);
        }

        public void WriteOpenTag(ByteBlock tagName)
        {
            // <tagName>
            _stream.WriteByte(UTF8.LessThan);
            tagName.WriteTo(_stream);
            _stream.WriteByte(UTF8.GreaterThan);
        }

        public void WriteCloseTag(ByteBlock tagName)
        {
            // </tagName>
            _stream.WriteByte(UTF8.LessThan);
            _stream.WriteByte(UTF8.Slash);
            tagName.WriteTo(_stream);
            _stream.WriteByte(UTF8.GreaterThan);
        }

        /// <summary>
        ///  Write a ByteBlock [UTF8 text] to the given stream, escaped to be
        ///  safe within HTML as inner text or attribute values.
        /// </summary>
        /// <param name="value">ByteBlock to write</param>
        /// <param name="stream">Stream to write to</param>
        public void WriteAsHtmlText(ByteBlock value)
        {
            int nextIndexToCopy = value.Index;
            int end = value.Index + value.Length;
            int length;

            for (int i = nextIndexToCopy; i < end; ++i)
            {
                byte c = value.Array[i];

                // Escape: Amperstand, LessThan, GreaterThan, Quote, Apostrophe
                if (c == UTF8.Amperstand || c == UTF8.LessThan || c == UTF8.GreaterThan || c == UTF8.DoubleQuote || c == UTF8.Apostrophe)
                {
                    // Copy characters before the one requiring escaping
                    length = i - nextIndexToCopy;
                    if (length > 0) _stream.Write(value.Array, nextIndexToCopy, length);

                    // Write character as "&#xx;" with the decimal keypoint
                    _stream.WriteByte(UTF8.Amperstand);
                    _stream.WriteByte(UTF8.Pound);
                    _stream.WriteByte((byte)(UTF8.Zero + (c / 10)));
                    _stream.WriteByte((byte)(UTF8.Zero + (c % 10)));
                    _stream.WriteByte(UTF8.Semicolon);

                    // Set the next character to copy (the one after this one)
                    nextIndexToCopy = i + 1;
                }
            }

            // Copy the remaining characters
            length = end - nextIndexToCopy;
            if (length > 0) _stream.Write(value.Array, nextIndexToCopy, length);
        }
    }
}
