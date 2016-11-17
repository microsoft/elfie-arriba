// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Expressions;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  ColumnSecurityCorrector replaces all column terms with 'AllExceptColumns' expressions
    ///  to avoid querying restricted columns. It throws if the query has an explicit clause for
    ///  a restricted column.
    /// </summary>
    public class ColumnSecurityCorrector : TermCorrector
    {
        private HashSet<string> RestrictedColumns;

        public ColumnSecurityCorrector(IEnumerable<string> restrictedColumns)
        {
            this.RestrictedColumns = new HashSet<string>(restrictedColumns, StringComparer.OrdinalIgnoreCase);
        }

        public override IExpression CorrectTerm(TermExpression te)
        {
            if (te == null) throw new ArgumentNullException("te");

            if(te.ColumnName.Equals("*"))
            {
                return new AllExceptColumnsTermExpression(this.RestrictedColumns, te);
            }

            if(RestrictedColumns.Contains(te.ColumnName))
            {
                throw new ArribaColumnAccessDeniedException(te.ColumnName);
            }

            return null;
        }
    }
}
