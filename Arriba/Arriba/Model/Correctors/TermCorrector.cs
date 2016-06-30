// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Arriba.Model.Expressions;

namespace Arriba.Model.Correctors
{
    /// <summary>
    ///  TermCorrector is the recommended base class for correctors which
    ///  will only replace terms. It uses the default traverser to traverse
    ///  the expression so that CorrectTerm will be called for each term.
    /// </summary>
    public abstract class TermCorrector : ICorrector
    {
        public IExpression Correct(IExpression expression)
        {
            // Default Implementation: Traverse and allow me to correct each term.
            return CorrectorTraverser.CorrectTerms(expression, this);
        }

        public abstract IExpression CorrectTerm(TermExpression te);
    }
}
