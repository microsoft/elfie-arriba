using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using XForm.Data;
using XForm.Extensions;
using XForm.IO;
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
        public string Usage => "'assert' (none|all) '(' subquery ')'";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, PipelineParser parser)
        {
            // TODO: Can I not pass the parser and still make the thing I'm constructing the head of the other pipeline?
            return new AssertCommand(source,
                parser.NextEnum<AssertType>(),
                parser);
        }
    }

    internal class SinglePageEnumerator : IDataBatchList
    {
        private IDataBatchEnumerator _source;

        private Func<DataBatch>[] _requestedGetters;
        private DataBatch[] _columnBatches;

        private int _currentPageCount;
        private ArraySelector _currentSelector;
        private ArraySelector _currentEnumerateSelector;

        public IReadOnlyList<ColumnDetails> Columns => _source.Columns;
        public int Count => _currentPageCount;

        public SinglePageEnumerator(IDataBatchEnumerator source)
        {
            _source = source;
            _requestedGetters = new Func<DataBatch>[source.Columns.Count];
            _columnBatches = new DataBatch[source.Columns.Count];
        }

        public int SourceNext(int desiredCount)
        {
            Reset();

            // Get a page from the real source
            _currentPageCount = _source.Next(desiredCount);

            // Call and cache DataBatches for all of the columns the caller has requested
            for(int i = 0; i < _requestedGetters.Length; ++i)
            {
                if(_requestedGetters[i] != null)
                {
                    _columnBatches[i] = _requestedGetters[i]();
                }
            }

            return _currentPageCount;
        }

        public Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Record that this column was requested
            if (_requestedGetters[columnIndex] == null) _requestedGetters[columnIndex] = _source.ColumnGetter(columnIndex);

            // Declare a remap array in case it's needed
            int[] remapArray = null;

            // Return the previously retrieved DataBatch for this page only
            return () =>
            {
                DataBatch raw = _columnBatches[columnIndex];
                return raw.Select(_currentSelector, ref remapArray);
            };
        }

        public void Reset()
        {
            // Reset enumeration over the cached single page
            _currentEnumerateSelector = ArraySelector.All(_currentPageCount).Slice(0, 0);
        }

        public int Next(int desiredCount)
        {
            // Iterate over the cached single page
            _currentEnumerateSelector = _currentEnumerateSelector.NextPage(_currentPageCount, desiredCount);
            _currentSelector = _currentEnumerateSelector;
            return _currentEnumerateSelector.Count;
        }

        public void Get(ArraySelector selector)
        {
            _currentSelector = selector;
        }

        public void Dispose()
        {
            // Don't actually dispose anything
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
