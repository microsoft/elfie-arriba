// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net;
using System.Threading;
using System.Web;

namespace XForm.Http
{
    /// <summary>
    ///  IHttpResponse is a generic interface for interacting with Http responses of different concrete types.
    /// </summary>
    public interface IHttpResponse
    {
        int StatusCode { get; set; }
        string ContentType { get; set; }
        void AddHeader(string name, string value);

        CancellationToken ClientDisconnectedToken { get; }
        Stream OutputStream { get; }
        void Close();
    }

    /// <summary>
    ///  HttpListenerResponseAdapter converts an HttpListenerResponse [System.Net] to an IHttpResponse.
    /// </summary>
    public class HttpListenerResponseAdapter : IHttpResponse
    {
        private HttpListenerResponse _response;

        public HttpListenerResponseAdapter(HttpListenerResponse response)
        {
            _response = response;
        }

        public int StatusCode
        {
            get { return (int)_response.StatusCode; }
            set { _response.StatusCode = (int)value; }
        }

        public string ContentType
        {
            get { return _response.ContentType; }
            set { _response.ContentType = value; }
        }

        public void AddHeader(string name, string value)
        {
            _response.AddHeader(name, value);
        }

        public Stream OutputStream => _response.OutputStream;

        // HttpListener doesn't expose whether the client disconnected
        public CancellationToken ClientDisconnectedToken => default(CancellationToken);

        public void Close()
        {
            _response.Close();
        }
    }

    /// <summary>
    ///  HttpResponseAdapter converts an HttpResponse [System.Web] to an IHttpResponse.
    /// </summary>
    public class HttpResponseAdapter : IHttpResponse
    {
        private HttpResponse _response;

        public HttpResponseAdapter(HttpResponse response)
        {
            _response = response;
        }

        public int StatusCode
        {
            get { return _response.StatusCode; }
            set { _response.StatusCode = value; }
        }

        public string ContentType
        {
            get { return _response.ContentType; }
            set { _response.ContentType = value; }
        }

        public void AddHeader(string name, string value)
        {
            _response.AddHeader(name, value);
        }

        public CancellationToken ClientDisconnectedToken => _response.ClientDisconnectedToken;

        public Stream OutputStream => _response.OutputStream;

        public void Close()
        {
            // Do not call Response.Close() - ERR_CONNECTION_RESET in browser if you do
        }
    }
}
