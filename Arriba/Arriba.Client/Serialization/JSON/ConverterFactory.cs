// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Newtonsoft.Json;

namespace Arriba.Client.Serialization.Json
{
    public static class ConverterFactory
    {
        public static IList<JsonConverter> GetArribaConverters()
        {
            List<JsonConverter> converters = new List<JsonConverter>();

            converters.Add(new DataBlockJsonConverter());
            converters.Add(new ColumnDetailsJsonConverter());
            converters.Add(new ValueJsonConverter());
            converters.Add(new IExpressionJsonConverter());
            converters.Add(new IAggregatorJsonConverter());

            return converters;
        }
    }
}
