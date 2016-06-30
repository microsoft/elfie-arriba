// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Defines a mechanism to to asyncronously handle requests and get responses. 
    /// </summary>
    public interface IRequestHandler
    {
        /// <summary>
        /// Handle the specified request.
        /// </summary>
        /// <param name="request">Requst.</param>
        /// <returns>Asynchrounous task that returns the response for the request.</returns>
        Task<IResponse> HandleAsync(IRequest request, bool passThrough = false);
    }
}
