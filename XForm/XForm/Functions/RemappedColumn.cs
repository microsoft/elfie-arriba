using System;
using XForm.Data;
using XForm.Transforms;

namespace XForm.Functions
{
    /// <summary>
    ///  RemappedColumn is an IXColumn wrapper which remaps the returned values based on
    ///  a remapper filtered set.
    ///  
    ///  It is used by Where, Join, and Choose to filter rows to the set matching the verb.
    /// </summary>
    public class RemappedColumn : IXColumn
    {
        private IXTable _table;
        private IXColumn _column;
        private RowRemapper _remapper;

        private XArray _currentArray;
        private ArraySelector _currentArraySelector;

        private XArray _currentIndices;
        private ArraySelector _currentIndicesSelector;

        // TODO: Re-add requesting more than the current desired count and paging through

        public RemappedColumn(IXTable table, IXColumn column, RowRemapper remapper)
        {
            _table = table;
            _column = column;
            _remapper = remapper;
        }

        public ColumnDetails ColumnDetails => _column.ColumnDetails;

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            int[] remapArray = null;

            return () =>
            {
                if (!_table.CurrentSelector.Equals(_currentArraySelector))
                {
                    _currentArray = _remapper.Remap(sourceGetter(), ref remapArray);
                    _currentArraySelector = _table.CurrentSelector;
                }

                return _currentArray;
            };
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            // Seeking isn't supported on remapped columns
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
            return () =>
            {
                if (!_table.CurrentSelector.Equals(_currentIndicesSelector))
                {
                    _currentIndices = _remapper.Remap(sourceGetter(), ref remapArray);
                    _currentIndicesSelector = _table.CurrentSelector;
                }

                return _currentIndices;
            };
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            // Seeking isn't supported on remapped columns
            return null;
        }

        public override string ToString()
        {
            return _column.ToString();
        }
    }
}
