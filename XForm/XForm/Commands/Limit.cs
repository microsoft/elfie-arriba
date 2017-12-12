// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;
using XForm.Query;

namespace XForm.Commands
{
    internal class LimitCommandBuilder : IPipelineStageBuilder
    {
        public string Verb => "limit";
        public string Usage => "'limit' [RowCount]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            int limit = context.Parser.NextInteger();
            return new Limit(source, limit);
        }
    }

    public class Limit : DataBatchEnumeratorWrapper
    {
        private int _countLimit;
        private int _countSoFar;

        public Limit(IDataBatchEnumerator source, int countLimit) : base(source)
        {
            _countLimit = countLimit;
        }

        public override void Reset()
        {
            base.Reset();
            _countSoFar = 0;
        }

        public override int Next(int desiredCount)
        {
            if (_countSoFar >= _countLimit) return 0;
            if (_countSoFar + desiredCount > _countLimit) desiredCount = _countLimit - _countSoFar;

            int sourceCount = _source.Next(desiredCount);
            _countSoFar += sourceCount;

            return sourceCount;
        }
    }
}
