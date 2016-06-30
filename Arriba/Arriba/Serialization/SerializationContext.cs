// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Arriba.Serialization
{
    public interface ISerializationContext : IDisposable
    {
        Stream Stream { get; }
        BinaryReader Reader { get; }
        BinaryWriter Writer { get; }
    }

    public class SerializationContext : ISerializationContext
    {
        public Stream Stream { get; private set; }
        public BinaryReader Reader { get; private set; }
        public BinaryWriter Writer { get; private set; }

        public SerializationContext(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            this.Stream = stream;
            if (stream.CanRead) this.Reader = new BinaryReader(stream);
            if (stream.CanWrite) this.Writer = new BinaryWriter(stream);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (this.Reader != null)
            {
                this.Reader.Dispose();
                this.Reader = null;
            }

            if (this.Writer != null)
            {
                this.Writer.Dispose();
                this.Writer = null;
            }

            if (this.Stream != null)
            {
                this.Stream.Dispose();
                this.Stream = null;
            }
        }
    }
}
