// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Arriba.Model;
using Arriba.Structures;

namespace Arriba.Model.Expressions
{
    /// <summary>
    ///  IExpression represents any WHERE clause expression or component. 
    ///  It requires the ability to evaluate itself on a given partition,
    ///  adding matching items to a given ShortSet of results.
    /// </summary>
    public interface IExpression
    {
        /// <summary>
        ///  Add items matching this IExpression within the partition
        ///  to the result ShortSet.
        /// </summary>
        /// <param name="partition">Partition against which to match</param>
        /// <param name="result">ShortSet to add matches to</param>
        /// <param name="details">Details of execution</param>
        void TryEvaluate(Partition partition, ShortSet result, ExecutionDetails details);

        /// <summary>
        ///  Return children of this expression, if any. Must support setting
        ///  to replace children (for correctors).
        /// </summary>
        /// <returns>Children of this expression, empty if none</returns>
        IList<IExpression> Children();
    }
}
