using Arriba.Structures;

namespace Arriba.Model.Query
{
    public class SelectResult : DataBlockResult
    {
        public ushort CountReturned { get; set; }

        internal DataBlock OrderByValues { get; set; }

        public SelectResult(SelectQuery query) : base(query) { }
    }
}
