// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Configuration;
using System.IO;
using System.Web;

using XForm;
using XForm.Http;
using XForm.IO.StreamProvider;

public class RequestHandler : IHttpHandler
{
    private static HttpService s_httpService;

    private void Initialize(HttpContext context)
    {
        string productionFolderPath = ConfigurationManager.AppSettings["XFormProductionFolder"];
        if (String.IsNullOrEmpty(productionFolderPath)) throw new InvalidOperationException("XForm.IIS requires web.config to contain appSetting 'XFormProductionFolder'.");
        if (!Path.IsPathRooted(productionFolderPath)) productionFolderPath = context.Server.MapPath(productionFolderPath);

        // Enable XForm Native Acceleration
        NativeAccelerator.Enable();

        // Build the Database Context
        XDatabaseContext db = new XDatabaseContext();
        db.RequestedAsOfDateTime = DateTime.MaxValue;
        db.StreamProvider = new StreamProviderCache(new LocalFileStreamProvider(productionFolderPath));
        db.Runner = new WorkflowRunner(db);

        // Build and save an HttpService instance to run queries
        s_httpService = new HttpService(db);
    }

    public bool IsReusable => true;

    public void ProcessRequest(HttpContext context)
    {
        try
        {
            if (s_httpService == null) Initialize(context);
            s_httpService.HandleRequest(new HttpContextAdapter(context), new HttpResponseAdapter(context.Response));
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

