// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Arriba.Monitoring;
using Arriba.Serialization;
using Arriba.Server.Owin;

using Microsoft.Owin;
using Microsoft.Owin.Extensions;

using Owin;

[assembly: OwinStartup(typeof(Arriba.IIS.IISArribaStartup))]

namespace Arriba.IIS
{
    public class IISArribaStartup : ArribaOwinStartup
    {
        public override void Configuration(IAppBuilder app)
        {
            string csvLogsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\Logs");
            EventPublisher.AddConsumer(new CsvEventConsumer(csvLogsPath));

            // TODO: this should be configurable.
            BinarySerializable.CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\DiskCache");

            // App configuration 
            base.Configuration(app);

            // Enable requests that are not handled to use the regular pipeline 
            app.UseStageMarker(Owin.PipelineStage.MapHandler);
        }
    }
}