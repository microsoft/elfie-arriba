#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "Test.h"

#pragma unmanaged

__int64 BandwidthTestAVX256(__int8* set, int length)
{
	// Maximum Bandwidth Test: Just load 256 bits and do a single XOR.
	__m256i accumulator = _mm256_set1_epi8(0);

	for (int i = 0; i < length; i += 32)
	{
		__m256i block = _mm256_loadu_si256((__m256i*)(&set[i]));
		accumulator = _mm256_xor_si256(accumulator, block);
	}

	unsigned __int64 mask = _mm256_movemask_epi8(accumulator);
	return _mm_popcnt_u64(mask);
}

__int64 BandwidthTestAVX128(__int8* set, int length)
{
	// Maximum Bandwidth Test: Just load 128 bits and do a single XOR.
	__m128i accumulator = _mm_set1_epi8(0);

	for (int i = 0; i < length; i += 16)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[i]));
		accumulator = _mm_xor_si128(accumulator, block);
	}

	unsigned __int64 mask = _mm_movemask_epi8(accumulator);
	return _mm_popcnt_u64(mask);
}

__int64 CompareTestAVX128(__int8* set, int length)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(15);
	__m128i mask;
	__int64 count = 0;

	for (int i = 0; i < length; i += 16)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[i]));
		__m128i mask = _mm_cmpgt_epi8(block, value);
		unsigned __int64 bits = _mm_movemask_epi8(mask);
		count += _mm_popcnt_u64(bits);
	}

	return count;
}

#pragma managed
namespace V5
{
	__int64 Test::Bandwidth(array<Byte>^ values, int offset, int length)
	{
		if (offset + length > values->Length) throw gcnew IndexOutOfRangeException();
		pin_ptr<Byte> pValues = &values[offset];
		return CompareTestAVX128((__int8*)pValues, length);
	}
}