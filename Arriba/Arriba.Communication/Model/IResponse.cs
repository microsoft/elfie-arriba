// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Arriba.Communication
{
    /// <summary>
    /// Defines a contract for encapsulating request responses. 
    /// </summary>
    public interface IResponse : IDisposable
    {
        /// <summary>
        /// Status of the response. 
        /// </summary>
        ResponseStatus Status { get; }

        /// <summary>
        /// Gets the payload of the response. 
        /// </summary>
        object ResponseBody { get; }

        /// <summary>
        /// Gets a writable value bag for the response headers. 
        /// </summary>
        IWritableValueBag Headers { get; }

        /// <summary>
        ///  Add a given header to the response headers.
        /// </summary>
        /// <param name="key">Key of header to add (Cache-Control)</param>
        /// <param name="value">Value of header to add (no-cache)</param>
        void AddHeader(string key, string value);
    }

    /// <summary>
    /// Defines a contract for encapsulating request responses. 
    /// </summary>
    public interface IResponse<T> : IResponse
    {
        /// <summary>
        /// Gets the payload of the response. 
        /// </summary>
        new T ResponseBody { get; }
    }
}
