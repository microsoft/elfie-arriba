// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Arriba.Model.Expressions;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  ICorrectors can transform expression terms arbitrarily, known as
    ///  "correction". These are used to resolve column aliases, transform
    ///  shortcut terms (today, me), and so on.
    /// </summary>
    public interface ICorrector
    {
        /// <summary>
        ///  Correct an IExpression, traversing the Expression. Return the
        ///  same IExpression or the corrected replacement.
        /// </summary>
        /// <param name="expression">IExpression to correct</param>
        /// <returns>Corrected Expression (or same one passed if no changes)</returns>
        IExpression Correct(IExpression expression);

        // ComposedCorrector will call Traverse for each nested corrector in turn.
        // Extension method will call CorrectTerm per term on passed corrector.
        // Existing Correctors will implement CorrectTerm.

        /// <summary>
        ///  Correct an Expression term. Return null if no correction or
        ///  the revised expression to correct it.
        ///  
        ///  You should return OrExpression(original, correction) unless
        ///  you know that your correction will always be wanted or will
        ///  always include what the original would.
        /// </summary>
        /// <param name="te">TermExpression to correct</param>
        /// <returns>Corrected expression or null for no change</returns>
        IExpression CorrectTerm(TermExpression te);
    }

    public static class CorrectorTraverser
    {
        /// <summary>
        ///  Traverse the Expression, considering correcting each term
        ///  with the provided Corrector. This should be the default
        ///  implementation of Correct() for Correctors which only care
        ///  about correcting individual terms.
        ///  
        ///  Callers which are not correctors should call corrector.Correct,
        ///  not this method, so that correctors which correct more broadly
        ///  than terms will work.
        /// </summary>
        /// <param name="expression">IExpression to correct</param>
        /// <param name="corrector">ICorrector to use</param>
        /// <returns>IExpression with any corrections</returns>
        public static IExpression CorrectTerms(IExpression expression, ICorrector corrector)
        {
            // Correct the root term, if it's a term
            if (expression is TermExpression)
            {
                IExpression correction = corrector.CorrectTerm((TermExpression)expression);
                if (correction != null) return correction;
            }
            else
            {
                // Otherwise, consider correcting each child and recurse
                IList<IExpression> children = expression.Children();

                for (int i = 0; i < children.Count; ++i)
                {
                    IExpression child = children[i];

                    // Correct this child, if it's a Term
                    if (child is TermExpression)
                    {
                        IExpression correction = corrector.CorrectTerm((TermExpression)child);
                        if (correction != null) children[i] = correction;
                    }
                    else
                    {
                        // Otherwise, recurse
                        CorrectTerms(child, corrector);
                    }
                }
            }

            return expression;
        }
    }
}
