// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Defines a constract for communication channels to communicate with applications. 
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// Gets the description of the channel. 
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Starts the channel listening. 
        /// </summary>
        /// <param name="handler">Request handler to dispatch requsts to.</param>
        /// <param name="readerWriter">Service for type serialization and deserialization.</param>
        /// <param name="cancellation">Cancellation token, that, when signaled the channel should end.</param>
        /// <returns>Asynchronous representing the lifetime of the channel.</returns>
        Task StartAsync(IRequestHandler handler, IContentReaderWriterService readerWriter, CancellationToken cancellation);
    }
}
