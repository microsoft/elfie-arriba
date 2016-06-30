// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Defines a mechanism to serialize content for a particular content type.  
    /// </summary>
    public interface IContentWriter
    {
        /// <summary>
        /// Gets the content type serialized by the writer. 
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Determines whether the writer can serialize the specified type. 
        /// </summary>
        /// <param name="t">Type to validate.</param>
        /// <returns>True if the type can be serialized otherwise false.</returns>
        bool CanWrite(Type t);

        /// <summary>
        /// Asyncronously writes the specified content to the specified output stream. 
        /// </summary>
        /// <param name="output">Stream to write to.</param>
        /// <param name="content">Content to write.</param>
        /// <returns>Asyncronous task.</returns>
        Task WriteAsync(IRequest request, Stream output, object content);
    }
}
