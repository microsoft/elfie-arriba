// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Query
{
    public class SuggestResult
    {
        public string Query { get; set; }
        public bool IsValid { get; set; }
        public ErrorContext Context { get; set; }
    }

    public class QuerySuggester
    {
        private XDatabaseContext _xDatabaseContext;

        public QuerySuggester(XDatabaseContext xDatabaseContext)
        {
            // *Copy* the XDatabaseContext and replace the runner. Only supported on a real underlying WorkflowRunner
            _xDatabaseContext = new XDatabaseContext(xDatabaseContext);
            _xDatabaseContext.Runner = new DeferredRunner((WorkflowRunner)_xDatabaseContext.Runner);
        }

        public SuggestResult Suggest(string partialXqlQuery, DateTime asOfDate = default(DateTime))
        {
            // Variables:
            //  - Is query valid before last token? If not, return invalid with that error position, message, and valid values.
            //  - Is there whitespace after the last token? If not, show information for the last token, not the next one.

            SuggestResult result = new SuggestResult();
            result.Query = partialXqlQuery;
            result.IsValid = false;

            XDatabaseContext context = _xDatabaseContext;
            if (asOfDate != default(DateTime) && asOfDate != _xDatabaseContext.RequestedAsOfDateTime)
            {
                context = new XDatabaseContext(context) { RequestedAsOfDateTime = asOfDate };
            }

            IXTable pipeline;

            // Add a 'nextTokenHint' suffix to ask for alternatives to the last token (if no trailing whitespace) or the next token (if there is)
            try
            {
                pipeline = context.Query(partialXqlQuery + "~");
            }
            catch (UsageException ex)
            {
                result.Context = ex.Context;
            }

            // Determine whether the query was valid as-is
            try
            {
                pipeline = context.Query(partialXqlQuery);

                // If it was valid, don't show an error message
                result.IsValid = true;

                if (result.Context != null)
                {
                    result.Context.ErrorMessage = "";
                }
            }
            catch (UsageException)
            { }

            return result;
        }

        private static bool LastTokenClosed(string query)
        {
            if (string.IsNullOrEmpty(query)) return true;

            char last = query[query.Length - 1];
            return (Char.IsWhiteSpace(last) || last == '(' || last == ']' || last == '"');
        }
    }
}
