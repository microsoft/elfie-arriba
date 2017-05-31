// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model.Correctors;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    public interface IQuery
    {
        /// <summary>
        ///  Table to query
        /// </summary>
        string TableName { get; set; }

        /// <summary>
        ///  Base Where clause to filter the set of rows to compute this query over
        /// </summary>
        IExpression Where { get; set; }

        /// <summary>
        /// Flag indicating this query requires merge to be called to complete result calculations
        /// </summary>
        bool RequireMerge { get; }

        /// <summary>
        ///  Generic place to do computation before per-partition splitting for
        ///  multi-step queries.
        /// </summary>
        /// <param name="table">Table being queried</param>
        void OnBeforeQuery(ITable table);

        /// <summary>
        ///  Correct all IExpressions using the provided corrector before evaluation.
        /// </summary>
        /// <param name="corrector">ICorrector to use for corrections</param>
        void Correct(ICorrector corrector);
    }

    public interface IQuery<T> : IQuery
    {
        /// <summary>
        ///  Compute a per-Partition result for the query
        /// </summary>
        /// <param name="p">Partition against which to evaluate</param>
        /// <returns>Result T for this partition</returns>
        T Compute(Partition p);

        /// <summary>
        ///  Merge together per-Partition results into a single result.
        ///  Design to allow rolling up in multiple passes (all partitions
        ///  on one machine, then merge across machines).
        /// </summary>
        /// <param name="partitionResults">Results from Compute per Partition</param>
        /// <returns>Overall result for query</returns>
        T Merge(T[] partitionResults);
    }
}
