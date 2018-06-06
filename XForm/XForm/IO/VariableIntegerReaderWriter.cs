// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.IO.StreamProvider;
using XForm.Types;

namespace XForm.IO
{
    /// <summary>
    ///  VariableIntegerWriter is an IColumnWriter which writes values as byte, ushort, or int.
    ///  Rows are written as the smallest possible type; if a value overflows the small type, the column is converted to a bigger type.
    /// </summary>
    public class VariableIntegerWriter : IColumnWriter
    {
        private IStreamProvider _streamProvider;
        private string _columnPathPrefix;

        private Func<XArray, XArray> _converter;
        private IColumnWriter _writer;
        private int _rowCountWritten;

        public Type WritingAsType { get; private set; }

        public VariableIntegerWriter(IStreamProvider streamProvider, string columnPathPrefix)
        {
            _streamProvider = streamProvider;
            _columnPathPrefix = columnPathPrefix;

            // Initialize writing values in just one byte (while we don't have any values too big)
            WritingAsType = typeof(byte);
            _writer = BuildDirectWriter(_streamProvider, WritingAsType, PathForType(_columnPathPrefix, WritingAsType));
            _converter = TypeConverterFactory.GetConverter(typeof(int), typeof(byte));
        }

        internal static string PathForType(string columnPathPrefix, Type type)
        {
            return $"{columnPathPrefix}.{PrimitiveTypeProvider<byte>.BinaryFileTypePart(type)}.bin";
        }

        public bool CanAppend(XArray xarray)
        {
            return _writer.CanAppend(xarray);
        }

        private static IColumnWriter BuildDirectWriter(IStreamProvider streamProvider, Type type, string columnPathFull)
        {
            // Open the correct *direct* writer for the desired type.
            // We aren't using TypeProviderFactory.TryGetColumnWriter so we don't have null or enum wrapping.

            if (type == typeof(byte)) return new ByteWriter(streamProvider.OpenWrite(columnPathFull));
            if (type == typeof(ushort)) return new PrimitiveArrayWriter<ushort>(streamProvider.OpenWrite(columnPathFull));
            if (type == typeof(int)) return new PrimitiveArrayWriter<int>(streamProvider.OpenWrite(columnPathFull));
            throw new NotImplementedException(type.Name.ToString());
        }

        private void Upconvert(XArray values)
        {
            if (WritingAsType == typeof(byte))
            {
                // Try to convert the new values to ushort
                Func<XArray, XArray> ushortConverter = TypeConverterFactory.GetConverter(typeof(int), typeof(ushort));
                XArray asUshort = ushortConverter(values);

                // If none overflow, just upconvert to ushort
                if (!asUshort.HasNulls)
                {
                    Upconvert(typeof(ushort));
                    _writer.Append(asUshort);
                    return;
                }
            }

            // If we were already ushort or new values overflowed ushort, upconvert to int
            Upconvert(typeof(int));
            _writer.Append(values);
        }

        private void Upconvert(Type toType)
        {
            // Close the current writer
            _writer.Dispose();
            _writer = null;

            // Determine previous and new file paths
            string columnValuesFullPath = PathForType(_columnPathPrefix, WritingAsType);
            string columnConvertedFullPath = PathForType(_columnPathPrefix, toType);

            // Build a writer for the larger type
            IColumnWriter writer = BuildDirectWriter(_streamProvider, toType, columnConvertedFullPath);

            // Convert already written values (if any)
            if (_rowCountWritten > 0)
            {
                // Build a converter to convert the values
                Func<XArray, XArray> converter = TypeConverterFactory.GetConverter(WritingAsType, toType);

                // Stream them in, convert them, and write them out
                using (IColumnReader reader = TypeProviderFactory.TryGetColumnReader(_streamProvider, WritingAsType, columnValuesFullPath))
                {
                    int rowCount = reader.Count;
                    ArraySelector page = ArraySelector.All(0).NextPage(rowCount, 10240);

                    while (page.Count > 0)
                    {
                        XArray original = reader.Read(page);
                        XArray converted = converter(original);
                        writer.Append(converted);

                        page = page.NextPage(rowCount, 10240);
                    }
                }
            }

            // Delete the original file
            _streamProvider.Delete(columnValuesFullPath);

            // Re-initialize for the new writer
            WritingAsType = toType;
            _writer = writer;
            _converter = (toType == typeof(int) ? null : TypeConverterFactory.GetConverter(typeof(int), toType));
        }

        public void Append(XArray xarray)
        {
            if (_converter != null)
            {
                // Convert the array to the type we're writing
                XArray convertedArray = _converter(xarray);

                if (!convertedArray.HasNulls)
                {
                    // If values were all in range, just write them
                    _writer.Append(convertedArray);
                }
                else
                {
                    // If values were out of range, we have to upconvert
                    Upconvert(xarray);
                }
            }
            else
            {
                // If we're already up to integer, just write directly
                _writer.Append(xarray);
            }

            _rowCountWritten += xarray.Count;
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }
    }

    /// <summary>
    ///  VariableIntegerReader is an IColumnReader which can read values written as byte, ushort, or int.
    ///  It returns the read values consistently as an int XArray.
    /// </summary>
    public class VariableIntegerReader : IColumnReader
    {
        private IColumnReader _reader;
        private Func<XArray, XArray> _converter;

        public VariableIntegerReader(IStreamProvider streamProvider, string columnPathPrefix, CachingOption option)
        {
            // Look for each potential size in descending order and build the right reader and converter
            Type type = typeof(int);
            string path = VariableIntegerWriter.PathForType(columnPathPrefix, typeof(int));
            if (streamProvider.Attributes(path).Exists)
            {
                _reader = TypeProviderFactory.TryGetColumnReader(streamProvider, typeof(int), path, option, typeof(VariableIntegerReader));
                _converter = null;
                return;
            }

            path = VariableIntegerWriter.PathForType(columnPathPrefix, typeof(ushort));
            if (streamProvider.Attributes(path).Exists)
            {
                _reader = TypeProviderFactory.TryGetColumnReader(streamProvider, typeof(ushort), path, option, typeof(VariableIntegerReader));
                _converter = TypeConverterFactory.GetConverter(typeof(ushort), typeof(int));
                return;
            }

            path = VariableIntegerWriter.PathForType(columnPathPrefix, typeof(byte));
            _reader = TypeProviderFactory.TryGetColumnReader(streamProvider, typeof(byte), path, option, typeof(VariableIntegerReader));
            _converter = TypeConverterFactory.GetConverter(typeof(byte), typeof(int));
        }

        public int Count => _reader.Count;

        public XArray Read(ArraySelector selector)
        {
            XArray raw = _reader.Read(selector);
            XArray asInteger = (_converter == null ? raw : _converter(raw));

            return asInteger;
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
    }
}
