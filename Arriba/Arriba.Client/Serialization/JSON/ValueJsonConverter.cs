// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Structures;

using Newtonsoft.Json;

namespace Arriba.Client.Serialization.Json
{
    /// <summary>
    /// JSON Serializer for Arriba Values
    /// </summary>
    public class ValueJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Value).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return Value.Create(reader.ReadAsString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((Value)value).ToString());
        }
    }
}
