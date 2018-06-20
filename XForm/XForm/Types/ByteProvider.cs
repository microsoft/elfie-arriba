// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class ByteTypeProvider : ITypeProvider
    {
        public string Name => typeof(byte).Name;
        public Type Type => typeof(byte);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath, CachingOption option)
        {
            // Cache direct byte[] columns
            return ColumnCache.Instance.GetOrBuild(columnPath, option, () =>
            {
                string filePath = ValuesFilePath(columnPath);
                if (!streamProvider.UncachedExists(filePath)) return null;
                return new ByteReader(streamProvider.OpenRead(filePath));
            });
        }

        private static string ValuesFilePath(string columnPath)
        {
            if (columnPath.EndsWith(".bin")) return columnPath;
            return Path.Combine(columnPath, $"V.u8.bin");
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new ByteWriter(streamProvider.OpenWrite(ValuesFilePath(columnPath)));
        }

        public IXArrayComparer TryGetComparer()
        {
            return new ByteComparer();
        }

        public NegatedTryConvert TryGetNegatedTryConvert(Type sourceType, Type targetType, object defaultValue)
        {
            // TODO: Add primitive number conversions
            return null;
        }

        public IValueCopier TryGetCopier()
        {
            // No copier needed for this type
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
        public long Length => _stream.Length;

        public XArray Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            Allocator.AllocateToSize(ref _array, selector.Count);

            _stream.Seek(selector.StartIndexInclusive, SeekOrigin.Begin);
            _stream.Read(_array, 0, selector.Count);

            return XArray.All(_array, selector.Count);
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
        private long _bytesWritten;

        public ByteWriter(Stream stream)
        {
            _stream = stream;
        }

        public Type WritingAsType => typeof(byte);

        public bool CanAppend(XArray xarray)
        {
            return (_bytesWritten + xarray.Count) <= BinaryTableWriter.ColumnFileSizeLimit;
        }

        public void Append(XArray xarray)
        {
            byte[] arrayToWrite = (byte[])xarray.Array;

            if (xarray.Selector.Indices != null)
            {
                Allocator.AllocateToSize(ref _buffer, xarray.Count);

                for (int i = 0; i < xarray.Count; ++i)
                {
                    int index = xarray.Index(i);
                    _buffer[i] = arrayToWrite[index];
                }

                arrayToWrite = _buffer;
            }

            _stream.Write((byte[])xarray.Array, xarray.Selector.StartIndexInclusive, xarray.Count);
            _bytesWritten += xarray.Count;
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
