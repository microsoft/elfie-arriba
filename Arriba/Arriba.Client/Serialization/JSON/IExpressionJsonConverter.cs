// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Expressions;
using Arriba.Model.Query;

using Newtonsoft.Json;

namespace Arriba.Client.Serialization.Json
{
    public class IExpressionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IExpression).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return SelectQuery.ParseWhere((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Write expressions as the ToString form
            IExpression expression = (IExpression)value;
            writer.WriteValue(expression.ToString());
        }
    }
}
