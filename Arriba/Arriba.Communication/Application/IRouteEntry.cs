// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Arriba.Communication.Application
{
    /// <summary>
    /// Encapsulates a routes matching and processing functionality. 
    /// </summary>
    internal interface IRouteHandler
    {
        RouteMatcher Matcher { get; }

        Task<IResponse> TryHandleAsync(IRequestContext ctx, Route data);
    }
}
