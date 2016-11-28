namespace Arriba.Model.Query
{
    public class SelectResult : DataBlockResult
    {
        public ushort CountReturned { get; set; }

        public SelectResult(SelectQuery query) : base(query) { }
    }
}
