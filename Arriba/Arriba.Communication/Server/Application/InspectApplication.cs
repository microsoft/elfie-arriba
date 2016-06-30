// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;

namespace Arriba.Server.Application
{
    internal class InspectApplication : ArribaApplication
    {
        [ImportingConstructor]
        public InspectApplication(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        {
            this.Get("/inspect/memory", this.Memory);
            this.Get("/inspect/machine", this.Machine);
        }

        private ArribaResponse Memory(IRequestContext request, Route routedata)
        {
            Process currentProcess = Process.GetCurrentProcess();

            return ArribaResponse.Ok(new Dictionary<string, object>(5)
            {
                { "totalGCBytes", GC.GetTotalMemory(false) },
                { "totalProcessBytes", currentProcess.WorkingSet64 },
                { "environmentWorkingSet", Environment.WorkingSet }
            });
        }

        private ArribaResponse Machine(IRequestContext rquest, Route data)
        {
            return ArribaResponse.Ok(new
            {
                MachineName = Environment.MachineName,
                OsVersion = Environment.OSVersion,
                OsBitness = Environment.Is64BitOperatingSystem ? 64 : 32,
                ProcessBitness = Environment.Is64BitProcess ? 64 : 32,
                ProcessorCount = Environment.ProcessorCount
            });
        }
    }
}
