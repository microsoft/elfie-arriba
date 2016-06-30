// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Server.Hosting;

using Microsoft.Owin.Cors;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Owin;

namespace Arriba.Server.Owin
{
    public abstract class ArribaOwinStartup
    {
        public virtual void Configuration(IAppBuilder app)
        {
            var host = new Host();
            host.Add<JsonConverter>(new StringEnumConverter());
            host.Compose();

            app.UseCors(CorsOptions.AllowAll)
               .UseArriba(host);
        }
    }
}
