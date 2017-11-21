// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using XForm.Data;

namespace XForm.Types
{
    public class ByteTypeProvider : ITypeProvider
    {
        public string Name => typeof(byte).Name;

        public Type Type => typeof(byte);

        public IColumnReader BinaryReader(string columnPath)
        {
            return new ByteReader(new FileStream(Path.Combine(columnPath, "V.u8.bin"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public IColumnWriter BinaryWriter(string columnPath)
        {
            Directory.CreateDirectory(columnPath);
            return new ByteWriter(new FileStream(Path.Combine(columnPath, "V.u8.bin"), FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete));
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            // TODO: Add primitive number conversions
            return null;
        }
    }

    public class ByteReader : IColumnReader
    {
        private FileStream _stream;
        private byte[] _array;

        public ByteReader(FileStream stream)
        {
            _stream = stream;
        }

        public int Count => (int)_stream.Length;

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            Allocator.AllocateToSize(ref _array, selector.Count);

            _stream.Seek(selector.StartIndexInclusive, SeekOrigin.Begin);
            _stream.Read(_array, 0, selector.Count);

            return DataBatch.All(_array, selector.Count);
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }

    public class ByteWriter : IColumnWriter
    {
        private FileStream _stream;
        private byte[] _buffer;

        public ByteWriter(FileStream stream)
        {
            _stream = stream;
        }

        public void Append(DataBatch batch)
        {
            byte[] arrayToWrite = (byte[])batch.Array;

            if (batch.Selector.Indices != null)
            {
                Allocator.AllocateToSize(ref _buffer, batch.Count);

                for (int i = 0; i < batch.Count; ++i)
                {
                    int index = batch.Index(i);
                    _buffer[i] = arrayToWrite[index];
                }

                arrayToWrite = _buffer;
            }

            _stream.Write((byte[])batch.Array, batch.Selector.StartIndexInclusive, batch.Count);
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }
    }
}
