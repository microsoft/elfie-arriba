// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using XForm.Data;

namespace XForm.Query
{
    /// <summary>
    ///  XForm Queries are a set of stages built by IPipelineStageBuilders.
    ///  Create an IPipelineStageBuilder and specify the verbs it supports to extend the language.
    /// </summary>
    public interface IPipelineStageBuilder
    {
        /// <summary>
        ///  Verb at the beginning of an XQL line which this builder constructs the command for.
        /// </summary>
        string Verb { get; }

        /// <summary>
        ///  Usage message to write out for this command if it isn't passed the right parameters
        /// </summary>
        string Usage { get; }

        /// <summary>
        ///  Method to build the stage given a source and the parser. This code calls parser methods
        ///  in order to read the arguments required for this stage.
        /// </summary>
        /// <param name="source">IDataSourceEnumerator so far in this pipeline</param>
        /// <param name="context">WorkflowContext to read arguments, get logger, and so on</param>
        /// <returns>IDataSourceEnumerator for the new stage</returns>
        IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context);
    }
}
