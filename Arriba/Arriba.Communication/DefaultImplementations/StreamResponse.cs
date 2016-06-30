// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Arriba.Communication
{
    internal class StreamResponse : StreamWriterResponse
    {
        public StreamResponse(string contentType, Stream stream)
            : base(contentType)
        {
            this.Stream = stream;
        }

        public Stream Stream
        {
            get;
            private set;
        }

        public override Task WriteToStreamAsync(Stream outputStream)
        {
            return Stream.CopyToAsync(outputStream);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (this.Stream != null)
            {
                this.Stream.Dispose();
            }

            base.Dispose();
        }
    }
}
