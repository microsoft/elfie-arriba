// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using XForm.Data;

namespace XForm.Types
{
    public class PrimitiveTypeProvider<T> : ITypeProvider
    {
        public string Name => typeof(T).Name;

        public Type Type => typeof(T);

        public PrimitiveTypeProvider()
        {
            if (!typeof(T).IsPrimitive) throw new ArgumentException($"PrimitiveTypeProvider does not support non-primitive type {typeof(T).Name}");
        }

        public IColumnReader BinaryReader(string columnPath)
        {
            return new PrimitiveArrayReader<T>(new FileStream(ValuesFilePath(columnPath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public IColumnWriter BinaryWriter(string columnPath)
        {
            Directory.CreateDirectory(columnPath);
            return new PrimitiveArrayWriter<T>(new FileStream(ValuesFilePath(columnPath), FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete));
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue)
        {
            // TODO: Add primitive number conversions
            return null;
        }

        public static string ValuesFilePath(string columnPath)
        {
            return Path.Combine(columnPath, $"V.{BinaryFileTypePart()}.bin");
        }

        public static string BinaryFileTypePart()
        {
            if (typeof(T) == typeof(bool)) return "b8";

            if (typeof(T) == typeof(sbyte)) return "i8";
            if (typeof(T) == typeof(byte)) return "u8";
            if (typeof(T) == typeof(short)) return "i16";
            if (typeof(T) == typeof(ushort)) return "u16";
            if (typeof(T) == typeof(int)) return "i32";
            if (typeof(T) == typeof(uint)) return "u32";
            if (typeof(T) == typeof(long)) return "i64";
            if (typeof(T) == typeof(ulong)) return "u64";
            if (typeof(T) == typeof(float)) return "f32";
            if (typeof(T) == typeof(double)) return "f64";

            throw new ArgumentException($"PrimitiveTypeProvider doesn't know how to read type {typeof(T).Name}.");
        }
    }

    public class PrimitiveArrayReader<T> : IColumnReader
    {
        private int _bytesPerItem;
        private FileStream _stream;
        private byte[] _bytesBuffer;
        private T[] _array;

        public PrimitiveArrayReader(FileStream stream)
        {
            _stream = stream;
            _bytesPerItem = (typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>());
        }

        public int Count => (int)(_stream.Length / _bytesPerItem);

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            int bytesToRead = _bytesPerItem * selector.Count;

            Allocator.AllocateToSize(ref _bytesBuffer, bytesToRead);
            Allocator.AllocateToSize(ref _array, selector.Count);

            _stream.Seek(_bytesPerItem * selector.StartIndexInclusive, SeekOrigin.Begin);
            _stream.Read(_bytesBuffer, 0, bytesToRead);
            Buffer.BlockCopy(_bytesBuffer, 0, _array, 0, bytesToRead);

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

    public class PrimitiveArrayWriter<T> : IColumnWriter
    {
        private int _bytesPerItem;
        private FileStream _stream;
        private byte[] _bytesBuffer;

        public PrimitiveArrayWriter(FileStream stream)
        {
            _stream = stream;
            _bytesPerItem = (typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>());
        }

        public void Append(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _bytesBuffer, _bytesPerItem * batch.Count);

            if (batch.Selector.Indices == null)
            {
                Buffer.BlockCopy(batch.Array, _bytesPerItem * batch.Selector.StartIndexInclusive, _bytesBuffer, 0, _bytesPerItem * batch.Count);
            }
            else
            {
                for (int i = 0; i < batch.Count; ++i)
                {
                    int index = batch.Index(i);
                    Buffer.BlockCopy(batch.Array, _bytesPerItem * index, _bytesBuffer, i * _bytesPerItem, _bytesPerItem);
                }
            }

            _stream.Write(_bytesBuffer, 0, _bytesPerItem * batch.Count);
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
