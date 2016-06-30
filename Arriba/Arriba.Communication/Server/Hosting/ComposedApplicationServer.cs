// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

using Arriba.Communication;

namespace Arriba.Server
{
    /// <summary>
    /// Represents an application server that fulfills its dependnecies via composition.
    /// </summary>
    [Export(typeof(ComposedApplicationServer)), Shared]
    public class ComposedApplicationServer : ApplicationServer
    {
        [ImportingConstructor]
        public ComposedApplicationServer(
            [ImportMany] IEnumerable<IApplication> applications,
            [ImportMany] IEnumerable<IContentReader> readers,
            [ImportMany] IEnumerable<IContentWriter> writers,
            [ImportMany] IEnumerable<IChannel> channels)
        {
            if (!applications.Any())
            {
                throw new ArgumentException("No applications registered");
            }

            //if(!channels.Any())
            //{
            //    throw new ArgumentException("No channels registered");
            //}

            foreach (var application in applications)
            {
                this.RegisterApplication(application);
            }

            foreach (var reader in readers)
            {
                this.RegisterContentReader(reader);
            }

            foreach (var writer in writers)
            {
                this.RegisterContentWriter(writer);
            }

            foreach (var channel in channels)
            {
                this.RegisterRequestChannel(channel);
            }
        }
    }
}
