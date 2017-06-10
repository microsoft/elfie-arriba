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

	// Match in 32 byte blocks
	__m128i signedToUnsigned = _mm_set1_epi8(-128);
	__m128i blockOfValue = _mm_set1_epi8(value);
	blockOfValue = _mm_sub_epi8(blockOfValue, signedToUnsigned);

	int blockLength = length - (length & 63);
	for (; i < blockLength; i += 64)
	{
		__m128i block1 = _mm_loadu_si128((__m128i*)(&set[i]));
		block1 = _mm_sub_epi8(block1, signedToUnsigned);

		__m128i matchMask1 = _mm_cmpgt_epi8(block1, blockOfValue);
		int matchBits1 = _mm_movemask_epi8(matchMask1);

		__m128i block2 = _mm_loadu_si128((__m128i*)(&set[i + 16]));
		block2 = _mm_sub_epi8(block2, signedToUnsigned);

		__m128i matchMask2 = _mm_cmpgt_epi8(block2, blockOfValue);
		int matchBits2 = _mm_movemask_epi8(matchMask2);

		__m128i block3 = _mm_loadu_si128((__m128i*)(&set[i + 32]));
		block3 = _mm_sub_epi8(block3, signedToUnsigned);

		__m128i matchMask3 = _mm_cmpgt_epi8(block3, blockOfValue);
		int matchBits3 = _mm_movemask_epi8(matchMask3);

		__m128i block4 = _mm_loadu_si128((__m128i*)(&set[i + 48]));
		block4 = _mm_sub_epi8(block4, signedToUnsigned);

		__m128i matchMask4 = _mm_cmpgt_epi8(block4, blockOfValue);
		int matchBits4 = _mm_movemask_epi8(matchMask4);

		unsigned long long result;
		result = matchBits4;
		result = result << 16;
		result |= matchBits3;
		result = result << 16;
		result |= matchBits2;
		result = result << 16;
		result |= matchBits1;

		matchVector[i >> 6] &= result;
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