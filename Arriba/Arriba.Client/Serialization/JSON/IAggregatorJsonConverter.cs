// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Aggregations;
using Arriba.Model.Expressions;
using Arriba.Model.Query;

using Newtonsoft.Json;

namespace Arriba.Client.Serialization.Json
{
    public class IAggregatorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IAggregator).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return AggregationQuery.BuildAggregator((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Write aggregators as the ToString form
            IAggregator aggregator = (IAggregator)value;
            writer.WriteValue(aggregator.ToString());
        }
    }
}
