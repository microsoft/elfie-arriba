using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm
{
    public class WorkflowContext
    {
        public IWorkflowRunner Runner { get; set; }
        public DateTime NewestDependency { get; set; }
        public bool RebuiltSomething { get; set; }

        public WorkflowContext(IWorkflowRunner runner)
        {
            this.Runner = runner;
            this.NewestDependency = DateTime.MinValue;
            this.RebuiltSomething = false;
        }
    }

    public interface IWorkflowRunner
    {
        IDataBatchEnumerator Build(string sourceName, WorkflowContext context);
        IEnumerable<string> SourceNames { get; }
    }
}
