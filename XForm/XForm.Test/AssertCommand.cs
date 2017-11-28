using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
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
        public IEnumerable<string> Verbs => new string[] { "assert" };
        public string Usage => "'assert' (none|all)\r\n  {subquery}\r\n  end";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            return new AssertCommand(source,
                parser.NextEnum<AssertType>(),
                parser);
        }
    }

    public class AssertCommand : DataBatchEnumeratorWrapper
    {
        private AssertType _type;
        private SinglePageEnumerator _singlePageSource;
        private IDataBatchEnumerator _assertPipeline;

        private int _sourceRowsTotal;
        private int _assertRowsTotal;

        public AssertCommand(IDataBatchEnumerator source, AssertType type, PipelineParser parser) : base(source)
        {
            _type = type;
            _singlePageSource = new SinglePageEnumerator(source);
            _assertPipeline = parser.NextPipeline(_singlePageSource);
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
                _assertRowsTotal += _assertPipeline.Run();
            }
            else
            {
                // If we're done, validate the assert
                int expectedCount = (_type == AssertType.All ? _sourceRowsTotal : 0);
                Assert.AreEqual(expectedCount, _assertRowsTotal, "Pipeline Assert Failed");
            }

            return count;
        }
    }
}
