// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model.Expressions;
using Arriba.Model.Query;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  UserAliasCorrector replaces recognized person aliases with the Display Names of those people.
    /// </summary>
    public class UserAliasCorrector : TermCorrector
    {
        private Table People { get; set; }

        public UserAliasCorrector(Table peopleTable)
        {
            this.People = peopleTable;
        }

        public override IExpression CorrectTerm(TermExpression te)
        {
            // If we couldn't get the People table, don't do anything
            if (this.People == null) return null;

            // For a specific column, a string value, and an operator safe to correct with 'original OR correction', consider alias correction
            if (!te.ColumnName.Equals("*") && te.Value.BestType().Equals(typeof(string)) && te.Operator.IsOrCorrectableOperator())
            {
                // Query for aliases equal to this value
                string value = te.Value.ToString();

                SelectQuery q = new SelectQuery();
                q.Columns = new string[] { "Display Name" };
                q.Where = new TermExpression("Alias", Operator.Equals, value);
                q.Count = 1;

                SelectResult r = this.People.Query(q);

                // If one is found, return the original value or the alias
                if (r.Total > 0)
                {
                    return new OrExpression(te, new TermExpression(te.ColumnName, te.Operator, r.Values[0, 0]));
                }
            }

            return null;
        }
    }
}
