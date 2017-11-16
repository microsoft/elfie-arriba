// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Types
{
    public class String8TypeProvider : ITypeProvider
    {
        public string Name => "String8";

        public Type Type => typeof(String8);

        public IColumnReader BinaryReader(string columnPath)
        {
            return new String8ColumnReader(columnPath);
        }

        public IColumnWriter BinaryWriter(string columnPath)
        {
            return new String8ColumnWriter(columnPath);
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            // Build a converter for the set of types
            if (targetType == typeof(String8))
            {
                ToString8Converter converter = new ToString8Converter();
                if (sourceType == typeof(string)) return converter.StringToString8;
                if (sourceType == typeof(int)) return converter.IntegerToString8;
                if (sourceType == typeof(DateTime)) return converter.DateTimeToString8;
                if (sourceType == typeof(bool)) return converter.BooleanToString8;
            }
            else if (sourceType == typeof(String8))
            {
                if (targetType == typeof(int))
                {
                    var converter = new String8ToIntegerConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
                else if (targetType == typeof(DateTime))
                {
                    var converter = new String8ToDateTimeConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
                else if (targetType == typeof(bool))
                {
                    var converter = new String8ToBooleanConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
                else if (targetType == typeof(long))
                {
                    var converter = new String8ToLongConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
                else if (targetType == typeof(ulong))
                {
                    var converter = new String8ToULongConverter(defaultValue);
                    if (strict) return converter.ConvertOrThrow;
                    return converter.ConvertOrDefault;
                }
            }

            return null;
        }
    }

    internal class String8ColumnReader : IColumnReader
    {
        private ByteReader _bytesReader;
        private PrimitiveArrayReader<int> _positionsReader;

        private String8[] _resultArray;

        public String8ColumnReader(string columnPath)
        {
            _bytesReader = new ByteReader(new FileStream(Path.Combine(columnPath, "V.s.bin"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            _positionsReader = new PrimitiveArrayReader<int>(new FileStream(Path.Combine(columnPath, "Vp.i32.bin"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public int Count => _positionsReader.Count;

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();

            // Read the end of the previous string
            int start;
            if (selector.StartIndexInclusive == 0)
            {
                start = 0;
            }
            else
            {
                start = ((int[])_positionsReader.Read(ArraySelector.All(Count).Slice(selector.StartIndexInclusive - 1, selector.StartIndexInclusive)).Array)[0];
            }

            // Read the ends of this batch
            int[] positions = (int[])_positionsReader.Read(selector).Array;
            int end = positions[selector.Count - 1];
            int lengthToRead = end - start;

            Allocator.AllocateToSize(ref _resultArray, selector.Count);

            // Read the raw string bytes
            DataBatch textBytes = _bytesReader.Read(ArraySelector.All(int.MaxValue).Slice(start, end));
            byte[] textArray = (byte[])textBytes.Array;

            // Update the String8 array to point to them
            int previousStringEnd = start;
            for (int i = 0; i < selector.Count; ++i)
            {
                int valueEnd = positions[i];
                _resultArray[i] = new String8(textArray, previousStringEnd - start, valueEnd - previousStringEnd);
                previousStringEnd = valueEnd;
            }

            return DataBatch.All(_resultArray, selector.Count);
        }

        public void Dispose()
        {
            if (_bytesReader != null)
            {
                _bytesReader.Dispose();
                _bytesReader = null;
            }

            if (_positionsReader != null)
            {
                _positionsReader.Dispose();
                _positionsReader = null;
            }
        }
    }

    internal class String8ColumnWriter : IColumnWriter
    {
        private FileStream _bytesWriter;
        private PrimitiveArrayWriter<int> _positionsWriter;

        private int[] _positionsBuffer;
        private int _position;

        public String8ColumnWriter(string columnPath)
        {
            Directory.CreateDirectory(columnPath);
            _bytesWriter = new FileStream(Path.Combine(columnPath, "V.s.bin"), FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete);
            _positionsWriter = new PrimitiveArrayWriter<int>(new FileStream(Path.Combine(columnPath, "Vp.i32.bin"), FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete));
        }

        public void Append(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _positionsBuffer, batch.Count);

            String8[] array = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                String8 value = array[batch.Index(i)];
                value.WriteTo(_bytesWriter);
                _position += value.Length;
                _positionsBuffer[i] = _position;
            }

            _positionsWriter.Append(DataBatch.All(_positionsBuffer, batch.Count));
        }

        public void Dispose()
        {
            if (_bytesWriter != null)
            {
                _bytesWriter.Dispose();
                _bytesWriter = null;
            }

            if (_positionsWriter != null)
            {
                _positionsWriter.Dispose();
                _positionsWriter = null;
            }
        }
    }

    internal class ToString8Converter
    {
        private String8[] _stringArray;
        private byte[] _buffer;
        private String8Block _block;

        private void ClearAndSize(int length, int bytesPerItem)
        {
            Allocator.AllocateToSize(ref _stringArray, length);
            Allocator.AllocateToSize(ref _buffer, length * bytesPerItem);
        }

        public DataBatch IntegerToString8(DataBatch batch)
        {
            ClearAndSize(batch.Count, 12);
            int bufferBytesUsed = 0;

            int[] sourceArray = (int[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _stringArray[i] = String8.FromInteger(sourceArray[batch.Index(i)], _buffer, bufferBytesUsed);
                bufferBytesUsed += _stringArray[i].Length;
            }

            return DataBatch.All(_stringArray, batch.Count);
        }

        public DataBatch DateTimeToString8(DataBatch batch)
        {
            ClearAndSize(batch.Count, 20);
            int bufferBytesUsed = 0;

            DateTime[] sourceArray = (DateTime[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _stringArray[i] = String8.FromDateTime(sourceArray[batch.Index(i)], _buffer, bufferBytesUsed);
                bufferBytesUsed += _stringArray[i].Length;
            }

            return DataBatch.All(_stringArray, batch.Count);
        }

        public DataBatch BooleanToString8(DataBatch batch)
        {
            ClearAndSize(batch.Count, 0);

            bool[] sourceArray = (bool[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _stringArray[i] = String8.FromBoolean(sourceArray[batch.Index(i)]);
            }

            return DataBatch.All(_stringArray, batch.Count);
        }

        public DataBatch StringToString8(DataBatch batch)
        {
            ClearAndSize(batch.Count, 0);
            if (_block == null)
            {
                _block = new String8Block();
            }
            else
            {
                _block.Clear();
            }

            string[] sourceArray = (string[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _stringArray[i] = _block.GetCopy(sourceArray[batch.Index(i)]);
            }

            return DataBatch.All(_stringArray, batch.Count);
        }
    }

    internal class String8ToIntegerConverter
    {
        private int[] _array;
        private int _default;

        public String8ToIntegerConverter(object defaultValue)
        {
            _default = (int)(defaultValue ?? default(int));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToInteger(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToInteger(out _array[i])) throw new InvalidCastException($"Could not convert {sourceArray[batch.Index(i)]} to integer.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }

    internal class String8ToDateTimeConverter
    {
        private DateTime[] _array;
        private DateTime _default;

        public String8ToDateTimeConverter(object defaultValue)
        {
            _default = (DateTime)(defaultValue ?? default(DateTime));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToDateTime(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToDateTime(out _array[i])) throw new InvalidCastException($"Could not convert {sourceArray[batch.Index(i)]} to DateTime.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }

    internal class String8ToBooleanConverter
    {
        private bool[] _array;
        private bool _default;

        public String8ToBooleanConverter(object defaultValue)
        {
            _default = (bool)(defaultValue ?? default(bool));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToBoolean(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToBoolean(out _array[i])) throw new InvalidCastException($"Could not convert {sourceArray[batch.Index(i)]} to boolean.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }

    internal class String8ToLongConverter
    {
        private long[] _array;
        private long _default;

        public String8ToLongConverter(object defaultValue)
        {
            _default = (long)(defaultValue ?? default(long));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToLong(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToLong(out _array[i])) throw new InvalidCastException($"Could not convert {sourceArray[batch.Index(i)]} to Long.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }

    internal class String8ToULongConverter
    {
        private ulong[] _array;
        private ulong _default;

        public String8ToULongConverter(object defaultValue)
        {
            _default = (ulong)(defaultValue ?? default(ulong));
        }

        public DataBatch ConvertOrDefault(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToULong(out _array[i])) _array[i] = _default;
            }

            return DataBatch.All(_array, batch.Count);
        }

        public DataBatch ConvertOrThrow(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                if (!sourceArray[batch.Index(i)].TryToULong(out _array[i])) throw new InvalidCastException($"Could not convert {sourceArray[batch.Index(i)]} to ULong.");
            }

            return DataBatch.All(_array, batch.Count);
        }
    }
}
