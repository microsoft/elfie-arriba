// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Arriba.Communication.ContentTypes;
using Arriba.Monitoring;

using Newtonsoft.Json;

namespace Arriba.Communication
{
    /// <summary>
    /// Controller that ties together channels, applications, content reading and content writing. 
    /// </summary>
    public class ApplicationServer : IRequestHandler, IDisposable
    {
        /// <summary>
        /// Reader writer service for input output content reading and writing.
        /// </summary>
        private readonly ContentReaderWriterService _readerWriter = new ContentReaderWriterService();

        /// <summary>
        /// Global cancellation source for shutting down the server. 
        /// </summary>
        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        /// <summary>
        /// I/O channels.
        /// </summary>
        private readonly List<IChannel> _channels = new List<IChannel>();

        /// <summary>
        /// Applications to route requests to. 
        /// </summary>
        private readonly List<IApplication> _applications = new List<IApplication>();

        /// <summary>
        /// Value indicating whether the application server is running.
        /// </summary>
        private bool _isRunning = false;

        /// <summary>
        /// Active channel listening tasks.
        /// </summary>
        private Task[] _channelTasks = null;

        private EventPublisherSource _eventSource = EventPublisher.CreateEventSource(new MonitorEventEntry() { Source = "Application", OpCode = MonitorEventOpCode.Mark });

        /// <summary>
        /// Default not found response.
        /// </summary>
        private static IResponse s_notFoundResponse = new Response(ResponseStatus.NotFound, "Resource not found");

        public ApplicationServer()
        { }

        /// <summary>
        /// Registers the specified channel for the application. 
        /// </summary>
        /// <param name="channel">I/O channel.</param>
        public void RegisterRequestChannel(IChannel channel)
        {
            _channels.Add(channel);
            Trace.TraceInformation("Added application channel {0}", channel.Description);
        }

        /// <summary>
        /// Registers the specified application.
        /// </summary>
        /// <param name="application">Application to register.</param>
        public void RegisterApplication(IApplication application)
        {
            _applications.Add(application);
            Trace.TraceInformation("Added application {0}", application.Name);
        }

        /// <summary>
        /// Registers the specified content reader. 
        /// </summary>
        /// <param name="reader">Reader to register.</param>
        public void RegisterContentReader(IContentReader reader)
        {
            _readerWriter.AddReader(reader);
            Trace.TraceInformation("Added content reader \"{0}\" for content types [{1}]", reader.GetType().Name, String.Join(", ", reader.ContentTypes));
        }

        /// <summary>
        /// Registers the specified content writer.
        /// </summary>
        /// <param name="writer">Writer to register.</param>
        public void RegisterContentWriter(IContentWriter writer)
        {
            _readerWriter.AddWriter(writer);
            Trace.TraceInformation("Added content writer \"{0}\" for content type [{1}]", writer.GetType().Name, writer.ContentType);
        }

        public IContentReaderWriterService ReaderWriter
        {
            get
            {
                return _readerWriter;
            }
        }

        /// <summary>
        /// Starts the application server and registered channels.
        /// </summary>
        /// <returns>Asynchronous task.</returns>
        public async Task StartAsync()
        {
            this.GuardIsNotRunning();


            if (_channels.Count == 0)
            {
                throw new InvalidOperationException("No application channels have been registered to the application server");
            }
            else if (_applications.Count == 0)
            {
                throw new InvalidOperationException("No applications have been registered with the application server");
            }

            _isRunning = true;

            // Start all the channels
            _channelTasks = _channels.Select(s => s.StartAsync(this, _readerWriter, _cancellationSource.Token)).ToArray();

            Trace.TraceInformation("Application server running with {0} applications and {1} channels...", _applications.Count, _channels.Count);

            // Wait for all channels to complete (this will come from cancellation) 
            await Task.WhenAll(_channelTasks);

            Trace.TraceInformation("All channels shutdown");
        }

        /// <summary>
        /// Stops the application server and waits for shutdown.
        /// </summary>
        public void Stop()
        {
            Trace.TraceInformation("Shutting down application server...");
            _cancellationSource.Cancel();
            Task.WaitAll(_channelTasks);
        }

        /// <summary>
        /// Handle the specified request.
        /// </summary>
        /// <param name="request">Request to handle.</param>
        /// <returns>Response for request.</returns>
        public async Task<IResponse> HandleAsync(IRequest request, bool passThrough)
        {
            var ctx = new RequestContext(request);

            using (ctx.Monitor(MonitorEventLevel.Verbose, "Global.EndToEnd"))
            {
                var originalVerb = request.Method;

                // HEAD asks for the response identical to the one that would correspond to a GET request, but without the response body.
                if (request.Method == RequestVerb.Head)
                {
                    request = new ModifiedVerbRequest(request, RequestVerb.Get);
                }

                // Try each application , if we get a handled response back, pass it back to the caller. 
                foreach (var application in _applications)
                {
                    var response = await HandleApplicationRequestAsync(ctx, application);

                    if (response.Status != ResponseStatus.NotHandled)
                    {
                        if (originalVerb == RequestVerb.Head)
                        {
                            response = new NullBodyResponse(response);
                        }

                        return response;
                    }
                }
            }


            // No application processed this, return a not found result. 
            Trace.TraceWarning("Unknown request [{0}]", GetRequestIdentityString(ctx));

            return passThrough ? null : s_notFoundResponse;
        }

        private async Task<IResponse> HandleApplicationRequestAsync(IRequestContext request, IApplication application)
        {
            IResponse result = null;
            Exception handleException = null;

            try
            {
                result = await application.TryProcessAsync(request);
            }
            catch (Exception e)
            {
                handleException = e;
            }

            if (result == null && handleException == null)
            {
                string errorMessage = String.Format("Application \"{0}\" return a null response result");

                Trace.TraceError(errorMessage);
                return Response.Error(errorMessage);
            }
            else if (handleException != null)
            {
                Trace.TraceError("Unexpected {0} for request [1]", handleException.GetType().Name, GetRequestIdentityString(request));
                Trace.TraceError(handleException.ToString());
                var resp = Response.Error(handleException);

                resp.Headers.Add("Runtime-Unhandled-Exception", handleException.GetType().FullName);

                // Log exception message 
                var detailObject = new { Request = request.Request.Resource, RequestParms = request.Request.ResourceParameters.ValuePairs, Exception = handleException };

                _eventSource.Raise(MonitorEventLevel.Error,
                                  entityType: "Server",
                                  entityIdentity: "",
                                  user: request.Request.User.Identity.Name,
                                  name: "RuntimeException",
                                  detail: JsonConvert.SerializeObject(detailObject));

                return resp;
            }

            return result;
        }

        private static string GetRequestIdentityString(IRequestContext ctx)
        {
            return String.Format("Resource: {0}, Method: {1}, User: {2}", ctx.Request.Resource, ctx.Request.Method, ctx.Request.User.Identity.Name);
        }

        private void GuardIsRunning()
        {
            if (!_isRunning)
            {
                throw new InvalidOperationException("Application server is running");
            }
        }

        private void GuardIsNotRunning()
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Application server is not running");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (disposed) return;

            if (_cancellationSource != null)
            {
                _cancellationSource.Dispose();
            }

            disposed = true;
        }
    }
}
