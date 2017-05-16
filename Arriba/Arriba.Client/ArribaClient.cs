// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Arriba.Client.Serialization.Json;
using Arriba.Server;
using Arriba.Types;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Arriba.Client
{
    public class ArribaClient : IDisposable
    {
        private HttpClient _httpClient;
        private JsonSerializerSettings _serializerSettings;

        public ArribaClient(string url)
            : this(new Uri(url))
        {
        }

        public ArribaClient(Uri url, TimeSpan? timeout = null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true; // Enable windows auth

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            _httpClient.BaseAddress = url;
            _httpClient.Timeout = timeout ?? _httpClient.Timeout;

            _serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false } },
                Formatting = Debugger.IsAttached ? Formatting.Indented : Formatting.None
            };

            // TODO: Use composition to import Converters
            foreach (JsonConverter converter in ConverterFactory.GetArribaConverters())
            {
                _serializerSettings.Converters.Add(converter);
            }
        }

        public ArribaTableClient this[string tableName]
        {
            get { return new ArribaTableClient(this, tableName); }
        }

        public IEnumerable<string> Tables
        {
            get
            {
                return new List<string>(this.GetAsync<IEnumerable<string>>("").Result);
            }
        }

        public async Task<ArribaTableClient> CreateTableAsync(CreateTableRequest request)
        {
            var tableClient = new ArribaTableClient(this);
            await tableClient.CreateAsync(request);
            return tableClient;
        }

        internal Task<T> GetAsync<T>(string path, dynamic parameters = null)
        {
            return SendAsync<T>(HttpMethod.Get, path, parameters);
        }

        internal Task<Stream> RequestStreamAsync(HttpMethod method, string path, dynamic parameters = null, object value = null)
        {
            HttpResponseMessage resp = this.SendObjectAsync(method, path, parameters, value).Result;
            return resp.Content.ReadAsStreamAsync();
        }

        internal async Task<T> SendAsync<T>(HttpMethod method, string path, dynamic parameters = null, HttpContent content = null)
        {
            var resp = await this.SendAsync(method, path, parameters, content);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            var env = JsonConvert.DeserializeObject<ArribaResponseEnvelope<T>>(body, _serializerSettings);
            return (T)env.Content;
        }

        internal async Task<T> SendObjectAsync<T>(HttpMethod method, string path, dynamic parameters = null, object value = null)
        {
            var resp = await this.SendObjectAsync(method, path, parameters, value);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            var env = JsonConvert.DeserializeObject<ArribaResponseEnvelope<T>>(body, _serializerSettings);
            return (T)env.Content;
        }

        internal async Task<T> SendStreamAsync<T>(HttpMethod method, string path, dynamic parameters = null, Stream value = null, string contentType = null)
        {
            var resp = await this.SendStreamAsync(method, path, parameters, value, contentType);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            var env = JsonConvert.DeserializeObject<ArribaResponseEnvelope<T>>(body, _serializerSettings);
            return (T)env.Content;
        }

        internal Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, dynamic parameters = null, HttpContent content = null)
        {
            var message = BuildRequest(method, path, parameters);

            if (content != null)
            {
                message.Content = content;
            }

            return _httpClient.SendAsync(message);
        }

        internal async Task<HttpResponseMessage> SendObjectAsync(HttpMethod method, string path, dynamic parameters = null, object value = null)
        {
            using (var content = new StringContent(JsonConvert.SerializeObject(value, _serializerSettings), Encoding.UTF8))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return await this.SendAsync(method, path, parameters, content);
            }
        }

        internal async Task<HttpResponseMessage> SendStreamAsync(HttpMethod method, string path, dynamic parameters = null, Stream value = null, string contentType = null)
        {
            using (var content = new StreamContent(value))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                return await this.SendAsync(method, path, parameters, content);
            }
        }

        private static HttpRequestMessage BuildRequest(HttpMethod method, string path, dynamic parameters)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            path = Uri.EscapeUriString(path);

            IDictionary<string, object> paramDict = DynamicToDictionary(parameters);

            if (paramDict != null && paramDict.Count > 0)
            {
                if (!path.Contains("?"))
                {
                    path += "?";
                }
                else if (!path.EndsWith("&"))
                {
                    path += "&";
                }

                path += String.Join("&", paramDict.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value.ToString())));
            }

            return new HttpRequestMessage(method, path);
        }

        private static IDictionary<string, object> DynamicToDictionary(dynamic parameters)
        {
            if (parameters == null)
            {
                return null;
            }

            IDictionary<string, object> dictionary = parameters as IDictionary<string, object>;

            if (dictionary != null)
            {
                return dictionary;
            }

            dictionary = new Dictionary<string, object>();

            object objectParameters = parameters as object;

            if (objectParameters != null)
            {
                Type t = objectParameters.GetType();

                foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.CanRead && !dictionary.ContainsKey(prop.Name))
                    {
                        dictionary.Add(prop.Name, prop.GetValue(objectParameters, null));
                    }
                }
            }

            // TODO: Other dynamic types 

            return dictionary;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _httpClient.Dispose();
        }
    }
}
