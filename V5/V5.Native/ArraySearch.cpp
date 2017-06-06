#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "ArraySearch.h"

#pragma unmanaged

extern "C" __declspec(dllexport) void WhereGreaterThanInternal(signed char* set, int length, signed char value, unsigned int* matchVector)
{
	int i = 0;

	// Match in 32 byte blocks
	__m128i blockOfValue = _mm_set1_epi8(value);
	int blockLength = length >> 5;
	for (; i < blockLength; ++i)
	{
		__m128i block1 = _mm_loadu_si128((__m128i*)(&set[i << 5]));
		__m128i matchMask1 = _mm_cmpgt_epi8(block1, blockOfValue);
		int matchBits1 = _mm_movemask_epi8(matchMask1);

		__m128i block2 = _mm_loadu_si128((__m128i*)(&set[(i << 5) + 16]));
		__m128i matchMask2 = _mm_cmpgt_epi8(block2, blockOfValue);
		int matchBits2 = _mm_movemask_epi8(matchMask2);

		matchVector[i] = matchBits2 << 16 | matchBits1;
	}

	// Match remaining values individually
	for (i = i << 5; i < length; ++i)
	{
		if (set[i] > value)
		{
			matchVector[i >> 5] |= (1 << (i & 31));
		}
	}
}

extern "C" __declspec(dllexport) int CountInternal(unsigned int* matchVector, int length)
{
	int count = 0;

	for (int i = 0; i < length; ++i)
	{
		count += _mm_popcnt_u32(matchVector[i]);
	}

	return count;
}

#pragma managed

void ArraySearch::WhereGreaterThan(array<Byte>^ set, SByte value, array<UInt32>^ matchVector)
{
	if (matchVector->Length * 32 < set->Length) return;

	pin_ptr<Byte> pSet = &set[0];
	pin_ptr<UInt32> pVector = &matchVector[0];
	WhereGreaterThanInternal((SByte*)pSet, set->Length, value, pVector);
}

int ArraySearch::Count(array<UInt32>^ matchVector)
{
	pin_ptr<UInt32> pVector = &matchVector[0];
	return CountInternal(pVector, matchVector->Length);
}