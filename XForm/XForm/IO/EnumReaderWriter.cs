// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Types;

namespace XForm.IO
{
    public interface IEnumColumnDictionary
    {
        bool Add(XArray values, ref byte[] valueIndices);
        XArray Values();
    }

    // Sort values and remap on Dispose?
    public class EnumColumnDictionary<T> : HashCore, IEnumColumnDictionary
    {
        private IEqualityComparer<T> _comparer;
        private IValueCopier<T> _copier;
        private T[] _keys;
        private byte[] _values;
        private int _nullItemIndex;

        private bool _currentIsNull;
        private T _currentKey;
        private byte _currentValue;

        public EnumColumnDictionary()
        {
            ITypeProvider typeProvider = TypeProviderFactory.Get(typeof(T));
            _comparer = new EqualityComparerAdapter<T>((IXArrayComparer<T>)typeProvider.TryGetComparer());
            _copier = (IValueCopier<T>)typeProvider.TryGetCopier();
            Reset(HashCore.SizeForCapacity(256));
        }

        protected override void Reset(int size)
        {
            base.Reset(size);
            _keys = new T[size];
            _values = new byte[size];
            _nullItemIndex = -1;
        }

        private uint HashCurrent()
        {
            return (_currentIsNull ? 0 : unchecked((uint)_comparer.GetHashCode(_currentKey)));
        }

        protected override bool EqualsCurrent(uint index)
        {
            if (_currentIsNull) return _nullItemIndex == index;
            return _comparer.Equals(_keys[index], _currentKey);
        }

        protected override void Expand()
        {
            // Save the current Keys/Values/Metadata
            T[] oldKeys = _keys;
            byte[] oldValues = _values;
            byte[] oldMetaData = this.Metadata;
            int oldNullIndex = _nullItemIndex;

            // Expand the table
            Reset(HashCore.ResizeToSize(_keys.Length));

            // Add items to the enlarged table
            for (int i = 0; i < oldMetaData.Length; ++i)
            {
                if (oldMetaData[i] > 0) Add(oldKeys[i], oldValues[i], i == oldNullIndex);
            }
        }

        private byte Add(T key, bool isNull)
        {
            byte valueIfAdded = (byte)this.Count;
            _currentKey = key;
            _currentIsNull = isNull;
            _currentValue = valueIfAdded;

            uint hash = HashCurrent();
            int index = this.IndexOf(hash);
            if (index != -1) return _values[index];

            if (!this.Add(hash))
            {
                Expand();
                _currentKey = key;
                _currentIsNull = isNull;
                _currentValue = valueIfAdded;
                this.Add(hash);
            }

            return valueIfAdded;
        }

        private void Add(T key, byte value, bool isNull)
        {
            _currentKey = key;
            _currentIsNull = isNull;
            _currentValue = value;
            uint hash = HashCurrent();

            if (!this.Add(hash))
            {
                Expand();

                _currentKey = key;
                _currentIsNull = isNull;
                _currentValue = value;
                this.Add(hash);
            }
        }

        protected override void SwapWithCurrent(uint index, SwapType swapType)
        {
            if (swapType == SwapType.Match) return;
            if (swapType == SwapType.Insert && _copier != null) _currentKey = _copier.Copy(_currentKey);
            if (swapType == SwapType.Insert && _currentIsNull)
            {
                _nullItemIndex = (int)index;
            }

            T swapKey = _keys[index];
            byte swapValue = _values[index];

            _keys[index] = _currentKey;
            _values[index] = _currentValue;

            _currentKey = swapKey;
            _currentValue = swapValue;
        }

        public bool Add(XArray xarray, ref byte[] indicesFound)
        {
            Allocator.AllocateToSize(ref indicesFound, xarray.Count);

            T[] array = (T[])xarray.Array;
            for (int i = 0; i < xarray.Count; ++i)
            {
                int index = xarray.Index(i);
                bool isNull = (xarray.IsNull != null && xarray.IsNull[index]);
                T value = array[index];

                indicesFound[i] = Add(value, isNull);
                if (this.Count > 256) break;
            }

            return this.Count <= 256;
        }

        public XArray Values()
        {
            bool[] isNull = null;
            if (_nullItemIndex != -1)
            {
                isNull = new bool[this.Metadata.Length];
                isNull[_nullItemIndex] = true;
            }

            int[] indicesInOrder = new int[this.Count];
            for (int i = 0; i < this.Metadata.Length; ++i)
            {
                if (this.Metadata[i] != 0)
                {
                    indicesInOrder[_values[i]] = i;
                }
            }

            return XArray.All(_keys, this.Count, isNull).Reselect(ArraySelector.Map(indicesInOrder, this.Count));
        }
    }

    public class EnumWriter : IColumnWriter
    {
        public const string RowIndexFileName = "VR.u8.bin";

        private IStreamProvider _streamProvider;
        private string _columnPath;

        private IEnumColumnDictionary _dictionary;

        private IColumnWriter _valueWriter;
        private IColumnWriter _rowIndexWriter;

        private byte[] _currentArrayIndices;

        public EnumWriter(IStreamProvider streamProvider, string columnPath, Type columnType, IColumnWriter valueWriter)
        {
            _streamProvider = streamProvider;
            _columnPath = columnPath;
            _valueWriter = valueWriter;

            _dictionary = (IEnumColumnDictionary)Allocator.ConstructGenericOf(typeof(EnumColumnDictionary<>), columnType);
            _rowIndexWriter = new PrimitiveArrayWriter<byte>(streamProvider.OpenWrite(Path.Combine(_columnPath, RowIndexFileName)));
        }

        public Type WritingAsType => _valueWriter.WritingAsType;

        public void Append(XArray xarray)
        {
            // If we already had too many values, we're just writing them out normally
            if (_dictionary == null)
            {
                _valueWriter.Append(xarray);
                return;
            }

            // Otherwise, find the index of each value added
            if (_dictionary.Add(xarray, ref _currentArrayIndices))
            {
                // If we're still under 256 values, write the indices
                _rowIndexWriter.Append(XArray.All(_currentArrayIndices, xarray.Count));
            }
            else
            {
                // If we went over 256 values, convert to writing the values directly
                Convert();
                _valueWriter.Append(xarray);
                return;
            }
        }

        private void Convert()
        {
            // Close the row index writer
            _rowIndexWriter.Dispose();
            _rowIndexWriter = null;

            // Get the set of unique values and get rid of the value dictionary
            XArray values = _dictionary.Values();
            _dictionary = null;

            // Convert the indices previously written into raw values
            int[] valueIndices = new int[10240];
            using (IColumnReader rowIndexReader = new PrimitiveArrayReader<byte>(_streamProvider.OpenRead(Path.Combine(_columnPath, RowIndexFileName))))
            {
                int rowCount = rowIndexReader.Count;
                ArraySelector page = ArraySelector.All(0).NextPage(rowCount, 10240);
                while (page.Count > 0)
                {
                    // Read an XArray of indices and convert to int[]
                    XArray xarray = rowIndexReader.Read(page);
                    byte[] array = (byte[])xarray.Array;
                    for (int i = 0; i < xarray.Count; ++i)
                    {
                        valueIndices[i] = array[xarray.Index(i)];
                    }

                    // Write the corresponding values
                    _valueWriter.Append(values.Reselect(ArraySelector.Map(valueIndices, xarray.Count)));

                    page = page.NextPage(rowCount, 10240);
                }
            }

            // Delete the row index file
            _streamProvider.Delete(Path.Combine(_columnPath, RowIndexFileName));
        }

        public void Dispose()
        {
            // If we're still an enum column, write the distinct values out
            if (_dictionary != null)
            {
                _valueWriter.Append(_dictionary.Values());
                _dictionary = null;
            }

            if (_valueWriter != null)
            {
                _valueWriter.Dispose();
                _valueWriter = null;
            }

            if (_rowIndexWriter != null)
            {
                _rowIndexWriter.Dispose();
                _rowIndexWriter = null;
            }
        }
    }

    public class EnumReader : IColumnReader
    {
        private IColumnReader _valueReader;
        private IColumnReader _rowIndexReader;
        private Func<XArray, XArray> _rowIndexToIntConverter;

        private XArray _allValues;

        private EnumReader(IColumnReader valueReader, IColumnReader rowIndexReader)
        {
            _valueReader = valueReader;
            _rowIndexReader = rowIndexReader;
            _rowIndexToIntConverter = TypeConverterFactory.GetConverter(typeof(byte), typeof(int));
        }

        public static IColumnReader Wrap(IStreamProvider streamProvider, Type columnType, string columnPath, CachingOption option)
        {
            // Build a reader for the row indices (will be null if the 'VR.u8.bin' file isn't there)
            IColumnReader rowIndexReader = TypeProviderFactory.TryGetColumnReader(streamProvider, typeof(byte), Path.Combine(columnPath, EnumWriter.RowIndexFileName), option, typeof(EnumReader));

            // Build a reader for the values (require caching if we we have row indices)
            IColumnReader valueReader = TypeProviderFactory.TryGetColumnReader(streamProvider, columnType, columnPath, (rowIndexReader != null ? CachingOption.Always : option), typeof(EnumReader));

            // If there were row indices, wrap the column. Otherwise, return as-is.
            if (rowIndexReader != null) return new EnumReader(valueReader, rowIndexReader);
            return valueReader;
        }

        public int Count => _rowIndexReader.Count;

        public Type IndicesType => typeof(byte);

        public void Dispose()
        {
            if (_valueReader != null)
            {
                _valueReader.Dispose();
                _valueReader = null;
            }

            if (_rowIndexReader != null)
            {
                _rowIndexReader.Dispose();
                _rowIndexReader = null;
            }
        }

        // Read builds an indexed XArray pointing to the value for each row
        public XArray Read(ArraySelector selector)
        {
            return Remap(Values(), selector);
        }

        public XArray Remap(XArray values, ArraySelector selector)
        {
            // Read row indices and convert to int[]
            XArray indexByteArray = _rowIndexReader.Read(selector);
            XArray indexIntArray = _rowIndexToIntConverter(indexByteArray);

            // Return the selected values
            return values.Reselect(ArraySelector.Map((int[])indexIntArray.Array, indexIntArray.Count));
        }

        // Values returns the set of distinct values themselves
        public XArray Values()
        {
            // Read the values (if we haven't previously)
            if (_allValues.Array == null)
            {
                _allValues = _valueReader.Read(ArraySelector.All(_valueReader.Count));
                if (_allValues.Selector.Indices != null || _allValues.Selector.StartIndexInclusive != 0) throw new InvalidOperationException("EnumColumnReader values reader must read values contiguously.");
            }

            return _allValues;
        }

        // Indices returns the index of the value for each row in the selector
        public XArray Indices(ArraySelector selector)
        {
            return _rowIndexReader.Read(selector);
        }
    }
}
