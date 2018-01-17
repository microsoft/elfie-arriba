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
        private int _rowCountSoFar;
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

        public void Append(DataBatch batch)
        {
            // Write the values
            _valueWriter.Append(batch);

            // Track the row count written so we know how many null=false values to write when we first see a null
            _rowCountSoFar += batch.Count;

            // If there are no nulls in this set and none previously, no null markers need to be written
            if (batch.IsNull == null && _nullWriter == null) return;

            if (_nullWriter == null)
            {
                // Check whether any rows in the set are actually null; the source may contain nulls but the filtered rows might not
                bool areAnyNulls = false;
                for (int i = 0; i < batch.Count && !areAnyNulls; ++i)
                {
                    areAnyNulls |= batch.IsNull[batch.Index(i)];
                }

                // If there are not actually any null rows in this set, don't write null output yet
                if (!areAnyNulls) return;

                // Open a new file to write IsNull booleans
                string nullsPath = Path.Combine(_columnPath, "Vn.b8.bin");
                _nullWriter = new PrimitiveArrayWriter<bool>(_streamProvider.OpenWrite(nullsPath));

                // Write false for every value so far
                int previousCount = _rowCountSoFar - batch.Count;
                Allocator.AllocateToSize(ref _falseArray, 1024);
                for (int i = 0; i < previousCount; i += 1024)
                {
                    int rowCount = Math.Min(1024, previousCount - i);
                    _nullWriter.Append(DataBatch.All(_falseArray, rowCount));
                }
            }

            if (batch.IsNull == null)
            {
                // If this batch doesn't have any nulls, write false for every value in this page
                Allocator.AllocateToSize(ref _falseArray, batch.Count);
                _nullWriter.Append(DataBatch.All(_falseArray, batch.Count));
            }
            else
            {
                // Write the actual true/false values for this page
                _nullWriter.Append(DataBatch.All(batch.IsNull).Reselect(batch.Selector));
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
        private IStreamProvider _streamProvider;
        private string _columnPath;
        private IColumnReader _valueReader;
        private IColumnReader _nullReader;

        private DataBatch _currentBatch;
        private ArraySelector _currentSelector;

        public NullableReader(IStreamProvider streamProvider, string columnPath, IColumnReader valueReader, bool requireCached)
        {
            _streamProvider = streamProvider;
            _columnPath = columnPath;
            _valueReader = valueReader;

            // NullableReader can't use TypeProviderFactory.TryGetColumn or it'll be recursively wrapped in a NullableReader also.
            string nullsPath = Path.Combine(_columnPath, "Vn.b8.bin");
            _nullReader = ColumnCache.Instance.GetOrBuild(nullsPath, requireCached, () =>
            {
                if (!streamProvider.Attributes(nullsPath).Exists) return null;
                return new PrimitiveArrayReader<bool>(streamProvider.OpenRead(nullsPath));
            });
        }

        public int Count => _valueReader.Count;

        public DataBatch Read(ArraySelector selector)
        {
            // Return the cached batch if re-requested
            if (selector.Equals(_currentSelector)) return _currentBatch;

            // Read the values themselves
            DataBatch values = _valueReader.Read(selector);

            // If there are no nulls, return as-is
            if (_nullReader == null) return values;

            // Otherwise, read the null markers
            DataBatch nulls = _nullReader.Read(selector);

            // Cache and return the values and null markers together
            _currentBatch = DataBatch.All(values.Array, -1, (bool[])nulls.Array).Reselect(values.Selector);
            _currentSelector = selector;
            return _currentBatch;
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
