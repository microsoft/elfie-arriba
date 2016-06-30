// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    /// <summary>
    /// Content reader writer service. 
    /// </summary>
    public interface IContentReaderWriterService
    {
        /// <summary>
        /// Gets a content reader for the specified content type.
        /// </summary>
        /// <typeparam name="T">Type to read content as.</typeparam>
        /// <param name="contentType">Content type to read as.</param>
        /// <returns>Content type reader.</returns>
        IContentReader GetReader<T>(string contentType);

        /// <summary>
        /// Gets a content writer for the specified content type, ensuring the content writer is capable of writing the specified content object.
        /// </summary>
        /// <param name="contentType">Content type to write output as.</param>
        /// <param name="content">Content to write.</param>
        /// <returns>Content type reader writer.</returns>
        IContentWriter GetWriter(string contentType, object content);

        /// <summary>
        /// Gets a content writer for the specified content type, ensuring the content writer is capable of writing the specified content object.
        /// </summary>
        /// <param name="contentType">Content type to write output as.</param>
        /// <param name="defaultContentType">Fallback content type if the lookup fails.</param>
        /// <param name="content">Content to write.</param>
        /// <returns>Content type reader writer.</returns>
        IContentWriter GetWriter(string contentType, string defaultContentType, object content);

        /// <summary>
        /// Gets a content writer for the specified content types, ensuring the content writer is capable of writing the specified content object.
        /// </summary>
        /// <param name="contentTypes">Ordered content type preference set.</param>
        /// <param name="content">Content to write.</param>
        /// <returns>Content type reader writer.</returns>
        IContentWriter GetWriter(IEnumerable<string> contentTypes, object content);

        /// <summary>
        /// Gets a content writer for the specified content types, ensuring the content writer is capable of writing the specified content object.
        /// </summary>
        /// <param name="contentTypes">Ordered content type preference set.</param>
        /// <param name="defaultContentType">Fallback content type if the lookup fails.</param>
        /// <param name="content">Content to write.</param>
        /// <returns>Content type reader writer.</returns>
        IContentWriter GetWriter(IEnumerable<string> contentTypes, string defaultContentType, object content);
    }
}
