// 1.3s Managed [16M longs] -> 1.0s this.
extern "C" __declspec(dllexport) int BucketBranchyInternal(long long* bucketMins, int bucketCount, long long value)
{
	// Binary search for the last value less than the search value [the bucket the value should go into]
	int min = 0;
	int max = bucketCount - 1;
	long long midValue;

	while (min < max)
	{
		int mid = (min + max + 1) / 2;
		midValue = bucketMins[mid];

		if (value < midValue)
		{
			max = mid - 1;
		}
		else if (value > midValue)
		{
			min = mid;
		}
		else
		{
			return mid;
		}
	}

	if (value < bucketMins[max] && max > 0)
	{
		// If the value is smaller than the last bucket, we would insert before it
		return max - 1;
	}
	else
	{
		// Otherwise, this bucket is fine
		return max;
	}
}

int BucketParallelInternal(long long* bucketMins, int bucketCount, long long value)
{
	// Not faster and not quite right yet either.

	// Binary search for the last value less than the search value [the bucket the value should go into]
	__m256i* base = (__m256i*)bucketMins;
	__m256i bigValue = _mm256_set1_epi64x(value);
	int matchBits;

	int count = bucketCount >> 2;
	while (count > 1)
	{
		int half = count >> 1;

		// base = (base[half] <= value ? &base[half] : base);
		__m256i block = _mm256_loadu_si256(&base[half]);
		__m256i matchMask = _mm256_cmpgt_epi64(block, bigValue);
		matchBits = _mm256_movemask_epi8(matchMask);
		base = (matchBits == -1 ? base : &base[half]);

		count -= half;
	}

	int index = (int)(base - (__m256i*)bucketMins) << 2;
	unsigned int countGreaterInBlock = __lzcnt(~matchBits) >> 3;
	return index + 3 - countGreaterInBlock;
}

// Eytzinger, 16M longs -> ~350ms.
int BucketEytzingerInternal(long long* bucketMins, int bucketCount, long long value)
{
	int i = 0;
	while (i < bucketCount)
	{
		//_m_prefetch(bucketMins + 4 * i);
		i = (bucketMins[i] <= value ? (2 * i + 1) : (2 * i + 2));
	}

	return i;
}
