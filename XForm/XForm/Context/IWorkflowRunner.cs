// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using XForm.Data;

namespace XForm.Context
{
    /// <summary>
    ///  IWorkflowRunner implements a Database Model on top of individual query files
    ///  by causing build to rebuild tables from queries, get them from a remote cache,
    ///  or trigger actions to create them.
    /// </summary>
    public interface IWorkflowRunner
    {
        /// <summary>
        ///  Build the requested named table and return an IXTable for it.
        ///   This may:
        ///     - Return a BinaryTableReader for an already computed result.
        ///     - Return a pipeline to construct the result from dependencies.
        ///     - Return a pipeline to construct the result from original sources.
        ///     - Do the work to build the result and return a reader for it after it's computed.
        /// </summary>
        /// <param name="sourceName">The name of the Table, Config, or Query to build</param>
        /// <param name="context">XDatabaseContext to use for construction</param>
        /// <returns>IXTable which returns the rows from the desired source</returns>
        IXTable Build(string sourceName, XDatabaseContext context);

        /// <summary>
        ///  Write a new query to the storage location and register it as a source
        /// </summary>
        /// <param name="query"></param>
        /// <param name="saveToPath"></param>
        void Save(string query, string saveToPath);

        /// <summary>
        ///  Return a list of Source names known to this runner. These are returned
        ///  if a source isn't provided to 'read' in the syntax.
        /// </summary>
        IEnumerable<string> SourceNames { get; }
    }
}
