// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Model.Column;
using Arriba.Model.Expressions;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  ColumnAliasCorrector replaces column aliases with full names.
    ///  It must be re-created when Table columns change.
    /// </summary>
    public class ColumnAliasCorrector : TermCorrector
    {
        private Dictionary<string, string> _columnNameMappings;

        public ColumnAliasCorrector(IEnumerable<KeyValuePair<string, string>> aliases)
        {
            _columnNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SetMappings(aliases);
        }

        public ColumnAliasCorrector(Table t)
            : this(AliasesFromTable(t))
        { }

        public void SetMappings(Table t)
        {
            this.SetMappings(AliasesFromTable(t));
        }

        public void SetMappings(IEnumerable<KeyValuePair<string, string>> aliases)
        {
            if (aliases == null) throw new ArgumentNullException("aliases");

            _columnNameMappings.Clear();

            foreach (KeyValuePair<string, string> mapping in aliases)
            {
                if (!String.IsNullOrEmpty(mapping.Key))
                {
                    _columnNameMappings[mapping.Key] = mapping.Value;
                }
            }
        }

        public override IExpression CorrectTerm(TermExpression te)
        {
            if (te == null) throw new ArgumentNullException("te");

            string fullColumnName;
            if (_columnNameMappings.TryGetValue(te.ColumnName, out fullColumnName))
            {
                // NOTE: We change the TermExpression directly rather than replacing it
                te.ColumnName = fullColumnName;
            }

            return null;
        }

        private static IEnumerable<KeyValuePair<string, string>> AliasesFromTable(Table t)
        {
            if (t == null) throw new ArgumentNullException("t");
            return t.ColumnDetails.Select((cd) => new KeyValuePair<string, string>(cd.Alias, cd.Name));
        }
    }
}
