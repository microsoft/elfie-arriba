// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;
using XForm.Query;

namespace XForm.Functions
{
    /// <summary>
    ///  XForm contains named functions
    ///  Create an IPipelineStageBuilder and specify the verbs it supports to extend the language.
    /// </summary>
    public interface IFunctionBuilder : IUsage
    {
        /// <summary>
        ///  Function Name which this builder constructs.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///  Method to build the function given a source and context.
        /// </summary>
        /// <param name="source">IDataSourceEnumerator so far in this pipeline</param>
        /// <param name="context">WorkflowContext to read arguments, get logger, and so on</param>
        /// <returns>IDataBatchFunction as configured in query</returns>
        IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context);
    }
}
