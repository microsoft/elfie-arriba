// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

using Newtonsoft.Json;

namespace Arriba.TfsWorkItemCrawler
{
    /// <summary>
    ///  JsonConverter for Microsoft.TeamFoundation.WorkItemTracking.Client.LinkCollection.
    /// </summary>
    internal class LinkCollectionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(LinkCollection).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            LinkCollection collection = (LinkCollection)value;

            writer.WriteStartArray();

            foreach (Link l in collection)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("type");
                writer.WriteValue(l.ArtifactLinkType.Name);

                writer.WritePropertyName("comment");
                writer.WriteValue(l.Comment);

                switch (l.BaseType)
                {
                    case BaseLinkType.WorkItemLink:
                        writer.WritePropertyName("id");
                        writer.WriteValue(((WorkItemLink)l).TargetId);
                        break;

                    case BaseLinkType.RelatedLink:
                        writer.WritePropertyName("id");
                        writer.WriteValue(((RelatedLink)l).RelatedWorkItemId);
                        break;

                    case BaseLinkType.ExternalLink:
                        writer.WritePropertyName("uri");
                        writer.WriteValue(((ExternalLink)l).LinkedArtifactUri);
                        break;

                    case BaseLinkType.Hyperlink:
                        writer.WritePropertyName("uri");
                        writer.WriteValue(((Hyperlink)l).Location);
                        break;
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
