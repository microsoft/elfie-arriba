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
            SuggestResult result = new SuggestResult();
            result.Query = partialXqlQuery;
            result.IsValid = false;

            // Determine whether the user is starting to type the next token (the last one is a newline, space, or function start)
            char lastQueryChar = ' ';
            if (!String.IsNullOrEmpty(partialXqlQuery)) lastQueryChar = partialXqlQuery[partialXqlQuery.Length - 1];
            bool userIsStartingNextToken = (Char.IsWhiteSpace(lastQueryChar) || lastQueryChar == '(');

            try
            {
                XDatabaseContext context = _xDatabaseContext;

                // Reset the as of date if requested
                if (asOfDate != default(DateTime) && asOfDate != _xDatabaseContext.RequestedAsOfDateTime)
                {
                    context = new XDatabaseContext(context) { RequestedAsOfDateTime = asOfDate };
                }

                // Parse the query as-is to see if it's valid
                IXTable pipeline = context.Query(partialXqlQuery);
                result.IsValid = true;

                // If the query is valid but the user is starting the next token, figure out what the token type is
                if (userIsStartingNextToken)
                {
                    pipeline = context.Query(partialXqlQuery + "?");
                }
            }
            catch (UsageException ex)
            {
                result.Context = ex.Context;

                // Don't show suggestions after the last token unless an explicit space, newline, or a '(' is at the end of the current query
                if (result.Context.InvalidTokenIndex >= partialXqlQuery.Length && !userIsStartingNextToken)
                {
                    result.Context.ValidValues = null;
                }
            }

            return result;
        }
    }
}
