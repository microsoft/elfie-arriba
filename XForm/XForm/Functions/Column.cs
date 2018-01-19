// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Extensions;
using XForm.IO;
using XForm.Query;
using XForm.Types;

namespace XForm.Functions
{
    /// <summary>
    ///  Column is an IDataBatchColumn used to contain regular columns in expressions
    /// </summary>
    public class Column : IDataBatchColumn
    {
        private IDataBatchEnumerator Source { get; set; }
        private string ColumnName { get; set; }
        private int ColumnIndex { get; set; }

        public ColumnDetails ColumnDetails => Source.Columns[ColumnIndex];

        private Column(IDataBatchEnumerator source, string columnName, int columnIndex)
        {
            Source = source;
            ColumnName = columnName;
            ColumnIndex = columnIndex;
        }

        public static IDataBatchColumn Build(IDataBatchEnumerator source, XDatabaseContext context)
        {
            string columnName = context.Parser.NextColumnName(source);
            int columnIndex = source.Columns.IndexOfColumn(columnName);

            IDataBatchList sourceList = source as IDataBatchList;
            if(sourceList != null)
            {
                IColumnReader columnReader = sourceList.ColumnReader(columnIndex);
                if (columnReader is EnumReader) return new EnumColumn(sourceList, (EnumReader)columnReader, columnName, columnIndex);
            }
            return new Column(source, columnName, columnIndex);
        }

        public Func<DataBatch> Getter()
        {
            return Source.ColumnGetter(ColumnIndex);
        }

        public override string ToString()
        {
            return XqlScanner.Escape(ColumnName, TokenType.ColumnName);
        }
    }

    /// <summary>
    ///  EnumColumn is an IDataBatchColumn used to indicate an enum column in Expressions,
    ///  which can be optimized by looking at the set of available values once instead of
    ///  resolving each row to a value and comparing it.
    /// </summary>
    public class EnumColumn : IDataBatchEnumColumn
    {
        private IDataBatchList _source;
        private EnumReader _enumReader;
        private DataBatch _values;
        private string _columnName;
        private int _columnIndex;
        
        public ColumnDetails ColumnDetails { get; private set; }

        internal EnumColumn(IDataBatchList source, EnumReader enumReader, string columnName, int columnIndex)
        {
            _source = source;
            _enumReader = enumReader;
            _columnName = columnName;
            _columnIndex = columnIndex;
            _values = enumReader.Values();
            ColumnDetails = _source.Columns[_columnIndex];
        }

        internal EnumColumn(EnumColumn original, DataBatch mappedValues, Type newType)
        {
            _source = original._source;
            _enumReader = original._enumReader;
            _columnName = original._columnName;
            _columnIndex = original._columnIndex;
            _values = mappedValues;
            ColumnDetails = original.ColumnDetails.ChangeType(newType);
        }

        internal EnumColumn(EnumColumn original, Func<DataBatch, DataBatch> converter, Type newType) : this(original, converter(original.Values()), newType)
        { }

        public Func<DataBatch> Getter()
        {
            // Remap _values instead of calling EnumReader.Getter so that casted or converted EnumColumns return the new values
            return () => _enumReader.Remap(_values, _source.EnumerateSelector);
        }

        public DataBatch Values()
        {
            return _values;
        }

        public Func<DataBatch> Indices()
        {
            return () => _enumReader.Indices(_source.EnumerateSelector);
        }

        public override string ToString()
        {
            return XqlScanner.Escape(_columnName, TokenType.ColumnName);
        }
    }
}
