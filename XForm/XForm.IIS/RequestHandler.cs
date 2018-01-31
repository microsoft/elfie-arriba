using System;
using System.Configuration;
using System.IO;
using System.Web;
using XForm;
using XForm.IIS.Http;
using XForm.IO.StreamProvider;

public class RequestHandler : IHttpHandler
{
    private static HttpService HttpService;

    private void Initialize(HttpContext context)
    {
        string productionFolderPath = ConfigurationManager.AppSettings["XFormProductionFolder"];
        if (String.IsNullOrEmpty(productionFolderPath)) throw new InvalidOperationException("XForm.IIS requires web.config to contain appSetting 'XFormProductionFolder'.");
        if (!Path.IsPathRooted(productionFolderPath)) productionFolderPath = context.Server.MapPath(productionFolderPath);

        XDatabaseContext db = new XDatabaseContext();
        db.RequestedAsOfDateTime = DateTime.MaxValue;
        db.StreamProvider = new StreamProviderCache(new LocalFileStreamProvider(productionFolderPath));
        db.Runner = new WorkflowRunner(db);

        HttpService = new HttpService(db);
    }

    public bool IsReusable => true;

    public void ProcessRequest(HttpContext context)
    {
        try
        {
            if (HttpService == null) Initialize(context);
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

