using XForm.Data;
using XForm.Transforms;

namespace XForm.Types.Comparers
{
    /// <summary>
    ///  Hard-coded IDataBatchComparer for float[].
    ///  NOTE: All hard coded comparers are copies of the same code.
    ///  They have to be copied because C# can't compile 'left == right' for generics.
    ///  If you change any of these, change all of them.
    /// </summary>
    internal class FloatComparer : IDataBatchComparer
    {
        public void WhereEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            float[] leftArray = (float[])left.Array;
            float[] rightArray = (float[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] == rightArray[rightIndex]) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] == rightArray[right.Index(i)]) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] == rightArray[i + leftIndexToRightIndex]) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                float rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] == rightValue) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] == rightArray[right.Selector.StartIndexInclusive])
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereNotEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            float[] leftArray = (float[])left.Array;
            float[] rightArray = (float[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] != rightArray[rightIndex]) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] != rightArray[right.Index(i)]) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] != rightArray[i + leftIndexToRightIndex]) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                float rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] != rightValue) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] != rightArray[right.Selector.StartIndexInclusive])
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereLessThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            float[] leftArray = (float[])left.Array;
            float[] rightArray = (float[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] < rightArray[rightIndex]) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] < rightArray[right.Index(i)]) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] < rightArray[i + leftIndexToRightIndex]) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                float rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] < rightValue) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] < rightArray[right.Selector.StartIndexInclusive])
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereLessThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            float[] leftArray = (float[])left.Array;
            float[] rightArray = (float[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] <= rightArray[rightIndex]) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] <= rightArray[right.Index(i)]) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] <= rightArray[i + leftIndexToRightIndex]) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                float rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] <= rightValue) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] <= rightArray[right.Selector.StartIndexInclusive])
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereGreaterThan(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            float[] leftArray = (float[])left.Array;
            float[] rightArray = (float[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] > rightArray[rightIndex]) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] > rightArray[right.Index(i)]) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] > rightArray[i + leftIndexToRightIndex]) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                float rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] > rightValue) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] > rightArray[right.Selector.StartIndexInclusive])
                {
                    result.All(left.Count);
                }
            }
        }

        public void WhereGreaterThanOrEquals(DataBatch left, DataBatch right, RowRemapper result)
        {
            result.ClearAndSize(left.Count);
            float[] leftArray = (float[])left.Array;
            float[] rightArray = (float[])right.Array;

            // Check how the DataBatches are configured and run the fastest loop possible for the configuration.
            if (left.IsNull != null || right.IsNull != null)
            {
                // Slowest Path: Null checks and look up indices on both sides. ~65ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    int leftIndex = left.Index(i);
                    int rightIndex = right.Index(i);
                    if (left.IsNull != null && left.IsNull[leftIndex]) continue;
                    if (right.IsNull != null && right.IsNull[rightIndex]) continue;
                    if (leftArray[leftIndex] >= rightArray[rightIndex]) result.Add(i);
                }
            }
            else if (left.Selector.Indices != null || right.Selector.Indices != null)
            {
                // Slow Path: Look up indices on both sides. ~55ms for 16M
                for (int i = 0; i < left.Count; ++i)
                {
                    if (leftArray[left.Index(i)] >= rightArray[right.Index(i)]) result.Add(i);
                }
            }
            else if (!right.Selector.IsSingleValue)
            {
                // Faster Path: Compare contiguous arrays. ~20ms for 16M
                int leftIndexToRightIndex = right.Selector.StartIndexInclusive - left.Selector.StartIndexInclusive;
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] >= rightArray[i + leftIndexToRightIndex]) result.Add(i);
                }
            }
            else if (!left.Selector.IsSingleValue)
            {
                // Fastest Path: Contiguous Array to constant. ~15ms for 16M
                float rightValue = rightArray[0];
                for (int i = left.Selector.StartIndexInclusive; i < left.Selector.EndIndexExclusive; ++i)
                {
                    if (leftArray[i] >= rightValue) result.Add(i);
                }
            }
            else
            {
                // Single Static comparison. ~0.7ms for 16M [called every 10,240 rows]
                if (leftArray[left.Selector.StartIndexInclusive] >= rightArray[right.Selector.StartIndexInclusive])
                {
                    result.All(left.Count);
                }
            }
        }
    }
}
