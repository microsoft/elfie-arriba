using System;
using System.IO;
using XForm.Data;

namespace XForm.Types
{
    /// <summary>
    ///  NullableWriter wraps an IColumnWriter and writes a second stream (Vn.b8.bin)
    ///  with which values are null (if any).
    /// </summary>
    public class NullableWriter : IColumnWriter
    {
        private int _rowCountSoFar;
        private string _columnPath;
        private IColumnWriter _valueWriter;
        private PrimitiveArrayWriter<bool> _nullWriter;

        private bool[] _falseArray;

        public NullableWriter(string columnPath, IColumnWriter valueWriter)
        {
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
                for(int i = 0; i < batch.Count && !areAnyNulls; ++i)
                {
                    areAnyNulls |= batch.IsNull[batch.Index(i)];
                }

                // If there are not actually any null rows in this set, don't write null output yet
                if (!areAnyNulls) return;

                // Open a new file to write IsNull booleans
                string nullsPath = Path.Combine(_columnPath, "Vn.b8.bin");
                _nullWriter = new PrimitiveArrayWriter<bool>(new FileStream(nullsPath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete));

                // Write false for every value so far
                int previousCount = _rowCountSoFar - batch.Count;
                Allocator.AllocateToSize(ref _falseArray, 1024);
                for(int i = 0; i < previousCount; i += 1024)
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
                _nullWriter.Append(DataBatch.All(batch.IsNull).Select(batch.Selector));
            }
        }

        public void Dispose()
        {
            if(_valueWriter != null)
            {
                _valueWriter.Dispose();
                _valueWriter = null;
            }

            if(_nullWriter != null)
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
        private string _columnPath;
        private IColumnReader _valueReader;
        private PrimitiveArrayReader<bool> _nullReader;

        public NullableReader(string columnPath, IColumnReader valueReader)
        {
            _columnPath = columnPath;
            _valueReader = valueReader;

            string nullsPath = Path.Combine(_columnPath, "Vn.b8.bin");
            if(File.Exists(nullsPath)) _nullReader = new PrimitiveArrayReader<bool>(new FileStream(nullsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public int Count => _valueReader.Count;

        public DataBatch Read(ArraySelector selector)
        {
            // Read the values themselves
            DataBatch values = _valueReader.Read(selector);

            // If there are no nulls, return as-is
            if (_nullReader == null) return values;

            // Otherwise, read the null markers
            DataBatch nulls = _nullReader.Read(selector);

            // Return the values and null markers together
            return DataBatch.All(values.Array, -1, (bool[])nulls.Array).Select(values.Selector);
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
