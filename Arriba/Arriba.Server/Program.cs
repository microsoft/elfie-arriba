// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;

using Arriba.Monitoring;
using Arriba.Server.Owin;

using Microsoft.Owin.Builder;
using Microsoft.Owin.Hosting;

using Owin;

namespace Arriba.Server
{
    internal class Program
    {
        private const int DefaultPort = 42784;

        private static void Main(string[] args)
        {
            Console.WriteLine("Arriba Local Server\r\n");

            Configuration c = Configuration.GetConfigurationForArgs(args);
            int portNumber = c.GetConfigurationInt("port", DefaultPort);

            // Write trace messages to console if /trace is specified 
            if (c.GetConfigurationBool("trace", Debugger.IsAttached))
            {
                EventPublisher.AddConsumer(new ConsoleEventConsumer());
            }

            // Always log to CSV
            EventPublisher.AddConsumer(new CsvEventConsumer());

            using (var app = WebApp.Start<SelfHostArribaOwinStartup>(String.Format("http://*:{0}/", portNumber)))
            {
                Console.WriteLine("Running... Press any key to exit.");
                Console.ReadKey();
            }

            Console.WriteLine("Exiting.");
            Environment.Exit(0);
        }
    }

    public class SelfHostArribaOwinStartup : ArribaOwinStartup
    {
        public override void Configuration(IAppBuilder app)
        {
            base.Configuration(app);

            // Enable self host NTLM authentication 
            var listener = (HttpListener)app.Properties[typeof(HttpListener).FullName];
            listener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;
        }
    }
}

