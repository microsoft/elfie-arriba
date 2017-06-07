using System.Collections.Generic;
using V5.Serialization;

namespace V5.Data
{
    class Partition
    {
        private string _identifier;
        private CachedLoader _loader;

        private Dictionary<string, IColumn> _columns;

        public Partition(CachedLoader loader, string identifier)
        {
            this._loader = loader;
            this._identifier = identifier;
        }

        public void AddColumn(string columnName, string typeIdentifier)
        {
            this._columns[columnName] = ColumnFactory.Build(columnName, typeIdentifier, this._identifier, this._loader);
        }

        public IColumn TryGetColumn(string columnName)
        {
            IColumn result;
            if (!_columns.TryGetValue(columnName, out result)) result = null;
            return result;
        }
    }
}
