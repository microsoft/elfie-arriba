// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

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

    internal class AssertBuilder : IVerbBuilder
    {
        public string Verb => "assert";
        public string Usage => "assert {none|all}\r\n  {subquery}\r\n  end";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            return new AssertCommand(source,
                context.Parser.NextEnum<AssertType>(),
                context);
        }
    }

    public class AssertCommand : XTableWrapper
    {
        private AssertType _type;
        private SinglePageEnumerator _singlePageSource;
        private IXTable _assertPipeline;

        private long _sourceRowsTotal;
        private long _assertRowsTotal;

        public AssertCommand(IXTable source, AssertType type, XDatabaseContext context) : base(source)
        {
            _type = type;
            _singlePageSource = new SinglePageEnumerator(source);
            _assertPipeline = context.Parser.NextQuery(_singlePageSource);
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Get the next rows from the real source
            int count = _singlePageSource.SourceNext(desiredCount, cancellationToken);

            if (count > 0)
            {
                // Count source rows
                _sourceRowsTotal += count;

                // Make the asserts run and count matching rows
                _assertRowsTotal += _assertPipeline.Count();
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

    internal class AssertCountBuilder : IVerbBuilder
    {
        public string Verb => "assertCount";
        public string Usage => "assertCount {rowCount}";

        public IXTable Build(IXTable source, XDatabaseContext context)
        {
            return new AssertCountCommand(source,
                context.Parser.NextInteger(),
                context);
        }
    }

    public class AssertCountCommand : XTableWrapper
    {
        private int _actualCount;
        private int _expectedCount;
        private QueryDebuggingContext _debuggingContext;

        public AssertCountCommand(IXTable source, int count, XDatabaseContext context) : base(source)
        {
            _expectedCount = count;
            _debuggingContext = new QueryDebuggingContext(context);
        }

        public override int Next(int desiredCount, CancellationToken cancellationToken)
        {
            // Get the next rows from the real source
            int count = _source.Next(desiredCount, cancellationToken);
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
        public Position Position { get; private set; }

        public QueryDebuggingContext(XDatabaseContext context)
        {
            this.TableName = context.CurrentTable;
            this.Query = context.CurrentQuery;
            this.Position = context.Parser.CurrentPosition;
        }

        public override string ToString()
        {
            return $"Building {TableName}, @{Position}.\r\nTo Debug:\r\n{Query}";
        }
    }
}
