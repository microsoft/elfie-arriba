// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Correctors;
using Arriba.Model.Expressions;

namespace Arriba.Model.Query
{
    /// <summary>
    ///  IQuery wrapper which implements Joins.
    ///  During correction, the JoinCorrector corrects additional joined queries
    ///  and then replaces terms in the primary query by evaluating the referenced join
    ///  and getting all results for the specified column.
    ///  
    ///  For example, if we have:
    ///    Q = [GroupName]:Nice AND [Members]::#Q1[Alias]
    ///    Q1 = [TeamName]:T1
    ///    
    ///  Then correcting 'Q' will cause the JoinCorrector to run Q1, get all
    ///  values for the 'Alias' column, split them, and replace [Members]::#Q1[Alias]
    ///  with [Members]:: IN(alias1, alias2, alias3, ...).
    ///  
    ///  NOTE: It's a future goal to allow the nested queries to be more generic than SelectQuery.
    ///   This requires:
    ///     - Must be able to set columns, count, and maybe sort order on query.
    ///     - Must be able to get DataBlock from the result.
    /// </summary>
    public class JoinQuery<T> : IQuery<T>
    {
        public Database DB { get; set; }
        public IQuery<T> PrimaryQuery { get; set; }
        public IList<SelectQuery> JoinQueries { get; set; }

        public JoinQuery(Database db, IQuery<T> primary, params SelectQuery[] joinQueries)
        {
            this.DB = db;
            this.PrimaryQuery = primary;
            this.JoinQueries = joinQueries;
        }

        public JoinQuery(Database db, IQuery<T> primary, IList<SelectQuery> joinQueries)
        {
            this.DB = db;
            this.PrimaryQuery = primary;
            this.JoinQueries = joinQueries;
        }

        public string TableName
        {
            get { return this.PrimaryQuery.TableName; }
            set { this.PrimaryQuery.TableName = value; }
        }

        public IExpression Where
        {
            get { return this.PrimaryQuery.Where; }
            set { this.PrimaryQuery.Where = value; }
        }

        public bool RequireMerge
        {
            get { return this.PrimaryQuery.RequireMerge; }
        }

        public void OnBeforeQuery(ITable table)
        {
            this.PrimaryQuery.OnBeforeQuery(table);
        }

        public void Correct(ICorrector corrector)
        {
            // Correct with the existing correctors
            this.PrimaryQuery.Correct(corrector);

            // Correct and evaluate any referenced join queries
            JoinCorrector j = new JoinCorrector(this.DB, corrector, this.JoinQueries);
            this.PrimaryQuery.Correct(j);
        }

        public T Compute(Partition p)
        {
            return this.PrimaryQuery.Compute(p);
        }

        public T Merge(T[] partitionResults)
        {
            return this.PrimaryQuery.Merge(partitionResults);
        }
    }
}
