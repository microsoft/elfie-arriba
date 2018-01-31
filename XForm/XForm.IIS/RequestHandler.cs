using System;
using System.IO;
using System.Web;
using XForm;
using XForm.IIS.Http;
using XForm.IO.StreamProvider;

public class RequestHandler : IHttpHandler
{
    private static HttpService HttpService;

    private void Initialize()
    {
        XDatabaseContext context = new XDatabaseContext();
        context.RequestedAsOfDateTime = DateTime.MaxValue;
        context.StreamProvider = new StreamProviderCache(new LocalFileStreamProvider(@"C:\Download\XFormProduction"));
        context.Runner = new WorkflowRunner(context);

        HttpService = new HttpService(context);
    }

    public bool IsReusable => true;

    public void ProcessRequest(HttpContext context)
    {
        try
        {
            if (HttpService == null) Initialize();
            HttpService.HandleRequest(new HttpContextAdapter(context), new HttpResponseAdapter(context.Response));
        }
        catch (Exception ex)
        {
            context.Response.Write(ex.ToString());
        }
    }

    // Test Method to respond with a payload
    private void ResponseWithPayload(HttpContext context)
    {
        // Use the OutputStream, but it must be disposed
        using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
        {
            writer.Write(context.Request.Url);
            writer.Write(context.User.Identity.Name);
        }

        // It's safe to set the StatusCode after the Dispose
        context.Response.StatusCode = 200;

        // Do not call Response.Close() - ERR_CONNECTION_RESET in browser if you do
    }

    private void NotFoundResponse(HttpContext context)
    {
        // It's safe to set the StatusCode without writing anything to the OutputStream
        context.Response.StatusCode = 404;

        // Do not call Response.Close() - ERR_CONNECTION_RESET in browser if you do
    }
}

