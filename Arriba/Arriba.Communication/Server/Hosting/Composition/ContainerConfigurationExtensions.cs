// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;

namespace Arriba.Server.Hosting
{
    /// <summary>
    /// Extension methods for Composition.
    /// </summary>
    internal static class ContainerConfigurationExtensions
    {
        public static ContainerConfiguration WithExport<T>(this ContainerConfiguration configuration, T exportedInstance, string contractName = null, IDictionary<string, object> metadata = null)
        {
            return WithExport(configuration, exportedInstance, typeof(T), contractName, metadata);
        }

        public static ContainerConfiguration WithExport(this ContainerConfiguration configuration, object exportedInstance, Type contractType, string contractName = null, IDictionary<string, object> metadata = null)
        {
            return configuration.WithProvider(new InstanceExportDescriptorProvider(
                exportedInstance, contractType, contractName, metadata));
        }
    }
}
