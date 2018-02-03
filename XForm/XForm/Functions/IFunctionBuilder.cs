// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;
using XForm.Query;

namespace XForm.Functions
{
    /// <summary>
    ///  XForm contains named functions
    ///  Create an IPipelineStageBuilder and specify the verbs it supports to extend the language.
    /// </summary>
    public interface IFunctionBuilder : IUsage, INamedBuilder
    {
        /// <summary>
        ///  Return Type of function, if constant. Return null if type matches a parameter.
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        ///  Method to build the function given a source and context.
        /// </summary>
        /// <param name="source">IDataSourceEnumerator so far in this pipeline</param>
        /// <param name="context">XDatabaseContext to read arguments, get logger, and so on</param>
        /// <returns>IXArrayFunction as configured in query</returns>
        IXColumn Build(IXTable source, XDatabaseContext context);
    }
}
