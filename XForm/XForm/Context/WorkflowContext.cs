// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Context;
using XForm.Extensions;
using XForm.IO;
using XForm.IO.StreamProvider;
using XForm.Query;

namespace XForm
{
    /// <summary>
    ///  WorkflowContext wraps the different configuration interfaces used to customize
    ///  how XForm runs. These implement different Database Model rules, different source locations,
    ///  different logging, and so on.
    /// </summary>
    public class WorkflowContext
    {
        /// <summary>
        ///  IWorkflowRunner implements 'Build(sourceName)' which knows how to build a table with a given name.
        ///  Runners implement the DatabaseModel which know how to create tables from Config queries and dependencies.
        /// </summary>
        public IWorkflowRunner Runner { get; set; }

        /// <summary>
        ///  IStreamProvider provides streams for components to read. It wraps the file system and can enforce
        ///  compression, encryption, download from remote sources, and publishing.
        /// </summary>
        public IStreamProvider StreamProvider { get; set; }

        /// <summary>
        ///  ILogger implements a logging interface writing to the log for the table currently being built.
        ///  It tracks table success or failure and records table diagnostics to help understand failures or changes.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        ///  PipelineParser is the parser for the query for the current table being built.
        ///  It provides pipeline stages with arguments of specific types (column names, table names, integers, etc)
        /// </summary>
        public XqlParser Parser { get; set; }

        /// <summary>
        ///  CurrentTable is the current table name (Source, Config, or Query) being built
        /// </summary>
        public string CurrentTable { get; set; }

        /// <summary>
        ///  CurrentQuery is the XQL query the current table is built from
        /// </summary>
        public string CurrentQuery { get; set; }

        /// <summary>
        ///  RequestedAsOfDateTime is the moment in time for which reporting should be generated.
        ///  The latest available inputs before this moment should be used.
        ///  The report created is the exact right version from NewestDependency to (at least) RequestedAsOfDateTime.
        /// </summary>
        public DateTime RequestedAsOfDateTime { get; set; }

        /// <summary>
        ///  NewestDependency tracks the most recent dependency reporting DateTime and is used by WorkflowRunners
        ///  to determine the reporting DateTime the output should be stamped with.
        /// </summary>
        public DateTime NewestDependency { get; set; }

        /// <summary>
        ///  RebuiltSomething tracks whether a dependency for the current table needed to be rebuilt,
        ///  and determines whether cached outputs can be used or need to be recomputed.
        /// </summary>
        public bool RebuiltSomething { get; set; }

        public WorkflowContext()
        {
            this.RequestedAsOfDateTime = DateTime.UtcNow;
            this.NewestDependency = DateTime.MinValue;
            this.RebuiltSomething = false;

            this.StreamProvider = new LocalFileStreamProvider(Environment.CurrentDirectory);
            this.Runner = new SingleFileRunner();
        }

        public WorkflowContext(IWorkflowRunner runner, IStreamProvider streamProvider) : this()
        {
            this.Runner = runner;
            this.StreamProvider = streamProvider;
        }

        public WorkflowContext(WorkflowContext copyFrom) : this()
        {
            if (copyFrom != null)
            {
                this.Runner = copyFrom.Runner;
                this.StreamProvider = copyFrom.StreamProvider;
                this.Logger = copyFrom.Logger;
                this.Parser = copyFrom.Parser;
                this.CurrentTable = copyFrom.CurrentTable;
                this.CurrentQuery = copyFrom.CurrentQuery;
                this.RequestedAsOfDateTime = copyFrom.RequestedAsOfDateTime;
            }
        }

        public static WorkflowContext Push(WorkflowContext outer)
        {
            return new WorkflowContext(outer);
        }

        public void Pop(WorkflowContext outer)
        {
            if (outer != null)
            {
                outer.NewestDependency = outer.NewestDependency.BiggestOf(this.NewestDependency);
                outer.RebuiltSomething |= this.RebuiltSomething;
            }
        }
    }
}
