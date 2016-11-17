namespace Arriba.Model.Query
{
    public class SelectResult : BaseBlockResult
    {
        public ushort CountReturned { get; set; }

        public SelectResult(SelectQuery query) : base(query) { }
    }
}
