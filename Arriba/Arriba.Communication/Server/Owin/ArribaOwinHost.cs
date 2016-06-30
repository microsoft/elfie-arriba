// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arriba.Server.Owin
{
    using Arriba.Communication;
    using Arriba.Server.Hosting;
    using System.IO;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ArribaOwinHost
    {
        private Host _arribaHost;
        private AppFunc _next;
        private ComposedApplicationServer _server;
        private IRequestHandler _handler;

        public ArribaOwinHost(AppFunc next, Host arribaHost)
        {
            _next = next;
            _arribaHost = arribaHost;
            _server = arribaHost.GetService<ComposedApplicationServer>();
            _handler = _server as IRequestHandler;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IRequest request = new ArribaOwinRequest(environment, _server.ReaderWriter);

            var resp = await _handler.HandleAsync(request, passThrough: true);

            if (resp == null || resp.Status == ResponseStatus.NotHandled)
            {
                await _next(environment);
                return;
            }

            // Write response. 
            await this.WriteResponse(request, resp, environment, _server.ReaderWriter);
        }

        private async Task WriteResponse(IRequest request, IResponse response, IDictionary<string, object> environment, IContentReaderWriterService readerWriter)
        {
            var responseHeaders = environment.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            var responseBody = environment.Get<Stream>("owin.ResponseBody");

            // Status Code
            environment["owin.ResponseStatusCode"] = ResponseStatusToHttpStatusCode(response);

            foreach (var header in response.Headers.ValuePairs)
            {
                string[] current;

                if (responseHeaders.TryGetValue(header.Item1, out current))
                {
                    Array.Resize(ref current, current.Length + 1);
                    current[current.Length - 1] = header.Item2;
                }
                else
                {
                    responseHeaders[header.Item1] = new[] { header.Item2 };
                }
            }

            // For stream responses we just write the content directly back to the context 
            IStreamWriterResponse streamedResponse = response as IStreamWriterResponse;

            if (streamedResponse != null)
            {
                responseHeaders["Content-Type"] = new[] { streamedResponse.ContentType };
                await streamedResponse.WriteToStreamAsync(responseBody);
            }
            else if (response.ResponseBody != null)
            {
                // Default to application/json output
                const string DefaultContentType = "application/json";

                string accept;
                if (!request.Headers.TryGetValue("Accept", out accept))
                {
                    accept = DefaultContentType;
                }

                // Split and clean the accept header and prefer output content types requested by the client,
                // always falls back to json if no match is found. 
                IEnumerable<string> contentTypes = accept.Split(';').Where(a => a != "*/*");
                var writer = readerWriter.GetWriter(contentTypes, DefaultContentType, response.ResponseBody);

                // NOTE: One must set the content type *before* writing to the output stream. 
                responseHeaders["Content-Type"] = new[] { writer.ContentType };

                Exception writeException = null;

                try
                {
                    await writer.WriteAsync(request, responseBody, response.ResponseBody);
                }
                catch (Exception e)
                {
                    writeException = e;
                }

                if (writeException != null)
                {
                    // Output formatter failed, set the response to a 500 and write a plain string message. 
                    environment["owin.ResponseStatusCode"] = 500;

                    if (responseBody.CanWrite)
                    {
                        using (var failureWriter = new StreamWriter(responseBody))
                        {
                            var message = String.Format("ERROR: Content writer {0} for content type {1} failed with exception {2}", writer.GetType(), writer.ContentType, writeException.GetType().Name);
                            await failureWriter.WriteAsync(message);
                        }
                    }
                }
            }

            response.Dispose();
        }

        /// <summary>
        /// Map generic responses to http status code responses.
        /// </summary>
        /// <param name="response">Response.</param>
        /// <returns>Http status code.</returns>
        private static int ResponseStatusToHttpStatusCode(IResponse response)
        {
            switch (response.Status)
            {
                case ResponseStatus.BadRequest:
                    return 400;
                case ResponseStatus.NotHandled:
                case ResponseStatus.Error:
                    return 500;
                case ResponseStatus.NotFound:
                    return 404;
                case ResponseStatus.Forbidden:
                    return 403;
                case ResponseStatus.Created:
                    return 201;
                case ResponseStatus.Ok:
                default:
                    return 200;
            }
        }
    }
}
