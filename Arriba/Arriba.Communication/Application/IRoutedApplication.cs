// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Arriba.Communication.Application
{
    internal interface IRoutedApplication
    {
        string Name { get; }

        IEnumerable<IRouteHandler> RouteEnteries { get; }
    }
}
