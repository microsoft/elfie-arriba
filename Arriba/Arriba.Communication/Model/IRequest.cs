// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Defines a contract for client requests. 
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// Gets the request method. 
        /// </summary>
        RequestVerb Method { get; }

        /// <summary>
        /// Gets the resource requested. 
        /// </summary>
        string Resource { get; }

        IEnumerable<string> AcceptedResponseTypes { get; }

        /// <summary>
        /// Gets the resource parameters.
        /// </summary>
        IValueBag ResourceParameters { get; }

        /// <summary>
        /// Gets the headers of the request. 
        /// </summary>
        IValueBag Headers { get; }

        /// <summary>
        /// Gets the user who requested the resource. 
        /// </summary>
        IPrincipal User { get; }

        /// <summary>
        /// Gets the request origin. 
        /// </summary>
        string Origin { get; }

        /// <summary>
        /// Gets a value indicating whether the request has a body. 
        /// </summary>
        bool HasBody { get; }

        Stream InputStream { get; }

        /// <summary>
        /// Asyncronously reads the body of the request. 
        /// </summary>
        /// <typeparam name="T">Type to read the body as.</typeparam>
        /// <returns>An asyncronous task that retursn the request body.</returns>
        Task<T> ReadBodyAsync<T>();
    }
}
