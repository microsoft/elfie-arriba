// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Transforms;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class ByteTypeProvider : ITypeProvider
    {
        public string Name => typeof(byte).Name;

        public Type Type => typeof(byte);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath)
        {
            return new ByteReader(streamProvider.OpenRead(Path.Combine(columnPath, "V.u8.bin")));
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            Directory.CreateDirectory(columnPath);
            return new ByteWriter(streamProvider.OpenWrite(Path.Combine(columnPath, "V.u8.bin")));
        }

        public ComparerExtensions.Comparer TryGetComparer(CompareOperator op)
        {
            return new ByteComparer().TryBuild(op);
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            // TODO: Add primitive number conversions
            return null;
        }
    }

    public class ByteReader : IColumnReader
    {
        private Stream _stream;
        private byte[] _array;

        public ByteReader(Stream stream)
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
        private Stream _stream;
        private byte[] _buffer;

        public ByteWriter(Stream stream)
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
