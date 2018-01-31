using System.IO;
using System.Web;
using XForm.Http;

namespace XForm.IIS.Http
{
    /// <summary>
    ///  HttpResponseAdapter converts an HttpResponse [System.Web] to an IHttpResponse.
    /// </summary>
    public class HttpResponseAdapter : IHttpResponse
    {
        private HttpResponse Response;

        public HttpResponseAdapter(HttpResponse response)
        {
            this.Response = response;
        }

        public int StatusCode
        {
            get { return Response.StatusCode; }
            set { Response.StatusCode = value; }
        }

        public string ContentType
        {
            get { return Response.ContentType; }
            set { Response.ContentType = value; }
        }

        public Stream OutputStream => Response.OutputStream;

        public void AddHeader(string name, string value)
        {
            Response.AddHeader(name, value);
        }

        public void Close()
        {
            // Do not call Response.Close() - ERR_CONNECTION_RESET in browser if you do
        }
    }
}
