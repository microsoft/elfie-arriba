// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Types.Comparers;


namespace XForm.Types
{
    public class String8TypeProvider : ITypeProvider
    {
        public string Name => "String8";
        public Type Type => typeof(String8);

        public IColumnReader BinaryReader(IStreamProvider streamProvider, string columnPath, CachingOption option)
        {
            // Cache the byte[] and int[], not the String8[] (much lower overhead; fast to construct String8 pages)
            return new String8ColumnReader(streamProvider, columnPath, option);
        }

        public IColumnWriter BinaryWriter(IStreamProvider streamProvider, string columnPath)
        {
            return new String8ColumnWriter(streamProvider, columnPath);
        }

        public IXArrayComparer TryGetComparer()
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
                if (sourceType == typeof(TimeSpan)) return new ToString8Converter<TimeSpan>(defaultValue, 21, String8.FromTimeSpan).Convert;

                if (sourceType == typeof(sbyte)) return new ToString8Converter<sbyte>(defaultValue, 4, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(byte)) return new ToString8Converter<byte>(defaultValue, 3, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(short)) return new ToString8Converter<short>(defaultValue, 6, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(ushort)) return new ToString8Converter<ushort>(defaultValue, 5, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(int)) return new ToString8Converter<int>(defaultValue, 11, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(uint)) return new ToString8Converter<uint>(defaultValue, 10, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(long)) return new ToString8Converter<long>(defaultValue, 21, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(ulong)) return new ToString8Converter<ulong>(defaultValue, 20, (value, buffer, index) => String8.FromNumber(value, false, buffer, index)).Convert;

                if (sourceType == typeof(float)) return new ToString8Converter<float>(defaultValue, 21, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
                if (sourceType == typeof(double)) return new ToString8Converter<double>(defaultValue, 21, (value, buffer, index) => String8.FromNumber(value, buffer, index)).Convert;
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
                else if (targetType == typeof(TimeSpan))
                {
                    // Support TimeSpan conversions from .NET Format (DDD.HH:MM:SS.mmm) and 'friendly' format (24h, 7d)
                    return new FromString8Converter<TimeSpan>(defaultValue, (String8 value, out TimeSpan result) => value.TryToTimeSpanFriendly(out result)).Convert;
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
                else if (targetType == typeof(double))
                {
                    return new FromString8Converter<double>(defaultValue, (String8 value, out double result) => value.TryToDouble(out result)).Convert;
                }
                else if (targetType == typeof(float))
                {
                    return new FromString8Converter<float>(defaultValue, (String8 value, out float result) => value.TryToFloat(out result)).Convert;
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

    public struct String8Raw
    {
        public ArraySelector Selector;
        public XArray Bytes;
        public XArray Positions;
    }

    internal class String8ColumnReader : IColumnReader
    {
        private string _columnPath;

        private IStreamProvider _streamProvider;
        private IColumnReader _bytesReader;
        private IColumnReader _positionsReader;

        private String8Raw _currentRaw;
        private XArray _currentArray;
        private ArraySelector _currentSelector;
        private String8[] _resultArray;

        public String8ColumnReader(IStreamProvider streamProvider, string columnPath, CachingOption option)
        {
            _columnPath = columnPath;

            _streamProvider = streamProvider;
            _bytesReader = TypeProviderFactory.GetColumnReader(streamProvider, typeof(byte), Path.Combine(columnPath, "V.s.bin"), option, typeof(String8ColumnReader));
            _positionsReader = TypeProviderFactory.GetColumnReader(streamProvider, typeof(int), Path.Combine(columnPath, "Vp.i32.bin"), option, typeof(String8ColumnReader));
        }

        public int Count => _positionsReader.Count;

        public XArray Read(ArraySelector selector)
        {
            if (selector.Indices != null) return ReadIndices(selector);
            if (selector.Count == 0) return XArray.All(_resultArray, 0);

            // Return previous xarray if re-requested
            if (selector.Equals(_currentSelector)) return _currentArray;

            Allocator.AllocateToSize(ref _resultArray, selector.Count);
            bool includesFirstString = (selector.StartIndexInclusive == 0);

            // Read the string positions and bytes
            ReadRaw(selector);

            // Update the String8 array to point to them
            byte[] textArray = (byte[])_currentRaw.Bytes.Array;
            int[] positionArray = (int[])_currentRaw.Positions.Array;
            int firstStringStart = (includesFirstString ? 0 : positionArray[_currentRaw.Positions.Index(0)]);
            int positionOffset = _currentRaw.Positions.Index((includesFirstString ? 0 : 1));
            int textOffset = firstStringStart - _currentRaw.Bytes.Index(0);

            int previousStringEnd = firstStringStart - textOffset;
            for (int i = 0; i < selector.Count; ++i)
            {
                int valueEnd = positionArray[i + positionOffset] - textOffset;
                _resultArray[i] = new String8(textArray, previousStringEnd, valueEnd - previousStringEnd);
                previousStringEnd = valueEnd;
            }

            // Cache the xarray and return it
            _currentArray = XArray.All(_resultArray, selector.Count);
            _currentSelector = selector;
            return _currentArray;
        }

        public String8Raw ReadRaw(ArraySelector selector)
        {
            if (selector.Equals(_currentRaw.Selector)) return _currentRaw;
            bool includesFirstString = (selector.StartIndexInclusive == 0);

            _currentRaw.Selector = selector;

            // Read the string positions
            _currentRaw.Positions = _positionsReader.Read(ArraySelector.All(Count).Slice((includesFirstString ? 0 : selector.StartIndexInclusive - 1), selector.EndIndexExclusive));
            if (_currentRaw.Positions.Selector.Indices != null) throw new NotImplementedException("String8TypeProvider requires positions to be read contiguously.");
            int[] positionArray = (int[])_currentRaw.Positions.Array;

            // Get the full byte range of all of the strings
            int firstStringStart = (includesFirstString ? 0 : positionArray[_currentRaw.Positions.Index(0)]);
            int lastStringEnd = positionArray[_currentRaw.Positions.Index(_currentRaw.Positions.Count - 1)];

            // Read the raw string bytes
            _currentRaw.Bytes = _bytesReader.Read(ArraySelector.All(int.MaxValue).Slice(firstStringStart, lastStringEnd));
            if (_currentRaw.Bytes.Selector.Indices != null) throw new NotImplementedException("String8TypeProvider requires positions to be read contiguously.");

            return _currentRaw;
        }

        private XArray ReadIndices(ArraySelector selector)
        {
            Allocator.AllocateToSize(ref _resultArray, selector.Count);

            // Read all string positions
            XArray positions = _positionsReader.Read(ArraySelector.All(_positionsReader.Count));
            int[] positionArray = (int[])positions.Array;

            // Read all raw string bytes
            XArray bytes = _bytesReader.Read(ArraySelector.All(_bytesReader.Count));
            byte[] textArray = (byte[])bytes.Array;

            // Update the String8 array to point to them
            for (int i = 0; i < selector.Count; ++i)
            {
                int rowIndex = selector.Index(i);
                int valueStart = (rowIndex == 0 ? 0 : positionArray[rowIndex - 1]);
                int valueEnd = positionArray[rowIndex];
                _resultArray[i] = new String8(textArray, valueStart, valueEnd - valueStart);
            }

            // Cache the xarray and return it
            _currentArray = XArray.All(_resultArray, selector.Count);
            _currentSelector = selector;
            return _currentArray;
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

        public Type WritingAsType => typeof(String8);

        public bool CanAppend(XArray xarray)
        {
            if (!_positionsWriter.CanAppend(xarray)) return false;

            long bytesThisTime = 0;
            String8[] array = (String8[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                String8 value = array[xarray.Index(i)];
                bytesThisTime += value.Length;
            }

            return (_position + bytesThisTime) <= BinaryTableWriter.ColumnFileSizeLimit;
        }

        public void Append(XArray xarray)
        {
            Allocator.AllocateToSize(ref _positionsBuffer, xarray.Count);

            String8[] array = (String8[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                String8 value = array[xarray.Index(i)];
                value.WriteTo(_bytesWriter);
                _position += value.Length;
                _positionsBuffer[i] = _position;
            }

            _positionsWriter.Append(XArray.All(_positionsBuffer, xarray.Count));
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

        public bool[] Convert(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _string8Array, xarray.Count);
            Allocator.AllocateToSize(ref _buffer, xarray.Count * _bytesPerItem);

            int bufferBytesUsed = 0;
            T[] sourceArray = (T[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                int index = xarray.Index(i);
                if (xarray.HasNulls && xarray.NullRows[index])
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

        public bool[] StringToString8(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _string8Array, xarray.Count);
            Allocator.AllocateToSize(ref _couldNotConvertArray, xarray.Count);

            if (_block == null) _block = new String8Block();
            _block.Clear();

            bool anyCouldNotConvert = false;
            string[] sourceArray = (string[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                int index = xarray.Index(i);
                string value = sourceArray[index];

                if (value == null || (xarray.HasNulls && xarray.NullRows[index]))
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

        public bool[] Convert(XArray xarray, out Array result)
        {
            Allocator.AllocateToSize(ref _array, (xarray.Selector.IsSingleValue ? 1 : xarray.Count));
            Allocator.AllocateToSize(ref _couldNotConvertArray, (xarray.Selector.IsSingleValue ? 1 : xarray.Count));

            bool anyCouldNotConvert = false;
            String8[] sourceArray = (String8[])xarray.Array;

            if (!xarray.Selector.IsSingleValue)
            {
                for (int i = 0; i < xarray.Count; ++i)
                {
                    _couldNotConvertArray[i] = !_tryConvert(sourceArray[xarray.Index(i)], out _array[i]);

                    if (_couldNotConvertArray[i])
                    {
                        _array[i] = _defaultValue;
                        anyCouldNotConvert = true;
                    }
                }
            }
            else
            {
                _couldNotConvertArray[0] = !_tryConvert(sourceArray[xarray.Index(0)], out _array[0]);
                if (_couldNotConvertArray[0])
                {
                    _array[0] = _defaultValue;
                    anyCouldNotConvert = true;
                }
            }

            result = _array;
            return (anyCouldNotConvert ? _couldNotConvertArray : null);
        }
    }
}
