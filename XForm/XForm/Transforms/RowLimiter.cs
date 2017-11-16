// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using XForm.Data;

namespace XForm.Transforms
{
    public class RowLimiter : DataBatchEnumeratorWrapper
    {
        private int _countLimit;
        private int _countSoFar;

        public RowLimiter(IDataBatchEnumerator source, int countLimit) : base(source)
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
