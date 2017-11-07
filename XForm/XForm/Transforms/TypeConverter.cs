using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;

namespace XForm.Transforms
{
    public class TypeConverter : IDataBatchSource
    {
        private IDataBatchSource _source;
        private List<ColumnDetails> _columns;
        private string _columnName;

        private int _sourceColumnIndex;
        private Type _sourceType;
        private Type _targetType;

        public TypeConverter(IDataBatchSource source, string columnName, Type targetType)
        {
            _source = source;
            _columnName = columnName;
            _targetType = targetType;

            _sourceColumnIndex = -1;
            _columns = new List<ColumnDetails>();
            for(int i = 0; i < source.Columns.Count; ++i)
            {
                ColumnDetails column = source.Columns[i];
                if (column.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    _sourceType = column.Type;
                    _sourceColumnIndex = i;
                    _columns.Add(column.ChangeType(targetType, column.Nullable));
                }
                else
                {
                    _columns.Add(column);
                }
            }

            if (_sourceColumnIndex == -1) throw new ArgumentException($"TypeConverter couldn't find column \"{columnName}\". Source columns: \"{string.Join(", ", source.Columns.Select((cd) => cd.Name))}\".");
        }

        public IReadOnlyList<ColumnDetails> Columns => _columns;

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being converted
            if (columnIndex != _sourceColumnIndex) return _source.ColumnGetter(columnIndex);

            // Cache the function to get the source data
            Func<DataBatch> sourceGetter = _source.ColumnGetter(columnIndex);

            // Build the appropriate converter and conversion function
            // TODO: This is hardcoded for int[] to String8 only.
            String8Converter converter = new String8Converter();
            return () =>
            {
                DataBatch sourceBatch = sourceGetter();
                return converter.ConvertInt(sourceBatch);
            };
        }

        public int Next(int desiredCount)
        {
            return _source.Next(desiredCount);
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }
        }
    }

    internal class String8Converter
    {
        private String8[] _array;
        private byte[] _buffer;

        public String8Converter()
        { }

        public DataBatch ConvertInt(DataBatch batch)
        {
            if (_array == null || _array.Length < batch.Count) _array = new String8[batch.Count];
            if (_buffer == null || _buffer.Length < batch.Count * 12) _buffer = new byte[batch.Count * 12];

            int count = 0;
            int bufferBytesUsed = 0;

            int[] sourceArray = (int[])batch.Array;
            for(int i = batch.StartIndexInclusive; i < batch.EndIndexExclusive; ++i)
            {
                int realIndex = i;
                if (batch.Indices != null) realIndex = batch.Indices[realIndex];

                String8 result = String8.FromInteger(sourceArray[realIndex], _buffer, bufferBytesUsed);
                bufferBytesUsed += result.Length;

                _array[count] = result;
                count++;
            }

            return DataBatch.All(_array, count);
        }
    }
}
