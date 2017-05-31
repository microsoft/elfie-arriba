// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  JoinCorrector replaces column references from other queries
    ///  with the collection of required values.
    /// </summary>
    public class JoinCorrector : TermCorrector
    {
        private Database DB { get; set; }
        private ICorrector NestedCorrector { get; set; }
        private IList<SelectQuery> Queries { get; set; }

        public JoinCorrector(Database db, ICorrector nestedCorrector, IList<SelectQuery> querySet)
        {
            this.DB = db;
            this.NestedCorrector = nestedCorrector;
            this.Queries = querySet;
        }

        public override IExpression CorrectTerm(TermExpression te)
        {
            if (te == null) throw new ArgumentNullException("te");

            // Look for a join term in the query
            string value = te.Value.ToString();
            if (value.StartsWith("#Q"))
            {
                Regex referenceExpression = new Regex(@"#Q(?<number>\d+)\[(?<columnName>[^\]]+)\]");
                Match m = referenceExpression.Match(value);
                if (m.Success)
                {
                    int referencedQuery = int.Parse(m.Groups["number"].Value);
                    string referencedColumn = m.Groups["columnName"].Value;

                    // Get the referenced query and recursively Join it, if required [1-based]
                    SelectQuery joinQuery = this.Queries[referencedQuery - 1];
                    joinQuery.Correct(this.NestedCorrector);
                    joinQuery.Correct(this);

                    // Ask for the Join column and maximum number of values
                    joinQuery.Columns = new string[] { referencedColumn };
                    joinQuery.Count = ushort.MaxValue;

                    // Run the query
                    Table t = this.DB[joinQuery.TableName];
                    SelectResult result = t.Query(joinQuery);

                    if (result.Total == 0)
                    {
                        return new TermExpression(te.ColumnName, te.Operator, String.Empty);
                    }
                    else
                    {
                        return new TermInExpression(te.ColumnName, te.Operator, result.Values.GetColumn(0));
                    }
                }
            }

            return null;
        }
    }
}
