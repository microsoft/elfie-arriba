using System.IO;
using System.Net;

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

        Stream OutputStream { get; }
        void Close();
    }

    /// <summary>
    ///  HttpListenerResponseAdapter converts an HttpListenerResponse [System.Net] to an IHttpResponse.
    /// </summary>
    public class HttpListenerResponseAdapter : IHttpResponse
    {
        private HttpListenerResponse Response;

        public HttpListenerResponseAdapter(HttpListenerResponse response)
        {
            this.Response = response;
        }

        public int StatusCode
        {
            get { return (int)Response.StatusCode; }
            set { Response.StatusCode = (int)value; }
        }

        public string ContentType
        {
            get { return Response.ContentType; }
            set { Response.ContentType = value; }
        }

        public void AddHeader(string name, string value)
        {
            Response.AddHeader(name, value);
        }

        public Stream OutputStream => Response.OutputStream;

        public void Close()
        {
            Response.Close();
        }
    }
}
