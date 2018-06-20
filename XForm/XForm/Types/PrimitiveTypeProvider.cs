// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class PrimitiveTypeProvider<T> : ITypeProvider where T : IComparable<T>
    {
        public string Name => typeof(T).Name;
        public Type Type => typeof(T);

        public PrimitiveTypeProvider()
        {
            if (!typeof(T).IsPrimitive) throw new ArgumentException($"PrimitiveTypeProvider does not support non-primitive type {typeof(T).Name}");
        }

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath, CachingOption option)
        {
            return ColumnCache.Instance.GetOrBuild(columnPath, option, () =>
            {
                string filePath = ValuesFilePath(columnPath);
                if (!streamProvider.UncachedExists(filePath)) return null;
                return new PrimitiveArrayReader<T>(streamProvider.OpenRead(filePath));
            });
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new PrimitiveArrayWriter<T>(streamProvider.OpenWrite(ValuesFilePath(columnPath)));
        }

        public NegatedTryConvert TryGetNegatedTryConvert(Type sourceType, Type targetType, object defaultValue)
        {
            return PrimitiveConverterFactory.TryGetNegatedTryConvert(sourceType, targetType, defaultValue);
        }

        public IValueCopier TryGetCopier()
        {
            // No copier needed for these types
            return null;
        }

        public IXArrayComparer TryGetComparer()
        {
            if (typeof(T) == typeof(sbyte)) return new SbyteComparer();
            if (typeof(T) == typeof(byte)) return new ByteComparer();
            if (typeof(T) == typeof(short)) return new ShortComparer();
            if (typeof(T) == typeof(ushort)) return new UshortComparer();
            if (typeof(T) == typeof(int)) return new IntComparer();
            if (typeof(T) == typeof(uint)) return new UintComparer();
            if (typeof(T) == typeof(long)) return new LongComparer();
            if (typeof(T) == typeof(ulong)) return new UlongComparer();
            if (typeof(T) == typeof(float)) return new FloatComparer();
            if (typeof(T) == typeof(double)) return new DoubleComparer();

            if (typeof(T) == typeof(bool)) return new BoolComparer();

            return null;
        }

        public static string ValuesFilePath(string columnPath)
        {
            if (columnPath.EndsWith(".bin")) return columnPath;
            return Path.Combine(columnPath, $"V.{BinaryFileTypePart()}.bin");
        }

        public static string BinaryFileTypePart()
        {
            return BinaryFileTypePart(typeof(T));
        }

        public static string BinaryFileTypePart(Type type)
        {
            if (type == typeof(bool)) return "b8";

            if (type == typeof(byte)) return "u8";
            if (type == typeof(sbyte)) return "i8";
            if (type == typeof(short)) return "i16";
            if (type == typeof(ushort)) return "u16";
            if (type == typeof(int)) return "i32";
            if (type == typeof(uint)) return "u32";
            if (type == typeof(long)) return "i64";
            if (type == typeof(ulong)) return "u64";
            if (type == typeof(float)) return "f32";
            if (type == typeof(double)) return "f64";

            throw new ArgumentException($"PrimitiveTypeProvider doesn't know how to read type {type.Name}.");
        }
    }

    public class PrimitiveArrayReader<T> : IColumnReader
    {
        private const int ReadPageSize = 64 * 1024;

        private int _bytesPerItem;
        private ByteReader _byteReader;
        private T[] _array;

        private XArray _currentArray;
        private ArraySelector _currentSelector;

        public PrimitiveArrayReader(Stream stream)
        {
            _byteReader = new ByteReader(stream);
            _bytesPerItem = (typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>());
        }

        public int Count => (int)(_byteReader.Count / _bytesPerItem);

        public XArray Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            // Return the previous xarray if re-requested
            if (selector.Equals(_currentSelector)) return _currentArray;

            // Allocate the result array
            Allocator.AllocateToSize(ref _array, selector.Count);

            // Read items in pages of 64k
            int byteStart = _bytesPerItem * selector.StartIndexInclusive;
            int byteEnd = _bytesPerItem * selector.EndIndexExclusive;
            int bytesRead = 0;
            for (int currentByteIndex = byteStart; currentByteIndex < byteEnd; currentByteIndex += ReadPageSize)
            {
                int currentByteEnd = Math.Min(byteEnd, currentByteIndex + ReadPageSize);
                XArray bytexarray = _byteReader.Read(ArraySelector.All(int.MaxValue).Slice(currentByteIndex, currentByteEnd));
                Buffer.BlockCopy(bytexarray.Array, 0, _array, bytesRead, bytexarray.Count);
                bytesRead += currentByteEnd - currentByteIndex;
            }

            // Cache and return the current xarray
            _currentArray = XArray.All(_array, selector.Count);
            _currentSelector = selector;
            return _currentArray;
        }

        public void Dispose()
        {
            if (_byteReader != null)
            {
                _byteReader.Dispose();
                _byteReader = null;
            }
        }
    }

    public class PrimitiveArrayWriter<T> : IColumnWriter
    {
        private int _bytesPerItem;
        private Stream _stream;
        private byte[] _bytesBuffer;
        private long _bytesWritten;

        public PrimitiveArrayWriter(Stream stream)
        {
            _stream = stream;
            _bytesPerItem = (typeof(T) == typeof(bool) ? 1 : Marshal.SizeOf<T>());
        }

        public Type WritingAsType => typeof(T);

        public bool CanAppend(XArray xarray)
        {
            // NOTE: Don't cast XArray.Array here; writers which wrap PrimitiveArrayWriter pass XArrays with the right row count
            // but unrelated values here.
            return (_bytesWritten + (_bytesPerItem * xarray.Count)) <= BinaryTableWriter.ColumnFileSizeLimit;
        }

        public void Append(XArray array)
        {
            int bytesToWrite = _bytesPerItem * array.Count;
            Allocator.AllocateToSize(ref _bytesBuffer, bytesToWrite);

            if (array.Selector.Indices == null && array.Selector.IsSingleValue == false)
            {
                Buffer.BlockCopy(array.Array, _bytesPerItem * array.Selector.StartIndexInclusive, _bytesBuffer, 0, bytesToWrite);
            }
            else
            {
                for (int i = 0; i < array.Count; ++i)
                {
                    int index = array.Index(i);
                    Buffer.BlockCopy(array.Array, _bytesPerItem * index, _bytesBuffer, i * _bytesPerItem, _bytesPerItem);
                }
            }

            _stream.Write(_bytesBuffer, 0, bytesToWrite);
            _bytesWritten += bytesToWrite;
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
