using Arriba.Structures;
using System;
using System.IO;

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
        private static ByteBlock newline = "\r\n";
        private static ByteBlock header = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\r\n<rss version=\"2.0\"><channel>";
        private static ByteBlock footer = "</channel></rss>";
        private static ByteBlock lastBuildDateTag = "lastBuildDate";
        private static ByteBlock ttlTag = "ttl";
        private static ByteBlock itemTag = "item";
        private static ByteBlock titleTag = "title";
        private static ByteBlock descriptionTag = "description";
        private static ByteBlock linkTag = "link";
        private static ByteBlock guidTag = "guid";
        private static ByteBlock isPermalinkTrueAttribute = @"isPermaLink=""true""";
        private static ByteBlock pubDateTag = "pubDate";

        private Stream stream;

        public RssWriter(ISerializationContext context)
        {
            this.stream = context.Stream;
        }

        public void WriteRssHeader(ByteBlock title, ByteBlock description, ByteBlock url, DateTime publishedDate, TimeSpan timeToLive)
        {
            header.WriteTo(stream);
            WriteTag(titleTag, title);
            WriteTag(descriptionTag, description);
            WriteTag(linkTag, url);
            WriteTag(lastBuildDateTag, publishedDate.ToString("r"));
            WriteTag(pubDateTag, publishedDate.ToString("r"));
            WriteTag(ttlTag, timeToLive.TotalMinutes.ToString());
            newline.WriteTo(stream);
        }

        public void WriteRssFooter()
        {
            footer.WriteTo(stream);
        }

        public void WriteItem(ByteBlock id, ByteBlock title, ByteBlock description, ByteBlock urlBeforeId, DateTime publishedDate)
        {
            WriteItem(id, title, (w) => w.WriteAsHtmlText(description), urlBeforeId, publishedDate);
        }

        public void WriteItem(ByteBlock id, ByteBlock title, Action<RssWriter> writeDescription, ByteBlock urlBeforeId, DateTime publishedDate)
        {
            WriteOpenTag(itemTag);

            // <title>[title]</title>
            WriteTag(titleTag, title);

            // Call method to write description arbitrarily
            WriteOpenTag(descriptionTag);
            writeDescription(this);
            WriteCloseTag(descriptionTag);

            // <link>[urlBeforeId][Id]</link>
            WriteOpenTag(linkTag);
            WriteAsHtmlText(urlBeforeId);
            WriteAsHtmlText(id);
            WriteCloseTag(linkTag);

            // <guid isPermaLink="true">GUID</guid>
            Guid itemGuid = id.GetHashAsGuid();
            ByteBlock guidString = itemGuid.ToString("D");
            WriteOpenTag(guidTag);
            isPermalinkTrueAttribute.WriteTo(stream);
            guidString.WriteTo(stream);
            WriteCloseTag(guidTag);

            // <pubDate>[publishDate]</pubDate]
            WriteTag(pubDateTag, publishedDate.ToString("r"));

            WriteCloseTag(itemTag);
            newline.WriteTo(stream);
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
            stream.WriteByte(UTF8.LessThan);
            tagName.WriteTo(stream);
            stream.WriteByte(UTF8.GreaterThan);
        }

        public void WriteCloseTag(ByteBlock tagName)
        {
            // </tagName>
            stream.WriteByte(UTF8.LessThan);
            stream.WriteByte(UTF8.Slash);
            tagName.WriteTo(stream);
            stream.WriteByte(UTF8.GreaterThan);
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
                    if (length > 0) stream.Write(value.Array, nextIndexToCopy, length);

                    // Write character as "&#xx;" with the decimal keypoint
                    stream.WriteByte(UTF8.Amperstand);
                    stream.WriteByte(UTF8.Pound);
                    stream.WriteByte((byte)(UTF8.Zero + (c / 10)));
                    stream.WriteByte((byte)(UTF8.Zero + (c % 10)));
                    stream.WriteByte(UTF8.Semicolon);

                    // Set the next character to copy (the one after this one)
                    nextIndexToCopy = i + 1;
                }
            }

            // Copy the remaining characters
            length = end - nextIndexToCopy;
            if (length > 0) stream.Write(value.Array, nextIndexToCopy, length);
        }
    }
}
