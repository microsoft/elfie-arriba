// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Expressions;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  MeCorrector replaces 'me' with the alias of the person making the query.
    /// </summary>
    public class MeCorrector : TermCorrector
    {
        private string UserAlias { get; set; }

        public MeCorrector(string userAlias)
        {
            this.UserAlias = userAlias;
        }

        public override IExpression CorrectTerm(TermExpression te)
        {
            string value = te.Value.ToString();
            if (value.Equals("me", StringComparison.OrdinalIgnoreCase) && te.Operator.IsOrCorrectableOperator())
            {
                // Correct 'me' to the current user alias, if seen
                return new OrExpression(te, new TermExpression(te.ColumnName, te.Operator, this.UserAlias));
            }

            return null;
        }
    }
}
