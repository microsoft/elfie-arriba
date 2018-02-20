using System.Collections.Generic;

namespace Arriba.Model.Query
{
    public class AllCountResult
    {
        public string Query { get; set; }
        public string ParsedQuery { get; set; }

        public List<CountResult> ResultsPerTable { get; set; }

        public AllCountResult(string query)
        {
            this.Query = query;
            this.ParsedQuery = QueryParser.Parse(query).ToString();

            this.ResultsPerTable = new List<CountResult>();
        }
    }

    public class CountResult
    {
        public string TableName { get; set; }
        public ulong Count { get; set; }
        public bool AllowedToRead { get; set; }
        public bool Succeeded { get; set; }

        public CountResult(string tableName, ulong count, bool allowedToRead, bool succeeded)
        {
            this.TableName = tableName;
            this.Count = count;
            this.AllowedToRead = allowedToRead;
            this.Succeeded = succeeded;
        }
    }

}
