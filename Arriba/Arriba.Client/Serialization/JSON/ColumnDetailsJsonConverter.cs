// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Column;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arriba.Client.Serialization.Json
{
    /// <summary>
    /// JSON Serializer for Arriba ColumnDetails. 
    /// </summary>
    public class ColumnDetailsJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ColumnDetails).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Default reader is sufficient
            JObject jObject = JObject.Load(reader);
            return jObject.ToObject<ColumnDetails>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ColumnDetails column = (ColumnDetails)value;

            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteValue(column.Name);

            if (!String.IsNullOrEmpty(column.Type))
            {
                writer.WritePropertyName("type");
                writer.WriteValue(column.Type);
            }

            if (column.Default != null)
            {
                writer.WritePropertyName("default");
                writer.WriteValue(column.Default);
            }

            if (!String.IsNullOrEmpty(column.Alias))
            {
                writer.WritePropertyName("alias");
                writer.WriteValue(column.Alias);
            }

            if (column.IsPrimaryKey)
            {
                writer.WritePropertyName("isPrimaryKey");
                writer.WriteValue(column.IsPrimaryKey);
            }

            writer.WriteEndObject();
        }
    }
}
