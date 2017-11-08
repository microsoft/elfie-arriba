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
            if (_countSoFar + desiredCount > _countLimit) desiredCount = _countLimit - _countSoFar;

            int sourceCount = _source.Next(desiredCount);
            _countSoFar += sourceCount;

            return sourceCount;
        }
    }
}
