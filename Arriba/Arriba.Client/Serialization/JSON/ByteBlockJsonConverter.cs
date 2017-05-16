// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Structures;

using Newtonsoft.Json;

namespace Arriba.Serialization.Json
{
    /// <summary>
    /// JSON Serializer for Arriba ByteBlocks
    /// </summary>
    public class ByteBlockJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ByteBlock).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return (ByteBlock)reader.ReadAsString();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((ByteBlock)value).ToString());
        }
    }
}
