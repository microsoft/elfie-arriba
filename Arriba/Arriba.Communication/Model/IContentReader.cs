// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Defines a mechanism to deserialize content for a particular set of content types.  
    /// </summary>
    public interface IContentReader
    {
        /// <summary>
        /// Gets a set of content types supported by the reader.
        /// </summary>
        IEnumerable<string> ContentTypes { get; }

        /// <summary>
        /// Determines whether the reader can deserialize the specified type. 
        /// </summary>
        /// <param name="t">Type to validate.</param>
        /// <returns>True if the type can be serialized otherwise false.</returns>
        bool CanRead<T>();

        /// <summary>
        /// Asyncronously reads the content from the specified stream as the specified type. 
        /// </summary>
        /// <typeparam name="T">Type to read.</typeparam>
        /// <param name="input">Stream to read from.</param>
        /// <returns>Asyncronous task for the content.</returns>
        Task<T> ReadAsync<T>(Stream input);
    }
}
