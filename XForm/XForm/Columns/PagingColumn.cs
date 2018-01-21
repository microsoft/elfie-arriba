using System;
using XForm.Data;

namespace XForm.Columns
{
    /// <summary>
    ///  PagingColumn pages through an inner array based on a set selector.
    /// </summary>
    internal class PagingColumn : IXColumn
    {
        private IXTable _table;
        private IXColumn _column;

        public PagingColumn(IXTable table, IXColumn column)
        {
            _table = table;
            _column = column;
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            int[] remapArray = null;

            return () => sourceGetter().Select(_table.CurrentSelector, ref remapArray);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            // Seek is blocked by SinglePageEnumerator
            return null;
        }

        public Func<XArray> ValuesGetter()
        {
            return _column.ValuesGetter();
        }

        public Type IndicesType => _column.IndicesType;

        public Func<XArray> IndicesCurrentGetter()
        {
            Func<XArray> sourceGetter = _column.IndicesCurrentGetter();
            if (sourceGetter == null) return null;

            int[] remapArray = null;
            return () => sourceGetter().Select(_table.CurrentSelector, ref remapArray);
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            // Seek is blocked by SinglePageEnumerator
            return null;
        }

        public override string ToString()
        {
            return _column.ToString();
        }
    }
}
