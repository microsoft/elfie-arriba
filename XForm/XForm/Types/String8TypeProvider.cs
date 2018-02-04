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

    public struct String8Raw
    {
        public ArraySelector Selector;
        public XArray Bytes;
        public XArray Lengths;
    }

    internal class String8ColumnReader : IColumnReader
    {
        private string _columnPath;

        private IStreamProvider _streamProvider;
        private ByteReader _bytesReader;
        private IColumnReader _lengthsReader;

        private IColumnReader _positionsReader;

        private String8Raw _currentRaw;
        private XArray _currentArray;
        private ArraySelector _currentSelector;
        private String8[] _resultArray;

        private byte[] _indicesByteBuffer;

        public String8ColumnReader(IStreamProvider streamProvider, string columnPath, CachingOption option)
        {
            _columnPath = columnPath;

            _streamProvider = streamProvider;
            _bytesReader = new ByteReader(streamProvider.OpenRead(Path.Combine(columnPath, "V.s.bin"))); // (ByteReader)TypeProviderFactory.TryGetColumnReader(streamProvider, typeof(byte), Path.Combine(columnPath, "V.s.bin"), option, typeof(String8ColumnReader));
            _lengthsReader = new VariableIntegerReader(streamProvider, Path.Combine(columnPath, "Vl"), option);
            _positionsReader = TypeProviderFactory.TryGetColumnReader(streamProvider, typeof(long), Path.Combine(columnPath, $"Vp{String8ColumnWriter.WritePositionPerRowCount}.i64.bin"), option, typeof(String8ColumnReader));
        }

        public int Count => _lengthsReader.Count;

        private long FirstByteOfRowValue(int rowIndex)
        {
            // The first row starts at the beginning of the file
            if (rowIndex == 0) return 0;

            // The row after the last row is at the end of the file
            if (rowIndex == _lengthsReader.Count) return _bytesReader.Count;

            // We write only every 64th (currently) position.
            // If we want the 70th string index, we read the first position (start of row 64) and then add the lengths of rows from there to the target row to find it
            long position = 0;

            // Figure out which position we need to read and how many lengths we'll have to add to it
            int rowsAfterPosition = (rowIndex % String8ColumnWriter.WritePositionPerRowCount);
            int rowWithPosition = rowIndex - rowsAfterPosition;
            int positionIndex = (rowWithPosition / String8ColumnWriter.WritePositionPerRowCount) - 1;

            // Read the closest 'every nth row' position (start of row 64)
            if (positionIndex >= 0)
            {
                XArray positions = _positionsReader.Read(ArraySelector.All(int.MaxValue).Slice(positionIndex, positionIndex + 1));
                position = ((long[])positions.Array)[positions.Index(0)];
            }

            // Read the lengths from there to the desired row and add them (length[64] - length[69]
            if (rowsAfterPosition > 0)
            {
                XArray lengths = _lengthsReader.Read(ArraySelector.All(int.MaxValue).Slice(rowWithPosition, rowWithPosition + rowsAfterPosition - 1));
                int[] lengthsArray = (int[])lengths.Array;
                for(int i = 0; i < lengths.Count; ++i)
                {
                    position += lengthsArray[lengths.Index(i)];
                }
            }

            // We should have the start of row 70.
            return position;
        }

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
            int[] lengthsArray = (int[])_currentRaw.Lengths.Array;
            int firstStringStart = _currentRaw.Bytes.Index(0);
            int lengthOffset = _currentRaw.Lengths.Index(0);

            int previousStringEnd = firstStringStart;
            for (int i = 0; i < selector.Count; ++i)
            {
                int length = lengthsArray[lengthOffset + i];
                _resultArray[i] = new String8(textArray, previousStringEnd, length);
                previousStringEnd += length;
            }

            // Cache the xarray and return it
            _currentArray = XArray.All(_resultArray, selector.Count);
            _currentSelector = selector;
            return _currentArray;
        }

        public String8Raw ReadRaw(ArraySelector selector)
        {
            if (selector.Equals(_currentRaw.Selector)) return _currentRaw;
            _currentRaw.Selector = selector;

            long firstRowStart = FirstByteOfRowValue(selector.StartIndexInclusive);
            long lastRowEnd = FirstByteOfRowValue(selector.EndIndexExclusive);

            // Read the bytes
            _currentRaw.Bytes = _bytesReader.Read(firstRowStart, lastRowEnd - firstRowStart);
            if (_currentRaw.Bytes.Selector.Indices != null) throw new NotImplementedException("String8TypeProvider requires positions to be read contiguously.");

            // Read the string lengths
            _currentRaw.Lengths = _lengthsReader.Read(ArraySelector.All(int.MaxValue).Slice(selector.StartIndexInclusive, selector.EndIndexExclusive));
            if (_currentRaw.Lengths.Selector.Indices != null) throw new NotImplementedException("String8TypeProvider requires lengths to be read contiguously.");
            int[] lengthsArray = (int[])_currentRaw.Lengths.Array;

            return _currentRaw;
        }

        private XArray ReadIndices(ArraySelector selector)
        {
            Allocator.AllocateToSize(ref _resultArray, selector.Count);

            // Read *all* lengths
            XArray lengths = _lengthsReader.Read(ArraySelector.All(this.Count));
            int[] lengthsArray = (int[])lengths.Array;

            // Determine total string length
            int totalLength = 0;
            for (int i = 0; i < selector.Count; ++i)
            {
                totalLength += lengthsArray[selector.Index(i)];
            }

            Allocator.AllocateToSize(ref _indicesByteBuffer, totalLength);

            int lengthUsed = 0;
            for (int i = 0; i < selector.Count; ++i)
            {
                int rowIndex = selector.Index(i);
                long firstRowStart = FirstByteOfRowValue(rowIndex);
                int length = lengthsArray[lengths.Index(rowIndex)];

                _bytesReader.Read(firstRowStart, length, _indicesByteBuffer, lengthUsed);
                _resultArray[i] = new String8(_indicesByteBuffer, lengthUsed, length);

                lengthUsed += length;
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

            if(_lengthsReader != null)
            {
                _lengthsReader.Dispose();
                _lengthsReader = null;
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
        internal const int WritePositionPerRowCount = 64;

        private IStreamProvider _streamProvider;
        private Stream _bytesWriter;
        private IColumnWriter _lengthsWriter;
        private IColumnWriter _positionsWriter;

        private long _position;
        private int _rowCountWritten;

        private int[] _lengthsBuffer;
        private long[] _positionsBuffer;

        public String8ColumnWriter(IStreamProvider streamProvider, string columnPath)
        {
            _streamProvider = streamProvider;
            _bytesWriter = _streamProvider.OpenWrite(Path.Combine(columnPath, "V.s.bin"));
            _lengthsWriter = new VariableIntegerWriter(streamProvider, Path.Combine(columnPath, "Vl"));
            _positionsWriter = new PrimitiveArrayWriter<long>(streamProvider.OpenWrite(Path.Combine(columnPath, $"Vp{WritePositionPerRowCount}.i64.bin")));
        }

        public Type WritingAsType => typeof(String8);

        public void Append(XArray xarray)
        {
            Allocator.AllocateToSize(ref _lengthsBuffer, xarray.Count);
            Allocator.AllocateToSize(ref _positionsBuffer, (xarray.Count / WritePositionPerRowCount) + 1);

            int positionCount = 0;

            String8[] array = (String8[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                String8 value = array[xarray.Index(i)];
                value.WriteTo(_bytesWriter);
                _lengthsBuffer[i] = value.Length;

                // Track the absolute position and write the end of every nth value
                _position += value.Length;
                if ((_rowCountWritten + i) % WritePositionPerRowCount == WritePositionPerRowCount - 1) _positionsBuffer[positionCount++] = _position;
            }

            _lengthsWriter.Append(XArray.All(_lengthsBuffer, xarray.Count));
            if(positionCount > 0) _positionsWriter.Append(XArray.All(_positionsBuffer, positionCount));

            _rowCountWritten += xarray.Count;
        }

        public void Dispose()
        {
            if (_bytesWriter != null)
            {
                _bytesWriter.Dispose();
                _bytesWriter = null;
            }

            if(_lengthsWriter != null)
            {
                _lengthsWriter.Dispose();
                _lengthsWriter = null;
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
                if (xarray.IsNull != null && xarray.IsNull[index])
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

                if (value == null || (xarray.IsNull != null && xarray.IsNull[index]))
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
