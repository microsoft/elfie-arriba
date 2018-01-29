// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;

namespace XForm.Query
{
    /// <summary>
    ///  XForm Queries are a set of stages built by IPipelineStageBuilders.
    ///  Create an IPipelineStageBuilder and specify the verbs it supports to extend the language.
    /// </summary>
    public interface IVerbBuilder : IUsage
    {
        /// <summary>
        ///  Verb at the beginning of an XQL line which this builder constructs the command for.
        /// </summary>
        string Verb { get; }

        /// <summary>
        ///  Method to build the stage given a source and context. This code calls parser methods
        ///  in order to read the arguments required for this stage.
        /// </summary>
        /// <param name="source">IDataSourceEnumerator so far in this pipeline</param>
        /// <param name="context">XDatabaseContext to read arguments, get logger, and so on</param>
        /// <returns>IDataSourceEnumerator for the new stage</returns>
        IXTable Build(IXTable source, XDatabaseContext context);
    }
}
