﻿using XForm.Data;

namespace XForm.Query
{
    public class SuggestResult
    {
        public string Query { get; set; }
        public bool IsValid { get; set; }
        public UsageException Usage { get; set; }
    }

    public class QuerySuggester
    {
        private WorkflowContext _workflowContext;

        public QuerySuggester(WorkflowContext workflowContext)
        {
            // *Copy* the WorkflowContext and replace the runner. Only supported on a real underlying WorkflowRunner
            _workflowContext = new WorkflowContext(workflowContext);
            _workflowContext.Runner = new DeferredRunner((WorkflowRunner)_workflowContext.Runner);
        }

        public SuggestResult Suggest(string partialXqlQuery)
        {
            SuggestResult result = new SuggestResult();
            result.Query = partialXqlQuery;
            result.IsValid = false;

            try
            {
                // Parse the query as-is to see if it's valid
                IDataBatchEnumerator pipeline = PipelineParser.BuildPipeline(partialXqlQuery, null, _workflowContext);
                result.IsValid = true;

                // Parse the query with an extra argument on the last line to see what would be suggested
                partialXqlQuery = partialXqlQuery + " \"\"";

                // Try building the query pipeline, using a *DeferredRunner* so dependencies aren't built right now
                pipeline = PipelineParser.BuildPipeline(partialXqlQuery, null, _workflowContext);
            }
            catch (UsageException ex)
            {
                result.Usage = ex;
            }

            return result;
        }
    }
}
