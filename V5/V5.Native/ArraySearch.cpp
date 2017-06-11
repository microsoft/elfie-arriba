#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "ArraySearch.h"

/*
	Set operations using SSE vector instructions, using the XMM (16 byte) registers.

	_mm_set1_epi8     - Load an XMM register with 16 copies of the passed byte.
	_mm_loadu_si128   - Load an XMM register with 16 bytes from the unaligned source.

	_mm_cmpgt_epi8    - Per-byte Greater Than: Set an XMM mask with all one bits where XMM1[i] > XMM2[i].
	_mm_movemask_epi8 - Set lower 16 bits based on whether each byte in an XMM mask is all one.
*/

#pragma unmanaged

extern "C" __declspec(dllexport) void AndWhereGreaterThanInternal(unsigned char* set, int length, unsigned char value, unsigned long long* matchVector)
{
	int i = 0;

	__m256i signedToUnsigned = _mm256_set1_epi8(-128);
	__m256i blockOfValue = _mm256_sub_epi8(_mm256_set1_epi8(value), signedToUnsigned);

	int blockLength = length - (length & 63);
	for (; i < blockLength; i += 64)
	{
		__m256i block1 = _mm256_loadu_si256((__m256i*)(&set[i]));
		block1 = _mm256_sub_epi8(block1, signedToUnsigned);

		__m256i matchMask1 = _mm256_cmpgt_epi8(block1, blockOfValue);
		unsigned int matchBits1 = _mm256_movemask_epi8(matchMask1);

		__m256i block2 = _mm256_loadu_si256((__m256i*)(&set[i + 32]));
		block2 = _mm256_sub_epi8(block2, signedToUnsigned);

		__m256i matchMask2 = _mm256_cmpgt_epi8(block2, blockOfValue);
		unsigned int matchBits2 = _mm256_movemask_epi8(matchMask2);

		unsigned long long result = matchBits2;
		result = result << 32;
		result |= matchBits1;

		matchVector[i >> 6] &= result;
	}

	// Match remaining values individually
	if ((length & 63) > 0)
	{
		unsigned long long last = 0;
		for (; i < length; ++i)
		{
			if (set[i] > value)
			{
				last |= ((unsigned long long)(1) << (i & 63));
			}
		}
		matchVector[length >> 6] &= last;
	}
}

extern "C" __declspec(dllexport) int CountInternal(unsigned long long* matchVector, int length)
{
	long long count = 0;

	for (int i = 0; i < length; ++i)
	{
		count += _mm_popcnt_u64(matchVector[i]);
	}

	return (int)count;
}

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

// 16M longs -> 270ms
// Adding _m_prefetch(base + (half >> 1)); _m_prefetch(base + half + (half >> 1)); made this slower.
extern "C" __declspec(dllexport) int BucketIndexInternal(long long* bucketMins, int bucketCount, long long value)
{
	// Binary search for the last value less than the search value [the bucket the value should go into]
	long long* base = bucketMins;

	int count = bucketCount;
	while (count > 1)
	{
		int half = count >> 1;
		base = (base[half] <= value ? &base[half] : base);
		count -= half;
	}

	int index = (int)(base - bucketMins);
	if (value < *base) return index - 1;
	return index;
}

extern "C" __declspec(dllexport) void BucketInternal(long long* values, int index, int length, long long* bucketMins, int bucketCount, unsigned char* rowBucketIndex)
{
	int end = index + length;
	for (int i = index; i < end; ++i)
	{
		rowBucketIndex[i] = BucketIndexInternal(bucketMins, bucketCount, values[i]);
	}
}

#pragma managed

void ArraySearch::AndWhereGreaterThan(array<Byte>^ set, Byte value, array<UInt64>^ matchVector)
{
	if (matchVector->Length * 64 < set->Length) return;

	pin_ptr<Byte> pSet = &set[0];
	pin_ptr<UInt64> pVector = &matchVector[0];
	AndWhereGreaterThanInternal(pSet, set->Length, value, pVector);
}

int ArraySearch::Count(array<UInt64>^ matchVector)
{
	pin_ptr<UInt64> pVector = &matchVector[0];
	return CountInternal(pVector, matchVector->Length);
}

void ArraySearch::Bucket(array<Int64>^ values, int index, int length, array<Int64>^ bucketMins, array<Byte>^ rowBucketIndex)
{
	if (values->Length < (index + length)) return;
	if (rowBucketIndex->Length < values->Length) return;

	pin_ptr<Int64> pValues = &values[0];
	pin_ptr<Int64> pBucketMins = &bucketMins[0];
	pin_ptr<Byte> pRowBucketIndex = &rowBucketIndex[0];
	BucketInternal(pValues, index, length, pBucketMins, bucketMins->Length, pRowBucketIndex);
}

int ArraySearch::BucketIndex(array<Int64>^ bucketMins, Int64 value)
{
	pin_ptr<Int64> pBucketMins = &bucketMins[0];
	return BucketIndexInternal(pBucketMins, bucketMins->Length, value);
}

