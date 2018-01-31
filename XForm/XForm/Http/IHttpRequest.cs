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

        bool HasRequestBody { get; }
        Stream RequestBody { get; }
    }

    /// <summary>
    ///  HttpListenerContextAdapter converts an HttpListenerContext [System.Net] to an IHttpRequest
    /// </summary>
    public class HttpListenerContextAdapter : IHttpRequest
    {
        private HttpListenerContext Context;

        public HttpListenerContextAdapter(HttpListenerContext context)
        {
            this.Context = context;
        }

        public IPrincipal User => Context.User;

        public Uri Url => Context.Request.Url;

        public NameValueCollection QueryString => Context.Request.QueryString;

        public bool HasRequestBody => Context.Request.HasEntityBody;
        public Stream RequestBody => Context.Request.InputStream;
    }

    /// <summary>
    ///  HttpContextAdapter maps an HttpContext [System.Web] to an IHttpRequest
    /// </summary>
    public class HttpContextAdapter : IHttpRequest
    {
        private HttpContext Context;

        public HttpContextAdapter(HttpContext context)
        {
            this.Context = context;
        }

        public IPrincipal User => Context.User;

        public Uri Url => Context.Request.Url;

        public NameValueCollection QueryString => Context.Request.QueryString;

        public bool HasRequestBody => Context.Request.ContentLength > 0;

        public Stream RequestBody => Context.Request.InputStream;
    }
}
