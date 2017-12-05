// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Data;
using XForm.IO;
using XForm.Query;

namespace XForm
{
    public class WorkflowContext
    {
        public IWorkflowRunner Runner { get; set; }
        public IStreamProvider StreamProvider { get; set; }
        public ILogger Logger { get; set; }
        public PipelineParser Parser { get; set; }

        public DateTime NewestDependency { get; set; }
        public bool RebuiltSomething { get; set; }

        public WorkflowContext()
        {
            this.NewestDependency = DateTime.MinValue;
            this.RebuiltSomething = false;
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
            }
        }
    }

    public interface IWorkflowRunner
    {
        IDataBatchEnumerator Build(string sourceName, WorkflowContext context);
        IEnumerable<string> SourceNames { get; }
    }
}
