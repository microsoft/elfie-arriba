// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Arriba.Communication.ContentTypes
{
    /// <summary>
    /// Json content writer.
    /// </summary>
    public sealed class JsonContentWriter : IContentWriter
    {
        private JsonSerializerSettings _settings;

        public JsonContentWriter(IEnumerable<JsonConverter> converters)
        {
            _settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false } }
            };

#if DEBUG
            _settings.Formatting = Newtonsoft.Json.Formatting.Indented;
#endif

            foreach (var converter in converters)
            {
                _settings.Converters.Add(converter);
            }
        }

        string IContentWriter.ContentType
        {
            get
            {
                return "application/json";
            }
        }

        bool IContentWriter.CanWrite(Type t)
        {
            return true;
        }

        async Task IContentWriter.WriteAsync(IRequest request, Stream output, object content)
        {
            using (StreamWriter writer = new StreamWriter(output))
            {
                await WriteAsyncCore(writer, content);
            }
        }

        internal async Task WriteAsyncCore(StreamWriter writer, object content)
        {
            string value = JsonConvert.SerializeObject(content, _settings);
            await writer.WriteAsync(value);
        }
    }
}
