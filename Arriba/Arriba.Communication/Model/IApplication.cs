// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Defines a contract for processing requests for applications. 
    /// </summary>
    public interface IApplication
    {
        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Asynchronously attempts to process the specified request. 
        /// </summary>
        Task<IResponse> TryProcessAsync(IRequestContext request);
    }
}
