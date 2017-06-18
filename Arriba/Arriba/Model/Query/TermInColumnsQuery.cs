using System;

using Arriba.Model.Correctors;
using Arriba.Model.Expressions;
using Arriba.Structures;
using System.Collections.Generic;
using System.Linq;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  TermInColumnsQuery returns the set of columns in a given query which contain
    ///  a specific bare term, in order by the count of rows with the term descending.
    ///  
    ///  It is used to provide Inline Insights for "BareTerm" queries.
    /// </summary>
    public class TermInColumnsQuery : IQuery<DataBlockResult>
    {
        public string Term { get; set; }

        public string TableName { get; set; }
        public IExpression Where { get; set; }
        public bool RequireMerge => false;

        public TermInColumnsQuery() : base()
        { }

        public TermInColumnsQuery(string term, string where)
        {
            this.Term = term;
            this.Where = QueryParser.Parse(where);
        }

        public void OnBeforeQuery(ITable table)
        { }

        public void Correct(ICorrector corrector)
        {
            if (corrector == null) throw new ArgumentNullException("corrector");
            this.Where = corrector.Correct(this.Where);
        }

        public DataBlockResult Compute(Partition p)
        {
            if (p == null) throw new ArgumentNullException("p");
            DataBlockResult result = new DataBlockResult(this);

            // Find matches for the remaining query
            ShortSet baseQueryMatches = new ShortSet(p.Count);
            this.Where.TryEvaluate(p, baseQueryMatches, result.Details);

            // Find and count matches per column for the term in the outer query
            List<Tuple<string, int>> matchCountPerColumn = new List<Tuple<string, int>>();

            if (baseQueryMatches.Count() > 0)
            {
                TermExpression bareTerm = new TermExpression(this.Term);
                ShortSet termMatchesForColumn = new ShortSet(p.Count);

                bool succeeded = false;
                ExecutionDetails perColumnDetails = new ExecutionDetails();

                foreach (IColumn<object> column in p.Columns.Values)
                {
                    termMatchesForColumn.Clear();

                    perColumnDetails.Succeeded = true;
                    column.TryWhere(Operator.Matches, this.Term, termMatchesForColumn, perColumnDetails);
                    succeeded |= perColumnDetails.Succeeded;

                    termMatchesForColumn.And(baseQueryMatches);

                    ushort matchCount = termMatchesForColumn.Count();
                    if (matchCount > 0)
                    {
                        matchCountPerColumn.Add(new Tuple<string, int>(column.Name, (int)matchCount));
                    }
                }

                // Sort results by count of matches descending
                matchCountPerColumn.Sort((left, right) => right.Item2.CompareTo(left.Item2));
            }

            // Copy to a DataBlock and return it
            int index = 0;
            DataBlock block = new DataBlock(new string[] { "ColumnName", "Count" }, matchCountPerColumn.Count);
            foreach(var column in matchCountPerColumn)
            {
                block[index, 0] = column.Item1;
                block[index, 1] = column.Item2;
                index++;
            }

            result.Values = block;
            result.Total = baseQueryMatches.Count();
            return result;
        }

        public DataBlockResult Merge(DataBlockResult[] partitionResults)
        {
            if (partitionResults == null) throw new ArgumentNullException("partitionResults");
            if (partitionResults.Length == 0) throw new ArgumentException("Length==0 not supported", "partitionResults");
            if (!partitionResults[0].Details.Succeeded) return partitionResults[0];

            DataBlockResult mergedResult = new DataBlockResult(this);

            // Add together the results per partition
            Dictionary<string, int> matchCountPerColumn = new Dictionary<string, int>();
            for(int partitionIndex = 0; partitionIndex < partitionResults.Length; ++partitionIndex)
            {
                DataBlockResult result = partitionResults[partitionIndex];

                for(int rowIndex = 0; rowIndex < result.Values.RowCount; ++rowIndex)
                {
                    string columnName = (string)result.Values[rowIndex, 0];

                    int count;
                    matchCountPerColumn.TryGetValue(columnName, out count);

                    matchCountPerColumn[columnName] = count + (int)result.Values[rowIndex, 1];
                }

                mergedResult.Details.Merge(result.Details);
                mergedResult.Total += result.Total;
            }

            // Build a combined block sorted by count descending
            DataBlock merged = new DataBlock(new string[] { "ColumnName", "Count" }, matchCountPerColumn.Count);
            int index = 0;
            foreach(var column in matchCountPerColumn.OrderByDescending((kvp) => kvp.Value))
            {
                merged[index, 0] = column.Key;
                merged[index, 1] = column.Value;
                index++;
            }

            mergedResult.Values = merged;
            return mergedResult;
        }
    }
}
