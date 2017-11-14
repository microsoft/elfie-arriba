using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.IO;
using XForm.Data;
using XForm.Transforms;
using XForm.Types;

namespace XForm.IO
{
    public class BinaryTableWriter : DataBatchEnumeratorWrapper
    {
        private string _tableRootPath;
        private Func<DataBatch>[] _getters;
        private IColumnWriter[] _writers;

        public BinaryTableWriter(IDataBatchEnumerator source, string tableRootPath) : base(source)
        {
            _tableRootPath = tableRootPath;
            Directory.CreateDirectory(tableRootPath);

            int columnCount = source.Columns.Count;

            _getters = new Func<DataBatch>[columnCount];
            _writers = new IColumnWriter[columnCount];
            for (int i = 0; i < columnCount; ++i)
            {
                ColumnDetails column = source.Columns[i];
                Func<DataBatch> getter = source.ColumnGetter(i);

                if(column.Type == typeof(String8))
                {
                    _getters[i] = getter;
                }
                else
                {
                    Func<DataBatch, DataBatch> converter = TypeConverterFactory.GetConverter(column.Type, typeof(String8), null, false);
                    _getters[i] = () => converter(getter());
                }

                _writers[i] = new String8ColumnWriter(Path.Combine(tableRootPath, source.Columns[i].Name));
            }

            SchemaSerializer.Write(_tableRootPath, _source.Columns);
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
