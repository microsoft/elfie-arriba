// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Web;

namespace XForm.Http
{
    /// <summary>
    ///  IHttpRequest is a generic interface for interacting with Http requests of different
    ///  concrete types.
    /// </summary>
    public interface IHttpRequest
    {
        IPrincipal User { get; }
        Uri Url { get; }
        NameValueCollection QueryString { get; }
        NameValueCollection Headers { get; }

        bool HasRequestBody { get; }
        Stream RequestBody { get; }
    }

    /// <summary>
    ///  HttpListenerContextAdapter converts an HttpListenerContext [System.Net] to an IHttpRequest
    /// </summary>
    public class HttpListenerContextAdapter : IHttpRequest
    {
        private HttpListenerContext _context;

        public HttpListenerContextAdapter(HttpListenerContext context)
        {
            _context = context;
        }

        public IPrincipal User => _context.User;

        public Uri Url => _context.Request.Url;

        public NameValueCollection QueryString => _context.Request.QueryString;
        public NameValueCollection Headers => _context.Request.Headers;

        public bool HasRequestBody => _context.Request.HasEntityBody;
        public Stream RequestBody => _context.Request.InputStream;
    }

    /// <summary>
    ///  HttpContextAdapter maps an HttpContext [System.Web] to an IHttpRequest
    /// </summary>
    public class HttpContextAdapter : IHttpRequest
    {
        private HttpContext _context;

        public HttpContextAdapter(HttpContext context)
        {
            _context = context;
        }

        public IPrincipal User => _context.User;

        public Uri Url => _context.Request.Url;

        public NameValueCollection QueryString => _context.Request.QueryString;
        public NameValueCollection Headers => _context.Request.Headers;

        public bool HasRequestBody => _context.Request.ContentLength > 0;

        public Stream RequestBody => _context.Request.InputStream;
    }
}
