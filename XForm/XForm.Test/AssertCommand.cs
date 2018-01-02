// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Test
{
    public enum AssertType
    {
        None = 0,
        All = 1
    }

    internal class AssertBuilder : IPipelineStageBuilder
    {
        public string Verb => "assert";
        public string Usage => "'assert' (none|all)\r\n  {subquery}\r\n  end";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new AssertCommand(source,
                context.Parser.NextEnum<AssertType>(),
                context);
        }
    }

    public class AssertCommand : DataBatchEnumeratorWrapper
    {
        private AssertType _type;
        private SinglePageEnumerator _singlePageSource;
        private IDataBatchEnumerator _assertPipeline;

        private long _sourceRowsTotal;
        private long _assertRowsTotal;

        public AssertCommand(IDataBatchEnumerator source, AssertType type, WorkflowContext context) : base(source)
        {
            _type = type;
            _singlePageSource = new SinglePageEnumerator(source);
            _assertPipeline = context.Parser.NextPipeline(_singlePageSource);
        }

        public override int Next(int desiredCount)
        {
            // Get the next rows from the real source
            int count = _singlePageSource.SourceNext(desiredCount);

            if (count > 0)
            {
                // Count source rows
                _sourceRowsTotal += count;

                // Make the asserts run and count matching rows
                _assertRowsTotal += _assertPipeline.RunWithoutDispose();
            }
            else
            {
                // If we're done, validate the assert
                long expectedCount = (_type == AssertType.All ? _sourceRowsTotal : 0);
                Assert.AreEqual(expectedCount, _assertRowsTotal, "Pipeline Assert Failed");
            }

            return count;
        }
    }

    internal class AssertCountBuilder : IPipelineStageBuilder
    {
        public string Verb => "assertCount";
        public string Usage => "'assertCount' [rowCount]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new AssertCountCommand(source,
                context.Parser.NextInteger(),
                context);
        }
    }

    public class AssertCountCommand : DataBatchEnumeratorWrapper
    {
        private int _actualCount;
        private int _expectedCount;
        private QueryDebuggingContext _debuggingContext;

        public AssertCountCommand(IDataBatchEnumerator source, int count, WorkflowContext context) : base(source)
        {
            _expectedCount = count;
            _debuggingContext = new QueryDebuggingContext(context);
        }

        public override int Next(int desiredCount)
        {
            // Get the next rows from the real source
            int count = _source.Next(desiredCount);
            _actualCount += count;

            // When done, ensure the row count matches
            if (count == 0)
            {
                Assert.AreEqual(_expectedCount, _actualCount, $"\r\nassertCount {_expectedCount} failed\r\n{_debuggingContext}");
            }

            return count;
        }

        public override void Reset()
        {
            base.Reset();
            _actualCount = 0;
        }
    }

    public class QueryDebuggingContext
    {
        public string TableName { get; private set; }
        public string Query { get; private set; }
        public int QueryLineNumber { get; private set; }

        public QueryDebuggingContext(WorkflowContext context)
        {
            this.TableName = context.CurrentTable;
            this.Query = context.CurrentQuery;
            this.QueryLineNumber = context.Parser.CurrentLineNumber;
        }

        public override string ToString()
        {
            return $"Building {TableName}, line {QueryLineNumber}.\r\nTo Debug:\r\n{Query}";
        }
    }
}
