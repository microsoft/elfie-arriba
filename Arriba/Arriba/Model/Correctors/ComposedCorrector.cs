// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Model.Expressions;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  ComposedCorrector allows wrapping multiple correctors. Each corrector
    ///  is called in turn, so they are able to build on each other. A given
    ///  corrector will not be called to re-correct anything it has returned from
    ///  correcting a term.
    /// </summary>
    public class ComposedCorrector : ICorrector
    {
        public List<ICorrector> InnerCorrectors { get; set; }

        public ComposedCorrector(params ICorrector[] correctors)
        {
            this.InnerCorrectors = new List<ICorrector>(correctors);
        }

        public void Add(ICorrector corrector)
        {
            this.InnerCorrectors.Add(corrector);
        }

        public IExpression Correct(IExpression expression)
        {
            // Traverse with each corrector in turn, so that the first correctors take precedence
            foreach (ICorrector corrector in this.InnerCorrectors)
            {
                expression = corrector.Correct(expression);
            }

            return expression;
        }

        public IExpression CorrectTerm(TermExpression expression)
        {
            throw new InvalidOperationException("CorrectTerm should not be called on a ComposedCorrector. Callers should always call corrector.Correct, and for ComposedCorrectors this will call CorrectTerm on each inner corrector, not this outer one.");
        }
    }
}
