// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    internal class StreamWriterResponse : Response, IStreamWriterResponse
    {
        private Func<Stream, Task> _streamWriter;

        protected StreamWriterResponse(string contentType)
            : base(ResponseStatus.Ok)
        {
            this.ContentType = contentType;
        }

        internal StreamWriterResponse(string contentType, Func<Stream, Task> streamWriter)
            : this(contentType)
        {
            if (streamWriter == null)
            {
                throw new ArgumentNullException("streamWriter");
            }

            _streamWriter = streamWriter;
        }


        public string ContentType
        {
            get;
            protected set;
        }

        public virtual Task WriteToStreamAsync(Stream outputStream)
        {
            return _streamWriter(outputStream);
        }
    }
}
