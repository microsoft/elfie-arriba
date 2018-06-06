// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using XForm.Data;
using XForm.IO;
using XForm.IO.StreamProvider;

namespace XForm.Types
{
    /// <summary>
    ///  NullableWriter wraps an IColumnWriter and writes a second stream (Vn.b8.bin)
    ///  with which values are null (if any).
    /// </summary>
    public class NullableWriter : IColumnWriter
    {
        private IStreamProvider _streamProvider;
        private int _rowCountWritten;
        private string _columnPath;
        private IColumnWriter _valueWriter;
        private PrimitiveArrayWriter<bool> _nullWriter;

        private bool[] _falseArray;

        public NullableWriter(IStreamProvider streamProvider, string columnPath, IColumnWriter valueWriter)
        {
            _streamProvider = streamProvider;
            _columnPath = columnPath;
            _valueWriter = valueWriter;
        }

        public Type WritingAsType => _valueWriter.WritingAsType;

        public bool CanAppend(XArray xarray)
        {
            // We can append if the nulls themselves fit (only one byte each, to be smaller) 
            //  and the values can fit (should almost always be more limiting)
            return (_rowCountWritten + xarray.Count) <= BinaryTableWriter.ColumnFileSizeLimit
                && _valueWriter.CanAppend(xarray);
        }

        public void Append(XArray xarray)
        {
            // Write the values (without the null markers; we're writing those here)
            _valueWriter.Append(xarray.WithoutNulls());

            // Track the row count written so we know how many null=false values to write when we first see a null
            _rowCountWritten += xarray.Count;

            // If there are no nulls in this set and none previously, no null markers need to be written
            if (!xarray.HasNulls && _nullWriter == null) return;

            if (_nullWriter == null)
            {
                // Check whether any rows in the set are actually null; the source may contain nulls but the filtered rows might not
                bool areAnyNulls = false;
                for (int i = 0; i < xarray.Count && !areAnyNulls; ++i)
                {
                    areAnyNulls |= xarray.NullRows[xarray.Index(i)];
                }

                // If there are not actually any null rows in this set, don't write null output yet
                if (!areAnyNulls) return;

                // Open a new file to write IsNull booleans
                string nullsPath = Path.Combine(_columnPath, "Vn.b8.bin");
                _nullWriter = new PrimitiveArrayWriter<bool>(_streamProvider.OpenWrite(nullsPath));

                // Write false for every value so far
                int previousCount = _rowCountWritten - xarray.Count;
                Allocator.AllocateToSize(ref _falseArray, 1024);
                for (int i = 0; i < previousCount; i += 1024)
                {
                    int rowCount = Math.Min(1024, previousCount - i);
                    _nullWriter.Append(XArray.All(_falseArray, rowCount));
                }
            }

            if (!xarray.HasNulls)
            {
                // If this xarray doesn't have any nulls, write false for every value in this page
                Allocator.AllocateToSize(ref _falseArray, xarray.Count);
                _nullWriter.Append(XArray.All(_falseArray, xarray.Count));
            }
            else
            {
                // Write the actual true/false values for this page
                _nullWriter.Append(XArray.All(xarray.NullRows).Reselect(xarray.Selector));
            }
        }

        public void Dispose()
        {
            if (_valueWriter != null)
            {
                _valueWriter.Dispose();
                _valueWriter = null;
            }

            if (_nullWriter != null)
            {
                _nullWriter.Dispose();
                _nullWriter = null;
            }
        }
    }

    /// <summary>
    ///  NullableReader wraps an IColumnReader and reads the nullable stream (Vn.b8.bin)
    ///  and sets IsNull for the underlying rows.
    /// </summary>
    public class NullableReader : IColumnReader
    {
        private IColumnReader _valueReader;
        private IColumnReader _nullReader;

        private XArray _currentArray;
        private ArraySelector _currentSelector;

        private NullableReader(IColumnReader valueReader, IColumnReader nullReader)
        {
            _valueReader = valueReader;
            _nullReader = nullReader;
        }

        public static IColumnReader Wrap(IStreamProvider streamProvider, Type columnType, string columnPath, CachingOption option)
        {
            // Get the underlying value column
            IColumnReader valueReader = TypeProviderFactory.TryGetColumnReader(streamProvider, columnType, columnPath, option, typeof(NullableReader));
            if (valueReader == null) return null;

            // Get a null reader (or null if there's no nulls file)
            string nullsPath = Path.Combine(columnPath, "Vn.b8.bin");
            IColumnReader nullReader = TypeProviderFactory.TryGetColumnReader(streamProvider, typeof(bool), nullsPath, option, typeof(NullableReader));

            // If there are nulls, wrap in a NullableReader
            if (nullReader != null) return new NullableReader(valueReader, nullReader);

            // If not, return the underlying reader unwrapped
            return valueReader;
        }

        public int Count => _valueReader.Count;

        public XArray Read(ArraySelector selector)
        {
            // Return the cached xarray if re-requested
            if (selector.Equals(_currentSelector)) return _currentArray;

            // Read the values themselves
            XArray values = _valueReader.Read(selector);

            // Read the null markers
            XArray nulls = _nullReader.Read(selector);

            // Cache and return the values and null markers together
            _currentArray = XArray.All(values.Array, -1, (bool[])nulls.Array).Reselect(values.Selector);
            _currentSelector = selector;
            return _currentArray;
        }

        public void Dispose()
        {
            if (_valueReader != null)
            {
                _valueReader.Dispose();
                _valueReader = null;
            }

            if (_nullReader != null)
            {
                _nullReader.Dispose();
                _nullReader = null;
            }
        }
    }
}
