// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Query;
using XForm.Transforms;
using XForm.Types.Comparers;

namespace XForm.Types
{
    public class String8TypeProvider : ITypeProvider
    {
        public string Name => "String8";
        public Type Type => typeof(String8);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath)
        {
            return new String8ColumnReader(streamProvider, columnPath);
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new String8ColumnWriter(streamProvider, columnPath);
        }

        public ComparerExtensions.Comparer TryGetComparer(CompareOperator op)
        {
            return new ComparableComparer<String8>().TryBuild(op);
        }

        public Func<DataBatch, DataBatch> TryGetConverter(Type sourceType, Type targetType, object defaultValue, bool strict)
        {
            // TODO: Need to handle strict

            // Build a converter for the set of types
            if (targetType == typeof(String8))
            {
                ToString8Converter converter = new ToString8Converter();
                if (sourceType == typeof(string)) return converter.StringToString8;
                if (sourceType == typeof(int)) return (batch) => converter.Convert<int>(batch, (value, buffer, index) => String8.FromInteger(value, buffer, index), 12);
                if (sourceType == typeof(DateTime)) return (batch) => converter.Convert<DateTime>(batch, String8.FromDateTime, 20);
                if (sourceType == typeof(bool)) return (batch) => converter.Convert<bool>(batch, (value, buffer, index) => String8.FromBoolean(value), 0);
            }
            else if (sourceType == typeof(String8))
            {
                if (targetType == typeof(int))
                {
                    return new FromString8Converter<int>((String8 value, out int result) => value.TryToInteger(out result)).Convert;
                }
                else if (targetType == typeof(uint))
                {
                    return new FromString8Converter<uint>((String8 value, out uint result) => value.TryToUInt(out result)).Convert;
                }
                else if (targetType == typeof(DateTime))
                {
                    return new FromString8Converter<DateTime>((String8 value, out DateTime result) => value.TryToDateTime(out result)).Convert;
                }
                else if (targetType == typeof(bool))
                {
                    return new FromString8Converter<bool>((String8 value, out bool result) => value.TryToBoolean(out result)).Convert;
                }
                else if (targetType == typeof(long))
                {
                    return new FromString8Converter<long>((String8 value, out long result) => value.TryToLong(out result)).Convert;
                }
                else if (targetType == typeof(ulong))
                {
                    return new FromString8Converter<ulong>((String8 value, out ulong result) => value.TryToULong(out result)).Convert;
                }
                else if (targetType == typeof(ushort))
                {
                    return new FromString8Converter<ushort>((String8 value, out ushort result) => value.TryToUShort(out result)).Convert;
                }
                else if (targetType == typeof(short))
                {
                    return new FromString8Converter<short>((String8 value, out short result) => value.TryToShort(out result)).Convert;
                }
                else if (targetType == typeof(byte))
                {
                    return new FromString8Converter<byte>((String8 value, out byte result) => value.TryToByte(out result)).Convert;
                }
                else if (targetType == typeof(sbyte))
                {
                    return new FromString8Converter<sbyte>((String8 value, out sbyte result) => value.TryToSByte(out result)).Convert;
                }
            }

            return null;
        }
    }

    internal class String8ColumnReader : IColumnReader
    {
        private IStreamProvider _streamProvider;
        private ByteReader _bytesReader;
        private PrimitiveArrayReader<int> _positionsReader;

        private String8[] _resultArray;

        public String8ColumnReader(IStreamProvider streamProvider, string columnPath)
        {
            _streamProvider = streamProvider;
            _bytesReader = new ByteReader(streamProvider.OpenRead(Path.Combine(columnPath, "V.s.bin")));
            _positionsReader = new PrimitiveArrayReader<int>(streamProvider.OpenRead(Path.Combine(columnPath, "Vp.i32.bin")));
        }

        public int Count => _positionsReader.Count;

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();
            if (selector.Count == 0) return DataBatch.All(_resultArray, 0);

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
        private IStreamProvider _streamProvider;
        private Stream _bytesWriter;
        private PrimitiveArrayWriter<int> _positionsWriter;

        private int[] _positionsBuffer;
        private int _position;

        public String8ColumnWriter(IStreamProvider streamProvider, string columnPath)
        {
            _streamProvider = streamProvider;
            _bytesWriter = _streamProvider.OpenWrite(Path.Combine(columnPath, "V.s.bin"));
            _positionsWriter = new PrimitiveArrayWriter<int>(streamProvider.OpenWrite(Path.Combine(columnPath, "Vp.i32.bin")));
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
        private bool[] _nullArray;
        private String8[] _stringArray;
        private byte[] _buffer;
        private String8Block _block;

        public DataBatch Convert<T>(DataBatch batch, Func<T, byte[], int, String8> converter, int bytesPerItem)
        {
            Allocator.AllocateToSize(ref _nullArray, batch.Count);
            Allocator.AllocateToSize(ref _stringArray, batch.Count);
            Allocator.AllocateToSize(ref _buffer, batch.Count * bytesPerItem);

            bool hasAnyNulls = false;
            int bufferBytesUsed = 0;
            T[] sourceArray = (T[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                if (batch.IsNull != null && batch.IsNull[index])
                {
                    _stringArray[i] = String8.Empty;
                    _nullArray[i] = true;
                    hasAnyNulls = true;
                }
                else
                {
                    _stringArray[i] = converter(sourceArray[index], _buffer, bufferBytesUsed);
                    bufferBytesUsed += _stringArray[i].Length;
                    _nullArray[i] = false;
                }
            }

            return DataBatch.All(_stringArray, batch.Count, (hasAnyNulls ? _nullArray : null));
        }

        public DataBatch StringToString8(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _nullArray, batch.Count);
            Allocator.AllocateToSize(ref _stringArray, batch.Count);

            if (_block == null)
            {
                _block = new String8Block();
            }
            else
            {
                _block.Clear();
            }

            bool hasAnyNulls = false;
            string[] sourceArray = (string[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                string value = sourceArray[index];

                if (value == null || batch.IsNull != null && batch.IsNull[index])
                {
                    _stringArray[i] = String8.Empty;
                    _nullArray[i] = true;
                }
                else
                {
                    _stringArray[i] = _block.GetCopy(sourceArray[batch.Index(i)]);
                    _nullArray[i] = false;
                }
            }

            return DataBatch.All(_stringArray, batch.Count, (hasAnyNulls ? _nullArray : null));
        }
    }

    internal class FromString8Converter<T>
    {
        public delegate bool TryConvert(String8 value, out T result);

        private bool[] _nullArray;
        private T[] _array;
        private TryConvert _converter;

        public FromString8Converter(TryConvert converter)
        {
            _converter = converter;
        }

        public DataBatch Convert(DataBatch batch)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _nullArray, batch.Count);

            bool areAnyNull = false;
            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _nullArray[i] = !_converter(sourceArray[batch.Index(i)], out _array[i]);
                areAnyNull |= _nullArray[i];
            }

            return DataBatch.All(_array, batch.Count, (areAnyNull ? _nullArray : null));
        }
    }
}
