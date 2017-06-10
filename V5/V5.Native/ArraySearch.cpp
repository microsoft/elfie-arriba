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

extern "C" __declspec(dllexport) void AndWhereGreaterThanInternal(signed char* set, int length, signed char value, unsigned long long* matchVector)
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

		matchVector[i >> 6] &= (unsigned long long)(matchBits2 << 32) | matchBits1;
	}

	// Match remaining values individually
	if ((length & 63) > 0)
	{
		unsigned long long last = 0;
		for (; i < length; ++i)
		{
			if (set[i] > value) last |= (1 << (i & 63));
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

#pragma managed

void ArraySearch::AndWhereGreaterThan(array<Byte>^ set, Byte value, array<UInt64>^ matchVector)
{
	if (matchVector->Length * 64 < set->Length) return;

	pin_ptr<Byte> pSet = &set[0];
	pin_ptr<UInt64> pVector = &matchVector[0];
	AndWhereGreaterThanInternal((SByte*)pSet, set->Length, (SByte)value, pVector);
}

int ArraySearch::Count(array<UInt64>^ matchVector)
{
	pin_ptr<UInt64> pVector = &matchVector[0];
	return CountInternal(pVector, matchVector->Length);
}