using Arriba.Structures;

namespace Arriba.Model.Query
{
    public class BaseBlockResult : BaseResult
    {
        public IQuery Query { get; set; }
        public long Total { get; set; }
        public DataBlock Values { get; set; }
        
        public BaseBlockResult(IQuery query)
        {
            this.Query = query;
        }
    }
}
