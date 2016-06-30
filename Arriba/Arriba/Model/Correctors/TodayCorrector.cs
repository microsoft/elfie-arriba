// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Expressions;
using Arriba.Structures;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  TodayCorrector replaces 'today' and 'today-n' with literal DateTimes.
    /// </summary>
    public class TodayCorrector : TermCorrector
    {
        public override IExpression CorrectTerm(TermExpression te)
        {
            if (te == null) throw new ArgumentNullException("te");

            if (!te.ColumnName.Equals("*"))
            {
                string value = te.Value.ToString();
                if (value.Equals("today", StringComparison.OrdinalIgnoreCase))
                {
                    return new TermExpression(te.ColumnName, te.Operator, DateTime.Today.ToUniversalTime());
                }
                else if (value.StartsWith("today-", StringComparison.OrdinalIgnoreCase))
                {
                    int relativeDays = 0;
                    if (int.TryParse(value.Substring(6), out relativeDays))
                    {
                        return new TermExpression(te.ColumnName, te.Operator, DateTime.Today.AddDays(-relativeDays).ToUniversalTime());
                    }
                }
            }

            return null;
        }
    }
}
