// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication.ContentTypes
{
    /// <summary>
    /// JsonP content writer. 
    /// </summary>
    /// <remarks>
    /// JsonP is used for cross domain json loading, by encoding json output as a function call targetted at a callback url parameter. 
    /// </remarks>
    public sealed class JsonpContentWriter : IContentWriter
    {
        private JsonContentWriter _jsonWriter;
        private const string CallbackNameKey = "callback";

        public JsonpContentWriter(JsonContentWriter jsonWriter)
        {
            _jsonWriter = jsonWriter;
        }

        public string ContentType
        {
            get
            {
                return "application/javascript";
            }
        }

        public bool CanWrite(Type t)
        {
            return true;
        }

        public async Task WriteAsync(IRequest request, Stream output, object content)
        {
            var callbackName = request.ResourceParameters[CallbackNameKey];

            if (String.IsNullOrEmpty(callbackName))
            {
                throw new ArgumentException("No callback name specified on request");
            }

            using (StreamWriter writer = new StreamWriter(output))
            {
                await writer.WriteAsync(callbackName + "(");
                await _jsonWriter.WriteAsyncCore(writer, content);
                await writer.WriteAsync(")");
            }
        }
    }
}
