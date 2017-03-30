using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Newtonsoft.Json;
using System;

namespace Arriba.TfsWorkItemCrawler
{
    /// <summary>
    ///  JsonConverter for Microsoft.TeamFoundation.WorkItemTracking.Client.AttachmentCollection.
    /// </summary>
    internal class AttachmentCollectionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(AttachmentCollection).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            AttachmentCollection collection = (AttachmentCollection)value;

            writer.WriteStartArray();

            foreach(Attachment a in collection)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("name");
                writer.WriteValue(a.Name);

                writer.WritePropertyName("comment");
                writer.WriteValue(a.Comment);

                writer.WritePropertyName("uri");
                writer.WriteValue(a.Uri);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
