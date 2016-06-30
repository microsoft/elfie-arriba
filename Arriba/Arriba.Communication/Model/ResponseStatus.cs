// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Communication
{
    /// <summary>
    /// Response status.
    /// </summary>
    public enum ResponseStatus
    {
        /// <summary>
        /// Request was not handled.
        /// </summary>
        NotHandled,

        /// <summary>
        /// Resource not found. 
        /// </summary>
        NotFound,

        /// <summary>
        /// Successful result. 
        /// </summary>
        Ok,

        /// <summary>
        /// Client made a bad request.
        /// </summary>
        BadRequest,

        /// <summary>
        /// Server errored whilst processing a request.
        /// </summary>
        Error,

        /// <summary>
        /// Client does not have permissions to the resource.
        /// </summary>
        Forbidden,

        /// <summary>
        /// Resources submitted are created successfully
        /// </summary>
        Created,
    }
}
