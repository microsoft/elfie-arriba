using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.IO;
using XForm.Data;
using XForm.Transforms;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryTableWriter : DataBatchEnumeratorWrapper
    {
        private string _tableRootPath;
        private Func<DataBatch>[] _innerGetters;
        private Func<DataBatch>[] _getters;
        private IColumnWriter[] _writers;

        public BinaryTableWriter(IDataBatchEnumerator source, string tableRootPath) : base(source)
        {
            _tableRootPath = tableRootPath;
            DirectoryIO.DeleteAllContents(tableRootPath);
            Directory.CreateDirectory(tableRootPath);

            int columnCount = source.Columns.Count;

            List<ColumnDetails> columnSchemaToWrite = new List<ColumnDetails>();

            _innerGetters = new Func<DataBatch>[columnCount];
            _getters = new Func<DataBatch>[columnCount];
            _writers = new IColumnWriter[columnCount];

            for (int i = 0; i < columnCount; ++i)
            {
                ColumnDetails column = source.Columns[i];

                Func<DataBatch> directGetter = source.ColumnGetter(i);
                Func<DataBatch> outputTypeGetter = directGetter;
                IColumnWriter writer = null;
                
                // Build a direct writer for the column type, if available
                ITypeProvider columnTypeProvider = TypeProviderFactory.TryGet(column.Type);
                if(columnTypeProvider != null) writer = columnTypeProvider.BinaryWriter(Path.Combine(tableRootPath, source.Columns[i].Name));
                
                // If the column type doesn't have a provider or writer, convert to String8 and write that
                if(writer == null)
                {
                    Func<DataBatch, DataBatch> converter = TypeConverterFactory.GetConverter(column.Type, typeof(String8), null, false);
                    outputTypeGetter = () => converter(directGetter());
                    writer = TypeProviderFactory.TryGet(typeof(String8)).BinaryWriter(Path.Combine(tableRootPath, source.Columns[i].Name));
                    column = column.ChangeType(typeof(String8));
                }

                columnSchemaToWrite.Add(column);
                _innerGetters[i] = directGetter;
                _getters[i] = outputTypeGetter;
                _writers[i] = writer;
            }

            SchemaSerializer.Write(_tableRootPath, columnSchemaToWrite);
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            return _innerGetters[columnIndex];
        }

        public override int Next(int desiredCount)
        {
            int count = _source.Next(desiredCount);
            if (count == 0) return 0;

            for (int i = 0; i < _getters.Length; ++i)
            {
                _writers[i].Append(_getters[i]());
            }

            return count;
        }

        public override void Dispose()
        {
            base.Dispose();

            if(_writers != null)
            {
                foreach(IColumnWriter writer in _writers)
                {
                    writer.Dispose();
                }

                _writers = null;
            }
        }
    }
}
