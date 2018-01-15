// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.IO.StreamProvider;
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

        public IDataBatchComparer TryGetComparer()
        {
            // String8Comparer is generated
            return new String8Comparer();
        }

        public IValueCopier TryGetCopier()
        {
            return new String8Copier();
        }

        public NegatedTryConvert TryGetNegatedTryConvert(Type sourceType, Type targetType, object defaultValue)
        {
            // Build a converter for the set of types
            if (targetType == typeof(String8))
            {
                if (sourceType == typeof(string)) return new StringToString8Converter(defaultValue).StringToString8;
                if (sourceType == typeof(DateTime)) return new ToString8Converter<DateTime>(defaultValue, 20, String8.FromDateTime).Convert;
                if (sourceType == typeof(bool)) return new ToString8Converter<bool>(defaultValue, 0, (value, buffer, index) => String8.FromBoolean(value)).Convert;

                if (sourceType == typeof(sbyte)) return new ToString8Converter<sbyte>(defaultValue, 4, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(byte)) return new ToString8Converter<byte>(defaultValue, 3, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(short)) return new ToString8Converter<short>(defaultValue, 6, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(ushort)) return new ToString8Converter<ushort>(defaultValue, 5, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(int)) return new ToString8Converter<int>(defaultValue, 11, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(uint)) return new ToString8Converter<uint>(defaultValue, 10, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(long)) return new ToString8Converter<long>(defaultValue, 21, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(ulong)) return new ToString8Converter<ulong>(defaultValue, 20, (value, buffer, index) => String8.FromNumber(value, false, buffer, index)).Convert;
            }
            else if (sourceType == typeof(String8))
            {
                if (targetType == typeof(int))
                {
                    return new FromString8Converter<int>(defaultValue, (String8 value, out int result) => value.TryToInteger(out result)).Convert;
                }
                else if (targetType == typeof(uint))
                {
                    return new FromString8Converter<uint>(defaultValue, (String8 value, out uint result) => value.TryToUInt(out result)).Convert;
                }
                else if (targetType == typeof(DateTime))
                {
                    return new FromString8Converter<DateTime>(defaultValue, (String8 value, out DateTime result) => value.TryToDateTime(out result)).Convert;
                }
                else if (targetType == typeof(bool))
                {
                    return new FromString8Converter<bool>(defaultValue, (String8 value, out bool result) => value.TryToBoolean(out result)).Convert;
                }
                else if (targetType == typeof(long))
                {
                    return new FromString8Converter<long>(defaultValue, (String8 value, out long result) => value.TryToLong(out result)).Convert;
                }
                else if (targetType == typeof(ulong))
                {
                    return new FromString8Converter<ulong>(defaultValue, (String8 value, out ulong result) => value.TryToULong(out result)).Convert;
                }
                else if (targetType == typeof(ushort))
                {
                    return new FromString8Converter<ushort>(defaultValue, (String8 value, out ushort result) => value.TryToUShort(out result)).Convert;
                }
                else if (targetType == typeof(short))
                {
                    return new FromString8Converter<short>(defaultValue, (String8 value, out short result) => value.TryToShort(out result)).Convert;
                }
                else if (targetType == typeof(byte))
                {
                    return new FromString8Converter<byte>(defaultValue, (String8 value, out byte result) => value.TryToByte(out result)).Convert;
                }
                else if (targetType == typeof(sbyte))
                {
                    return new FromString8Converter<sbyte>(defaultValue, (String8 value, out sbyte result) => value.TryToSByte(out result)).Convert;
                }
            }

            return null;
        }
    }

    public class String8Copier : IValueCopier<String8>
    {
        private String8Block _block;

        public String8Copier()
        {
            _block = new String8Block();
        }

        public String8 Copy(String8 value)
        {
            return _block.GetCopy(value);
        }

        public void Reset()
        {
            _block.Clear();
        }
    }

    internal class String8ColumnReader : IColumnReader
    {
        private IStreamProvider _streamProvider;
        private IColumnReader _bytesReader;
        private IColumnReader _positionsReader;

        private DataBatch _currentBatch;
        private ArraySelector _currentSelector;

        private String8[] _resultArray;

        public String8ColumnReader(IStreamProvider streamProvider, string columnPath)
        {
            _streamProvider = streamProvider;
            _bytesReader = TypeProviderFactory.Get(typeof(byte)).BinaryReader(streamProvider, Path.Combine(columnPath, "V.s.bin"));
            _positionsReader = TypeProviderFactory.Get(typeof(int)).BinaryReader(streamProvider, Path.Combine(columnPath, "Vp.i32.bin"));
        }

        public int Count => _positionsReader.Count;

        public DataBatch Read(ArraySelector selector)
        {
            if (selector.Indices != null) throw new NotImplementedException();
            if (selector.Count == 0) return DataBatch.All(_resultArray, 0);

            // Return previous batch if re-requested
            if (selector.Equals(_currentSelector)) return _currentBatch;

            // Read the end of the previous string
            int start;
            if (selector.StartIndexInclusive == 0)
            {
                start = 0;
            }
            else
            {
                DataBatch first = _positionsReader.Read(ArraySelector.All(Count).Slice(selector.StartIndexInclusive - 1, selector.StartIndexInclusive));
                start = ((int[])first.Array)[first.Index(0)];
            }

            // Read the ends of this batch
            DataBatch positionBatch = _positionsReader.Read(selector);
            int[] positions = (int[])positionBatch.Array;
            int end = positions[positionBatch.Index(selector.Count - 1)];
            int lengthToRead = end - start;

            Allocator.AllocateToSize(ref _resultArray, selector.Count);

            // Read the raw string bytes
            DataBatch textBytes = _bytesReader.Read(ArraySelector.All(int.MaxValue).Slice(start, end));
            byte[] textArray = (byte[])textBytes.Array;

            // Update the String8 array to point to them
            int previousStringEnd = textBytes.Index(0);
            for (int i = 0; i < selector.Count; ++i)
            {
                int valueEnd = positions[positionBatch.Index(i)] - start;
                _resultArray[i] = new String8(textArray, previousStringEnd, valueEnd - previousStringEnd);
                previousStringEnd = valueEnd;
            }

            // Cache the batch and return it
            _currentBatch = DataBatch.All(_resultArray, selector.Count);
            _currentSelector = selector;
            return _currentBatch;
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

    internal class ToString8Converter<T>
    {
        private String8 _defaultValue;
        private Func<T, byte[], int, String8> _converter;
        private int _bytesPerItem;

        private byte[] _buffer;
        private String8[] _string8Array;

        public ToString8Converter(object defaultValue, int bytesPerItem, Func<T, byte[], int, String8> converter)
        {
            if (defaultValue == null)
            {
                _defaultValue = String8.Empty;
            }
            else if (defaultValue is String8)
            {
                _defaultValue = (String8)defaultValue;
            }
            else
            {
                string defaultAsString = defaultValue.ToString();
                _defaultValue = String8.Convert(defaultAsString, new byte[String8.GetLength(defaultAsString)]);
            }

            _converter = converter;
            _bytesPerItem = bytesPerItem;
        }

        public bool[] Convert(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _string8Array, batch.Count);
            Allocator.AllocateToSize(ref _buffer, batch.Count * _bytesPerItem);

            int bufferBytesUsed = 0;
            T[] sourceArray = (T[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                if (batch.IsNull != null && batch.IsNull[index])
                {
                    // Always turn nulls into the default value rather than converting default of other type
                    _string8Array[i] = _defaultValue;
                }
                else
                {
                    String8 converted = _converter(sourceArray[index], _buffer, bufferBytesUsed);
                    _string8Array[i] = converted;
                    bufferBytesUsed += converted.Length;
                }
            }

            result = _string8Array;
            return null;
        }
    }

    internal class StringToString8Converter
    {
        private String8 _defaultValue;

        private String8Block _block;
        private String8[] _string8Array;
        private bool[] _couldNotConvertArray;

        public StringToString8Converter(object defaultValue)
        {
            if (defaultValue == null)
            {
                _defaultValue = String8.Empty;
            }
            else if (defaultValue is String8)
            {
                _defaultValue = (String8)defaultValue;
            }
            else
            {
                string defaultAsString = defaultValue.ToString();
                _defaultValue = String8.Convert(defaultAsString, new byte[String8.GetLength(defaultAsString)]);
            }
        }

        public bool[] StringToString8(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _string8Array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvertArray, batch.Count);

            if (_block == null) _block = new String8Block();
            _block.Clear();

            bool anyCouldNotConvert = false;
            string[] sourceArray = (string[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                int index = batch.Index(i);
                string value = sourceArray[index];

                if (value == null || (batch.IsNull != null && batch.IsNull[index]))
                {
                    // Always turn nulls into the default value rather than converting string default
                    _string8Array[i] = _defaultValue;
                    _couldNotConvertArray[i] = true;
                    anyCouldNotConvert = true;
                }
                else
                {
                    _string8Array[i] = _block.GetCopy(value);
                    _couldNotConvertArray[i] = false;
                }
            }

            result = _string8Array;
            return (anyCouldNotConvert ? _couldNotConvertArray : null);
        }
    }

    internal class FromString8Converter<T>
    {
        public delegate bool TryConvert(String8 value, out T result);
        private TryConvert _tryConvert;

        private T _defaultValue;

        private T[] _array;
        private bool[] _couldNotConvertArray;

        public FromString8Converter(object defaultValue, TryConvert tryConvert)
        {
            _tryConvert = tryConvert;
            _defaultValue = (T)(TypeConverterFactory.ConvertSingle(defaultValue, typeof(T)) ?? default(T));
        }

        public bool[] Convert(DataBatch batch, out Array result)
        {
            Allocator.AllocateToSize(ref _array, batch.Count);
            Allocator.AllocateToSize(ref _couldNotConvertArray, batch.Count);

            bool anyCouldNotConvert = false;
            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                _couldNotConvertArray[i] = !_tryConvert(sourceArray[batch.Index(i)], out _array[i]);

                if (_couldNotConvertArray[i])
                {
                    _array[i] = _defaultValue;
                    anyCouldNotConvert = true;
                }
            }

            result = _array;
            return (anyCouldNotConvert ? _couldNotConvertArray : null);
        }
    }
}
