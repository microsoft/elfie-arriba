using System;
using System.Collections.Specialized;
using System.IO;
using System.Security.Principal;
using System.Web;
using XForm.Http;

namespace XForm.IIS.Http
{
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
