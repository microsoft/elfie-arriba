// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

namespace XForm
{
    /// <summary>
    ///  IHttpResponse is a generic interface for interacting with System.Net.HttpListenerResponse and System.Net.Http.HttpResponseMessage uniformly.
    /// </summary>
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; set; }
        string ContentType { get; set; }
        Stream OutputStream { get; }
    }

    public class HttpListenerResponseWrapper : IHttpResponse
    {
        private HttpListenerResponse Response;

        public HttpListenerResponseWrapper(HttpListenerResponse response)
        {
            this.Response = response;
        }

        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode)Response.StatusCode; }
            set { Response.StatusCode = (int)value; }
        }

        public string ContentType
        {
            get { return Response.ContentType; }
            set { Response.ContentType = value; }
        }

        public Stream OutputStream => Response.OutputStream;
    }

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
        private HttpListener Listener { get; set; }
        private Thread ListenerThread { get; set; }

        private string DefaultDocument { get; set; }
        private Dictionary<string, Action<HttpListenerContext, IHttpResponse>> MethodsToServe { get; set; }
        private Dictionary<string, string> FilesToServe { get; set; }
        private string FolderToServe { get; set; }

        public BackgroundWebServer(string defaultDocument, string serveUnderRelativePath)
        {
            this.IsRunning = false;

            this.DefaultDocument = defaultDocument;
            this.MethodsToServe = new Dictionary<string, Action<HttpListenerContext, IHttpResponse>>(StringComparer.OrdinalIgnoreCase);
            this.FilesToServe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!String.IsNullOrEmpty(serveUnderRelativePath))
            {
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly == null) entryAssembly = Assembly.GetCallingAssembly();

                this.FolderToServe = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(entryAssembly.Location), serveUnderRelativePath));
                if (!Directory.Exists(this.FolderToServe)) this.FolderToServe = null;
            }
        }

        public void AddResponder(string url, Action<HttpListenerContext, IHttpResponse> itemWrite)
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

        private void StartForBinding(params string[] urls)
        {
            this.Listener = new HttpListener();

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
                    // Try binding for all names on port 5073
                    StartForBinding("http://+:5073/");
                    Console.WriteLine("Http Server running; browse to http://localhost:5073. [Local and Remote]\r\nPress enter to stop server.");
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 5)
                {
                    // If we couldn't get the binding, there's no URL ACL set yet
                    this.Listener = null;
                }

                if (this.Listener == null)
                {
                    // Try binding to just localhost:5073
                    StartForBinding("http://localhost:5073/");
                    Console.WriteLine("Http Server running; browse to http://localhost:5073. [Local Only]");
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
                catch (HttpListenerException ex)
                {
                    // Happens when connection to client lost before response sent
                    Trace.TraceWarning(String.Format("BackgroundWebServer: Lost Connection During Request. Retrying. Error: {0}", ex.ToString()));
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
        private void HandleRequest(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;

            try
            {
                // Add header to ask IE not to use compatibility mode
                response.AddHeader("X-UA-Compatible", "IE=edge");

                // Add CORS header to allow requests from other domains
                response.AddHeader("Access-Control-Allow-Origin", "*");

                // Get the URI requested
                string localPath = context.Request.Url.LocalPath;
                if (!String.IsNullOrEmpty(localPath)) localPath = localPath.TrimStart('/');

                // Respond with the default document, a JSON item, file, or a folder item, or not found
                if (ReturnDefaultDocument(localPath, response)) return;
                if (ReturnMethodItem(localPath, context, response)) return;
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

        private bool ReturnDefaultDocument(string requestUri, HttpListenerResponse response)
        {
            if (String.IsNullOrEmpty(requestUri))
            {
                return ReturnServedFolderFile(this.DefaultDocument, response);
            }

            return false;
        }

        private bool ReturnMethodItem(string requestUri, HttpListenerContext context, HttpListenerResponse response)
        {
            Action<HttpListenerContext, IHttpResponse> writeMethod;
            if (this.MethodsToServe.TryGetValue(requestUri, out writeMethod))
            {
                response.AddHeader("Cache-Control", "no-cache, no-store");
                response.ContentType = ContentType(requestUri);
                response.StatusCode = 200;

                writeMethod(context, new HttpListenerResponseWrapper(response));
                response.Close();
                return true;
            }

            return false;
        }

        private bool ReturnFileItem(string requestUri, HttpListenerResponse response)
        {
            string filePath;
            if (this.FilesToServe.TryGetValue(requestUri, out filePath))
            {
                RespondWithFile(filePath, response);
                return true;
            }

            return false;
        }

        private bool ReturnServedFolderFile(string requestUri, HttpListenerResponse response)
        {
            if (String.IsNullOrEmpty(this.FolderToServe)) return false;
            if (String.IsNullOrEmpty(requestUri)) return false;

            string localServablePath = Path.GetFullPath(Path.Combine(this.FolderToServe, requestUri));
            if (localServablePath.StartsWith(this.FolderToServe) && File.Exists(localServablePath))
            {
                RespondWithFile(localServablePath, response);
                return true;
            }

            return false;
        }

        private void RespondWithFile(string filePath, HttpListenerResponse response)
        {
            using (StreamWriter writer = new StreamWriter(response.OutputStream))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    response.AddHeader("Cache-Control", "max-age=60");
                    response.ContentType = ContentType(filePath);
                    reader.BaseStream.CopyTo(writer.BaseStream);
                }
            }

            response.StatusCode = 200;
            response.Close();
        }

        private void ReturnNotFound(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.Close();
        }

        private void ReturnError(HttpListenerResponse response)
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
