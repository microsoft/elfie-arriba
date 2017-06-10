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

extern "C" __declspec(dllexport) void AndWhereGreaterThanInternal(signed char* set, int length, signed char value, unsigned int* matchVector)
{
	int i = 0;

	// Match in 32 byte blocks
	__m128i signedToUnsigned = _mm_set1_epi8(0x80);
	__m128i blockOfValue = _mm_set1_epi8(value);
	blockOfValue = _mm_sub_epi8(blockOfValue, signedToUnsigned);

	int blockLength = length - (length & 31);
	for (; i < blockLength; i += 32)
	{
		__m128i block1 = _mm_loadu_si128((__m128i*)(&set[i]));
		block1 = _mm_sub_epi8(block1, signedToUnsigned);

		__m128i matchMask1 = _mm_cmpgt_epi8(block1, blockOfValue);
		int matchBits1 = _mm_movemask_epi8(matchMask1);

		__m128i block2 = _mm_loadu_si128((__m128i*)(&set[i + 16]));
		block2 = _mm_sub_epi8(block2, signedToUnsigned);

		__m128i matchMask2 = _mm_cmpgt_epi8(block2, blockOfValue);
		int matchBits2 = _mm_movemask_epi8(matchMask2);

		// Danny: _mm_stream_pd? Non-temporal write hints?
		matchVector[i >> 5] &= matchBits2 << 16 | matchBits1;
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

void ArraySearch::AndWhereGreaterThan(array<Byte>^ set, Byte value, array<UInt32>^ matchVector)
{
	if (matchVector->Length * 32 < set->Length) return;

	pin_ptr<Byte> pSet = &set[0];
	pin_ptr<UInt32> pVector = &matchVector[0];
	AndWhereGreaterThanInternal((SByte*)pSet, set->Length, (SByte)value, pVector);
}

int ArraySearch::Count(array<UInt32>^ matchVector)
{
	pin_ptr<UInt32> pVector = &matchVector[0];
	return CountInternal(pVector, matchVector->Length);
}