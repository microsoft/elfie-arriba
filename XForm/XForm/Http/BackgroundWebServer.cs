// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

using XForm.Http;

namespace XForm
{
    /// <summary>
    ///  BackgroundWebServer handles Http Requests for the XForm engine on a background thread.
    /// </summary>
    /// <remarks>
    ///   Access Rules:
    ///     - HttpListener cannot register for any host name [http://+:5073] unless a URL ACL has been set first.
    ///     - HttpListener *can* register for localhost only unelevated [http://localhost:5073].
    ///     - Register for a URL ACL by running elevated: 'netsh http add urlacl url="http://+:5073" user=[DOMAIN\User]'
    ///     - Delete the URL ACL with: 'netsh http delete urlacl url="http://+:5073"'
    /// </remarks>
    public class BackgroundWebServer : IDisposable
    {
        private bool IsRunning { get; set; }
        private bool UsingAuthentication { get; set; }
        private HttpListener Listener { get; set; }
        private Thread ListenerThread { get; set; }

        private ushort PortNumber { get; set; }
        private string DefaultDocument { get; set; }
        private Dictionary<string, Action<IHttpRequest, IHttpResponse>> MethodsToServe { get; set; }
        private Dictionary<string, string> FilesToServe { get; set; }
        private string FolderToServe { get; set; }

        public BackgroundWebServer(ushort portNumber, string defaultDocument, string serveUnderRelativePath)
        {
            this.PortNumber = portNumber;
            this.IsRunning = false;

            this.DefaultDocument = defaultDocument;
            this.MethodsToServe = new Dictionary<string, Action<IHttpRequest, IHttpResponse>>(StringComparer.OrdinalIgnoreCase);
            this.FilesToServe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!String.IsNullOrEmpty(serveUnderRelativePath))
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                this.FolderToServe = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assembly.Location), serveUnderRelativePath));
                if (!Directory.Exists(this.FolderToServe)) this.FolderToServe = null;
            }
        }

        public void AddResponder(string url, Action<IHttpRequest, IHttpResponse> itemWrite)
        {
            this.MethodsToServe[url] = itemWrite;
        }

        public void AddFile(string url, string localPath)
        {
            this.FilesToServe[url] = localPath;
        }

        #region Start/Stop
        public void Run()
        {
            this.IsRunning = true;

            // Start the background thread (it'll write a running message)
            this.ListenerThread = new Thread(StartWithinThread);
            this.ListenerThread.IsBackground = true;
            this.ListenerThread.Start();

            // Wait for a newline on this thread and then stop
            Console.ReadLine();
            this.Stop();
        }

        private void StartForBinding(bool withAuthentication, params string[] urls)
        {
            this.UsingAuthentication = withAuthentication;
            this.Listener = new HttpListener();
            if (withAuthentication)
            {
                this.Listener.AuthenticationSchemeSelectorDelegate = (request) => (request.HttpMethod == "OPTIONS" ? AuthenticationSchemes.Anonymous : AuthenticationSchemes.Negotiate);
            }

            foreach (string url in urls)
            {
                this.Listener.Prefixes.Add(url);
            }

            this.Listener.Start();
        }

        private void StartWithinThread()
        {
            try
            {
                try
                {
                    // Try binding for all names on port
                    StartForBinding(true, $"http://+:{PortNumber}/");
                    Console.WriteLine($"Web Server running; browse to http://localhost:{PortNumber}. [Local and Remote]\r\nPress enter to stop server.");
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 5)
                {
                    // If we couldn't get the binding, there's no URL ACL set yet
                    this.Listener = null;
                }

                if (this.Listener == null)
                {
                    // Try binding to just localhost
                    StartForBinding(false, $"http://localhost:{PortNumber}/");
                    Console.WriteLine($"Web Server running; browse to http://localhost:{PortNumber}. [Local Only]");
                    Console.WriteLine(" To enable remote: https://github.com/Microsoft/elfie-arriba/wiki/XForm-Http-Remote-Access");
                    Console.WriteLine("Press enter to stop server.");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("BackgroundWebServer Thread Startup Error: " + ex.ToString());
                return;
            }

            while (this.IsRunning)
            {
                try
                {
                    HandleRequest(this.Listener.GetContext());
                }
                catch (HttpListenerException)
                {
                    // Happens when connection to client lost before response sent (request cancelled). Do nothing.
                }
                catch (ThreadAbortException)
                {
                    // This is expected - no logging, allow thread to stop
                    break;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("BackgroundWebServer Thread Error: " + ex.ToString());
                }
            }
        }

        public void Stop()
        {
            // Tell thread to stop and give it a moment to finish cleanly
            this.IsRunning = false;
            Thread.Sleep(50);

            // Stop thread first
            if (this.ListenerThread != null)
            {
                if (this.ListenerThread.IsAlive) this.ListenerThread.Abort();
                this.ListenerThread = null;
            }

            // Stop and clean up listener
            if (this.Listener != null)
            {
                ((IDisposable)this.Listener).Dispose();
                this.Listener = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
        #endregion

        #region Request Handling
        public void HandleRequest(HttpListenerContext context)
        {
            HandleRequest(new HttpListenerContextAdapter(context), new HttpListenerResponseAdapter(context.Response));
        }

        public void HandleRequest(IHttpRequest request, IHttpResponse response)
        {
            try
            {
                string origin = request.Headers["Origin"] ?? "*";
                response.AddHeader("Access-Control-Allow-Origin", origin);
                response.AddHeader("Access-Control-Allow-Credentials", "true");
                response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");

                // If the request is not authenticated, just return the CORS header
                if (this.UsingAuthentication && request.User == null)
                {
                    response.StatusCode = 200;
                    return;
                }

                // Add header to ask IE not to use compatibility mode
                response.AddHeader("X-UA-Compatible", "IE=edge");

                // Get the URI requested
                string localPath = request.Url.LocalPath;
                if (!String.IsNullOrEmpty(localPath)) localPath = localPath.TrimStart('/');

                // Respond with the default document, a JSON item, file, or a folder item, or not found
                if (ReturnDefaultDocument(localPath, response)) return;
                if (ReturnMethodItem(localPath, request, response)) return;
                if (ReturnFileItem(localPath, response)) return;
                if (ReturnServedFolderFile(localPath, response)) return;
                ReturnNotFound(response);
            }
            catch (Exception ex)
            {
                // On unhandled exception, return error and rethrow
                ReturnError(response);
                throw ex;
            }
        }

        private bool ReturnDefaultDocument(string requestUri, IHttpResponse response)
        {
            if (String.IsNullOrEmpty(requestUri))
            {
                return ReturnServedFolderFile(this.DefaultDocument, response);
            }

            return false;
        }

        private bool ReturnMethodItem(string requestUri, IHttpRequest request, IHttpResponse response)
        {
            Action<IHttpRequest, IHttpResponse> writeMethod;

            while (true)
            {
                // Look for a handler for the passed URI
                if (this.MethodsToServe.TryGetValue(requestUri, out writeMethod))
                {
                    response.AddHeader("Cache-Control", "no-cache, no-store");
                    response.StatusCode = 200;

                    writeMethod(request, response);
                    response.Close();
                    return true;
                }

                // If not found, look for handlers for prefixes of the URI (which will route)
                int lastSlash = requestUri.LastIndexOf('/');
                if (lastSlash == -1) break;
                requestUri = requestUri.Substring(0, lastSlash);
            }

            return false;
        }

        private bool ReturnFileItem(string requestUri, IHttpResponse response)
        {
            string filePath;
            if (this.FilesToServe.TryGetValue(requestUri, out filePath))
            {
                RespondWithFile(filePath, response);
                return true;
            }

            return false;
        }

        private bool ReturnServedFolderFile(string requestUri, IHttpResponse response)
        {
            if (String.IsNullOrEmpty(this.FolderToServe)) return false;
            if (String.IsNullOrEmpty(requestUri)) return false;

            string localServablePath = Path.GetFullPath(Path.Combine(this.FolderToServe, requestUri));
            if (localServablePath.StartsWith(this.FolderToServe) && File.Exists(localServablePath))
            {
                RespondWithFile(localServablePath, response);
                response.Close();
                return true;
            }

            return false;
        }

        private void RespondWithFile(string filePath, IHttpResponse response)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                response.AddHeader("Cache-Control", "max-age=60");
                response.ContentType = ContentType(filePath);
                reader.BaseStream.CopyTo(response.OutputStream);
                response.StatusCode = 200;
            }

            response.Close();
        }

        private void ReturnNotFound(IHttpResponse response)
        {
            response.StatusCode = 404;
            response.Close();
        }

        private void ReturnError(IHttpResponse response)
        {
            response.StatusCode = 500;
            response.Close();
        }

        private string ContentType(string filePath)
        {
            // Use System.Web.MimeMapping.GetMimeMapping(filePath) if willing to reference System.Web.
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".html":
                case ".htm":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "text/javascript";
                case ".json":
                    return "application/json";
                default:
                    return "text/plain";
            }
        }
        #endregion

    }
}
