#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "Test.h"

#pragma unmanaged

__int64 BandwidthTestAVX256(__int64* set, int length)
{
	// Maximum Bandwidth Test: Just load 256 bits and do a single XOR.
	// 8M x 1,000   Book: x1: 16.7B, x2: 24.5B, x4: 29.8B. Beast: x1: 32.1B/s, x2: 51.6B, x4: 58.0B.
	__m256i accumulator = _mm256_set1_epi8(0);

	for (int i = 0; i < length; i += 4)
	{
		__m256i block = _mm256_loadu_si256((__m256i*)(&set[i]));
		accumulator = _mm256_xor_si256(accumulator, block);
	}

	unsigned __int64 mask = _mm256_movemask_epi8(accumulator);
	return _mm_popcnt_u64(mask);
}

__int64 BandwidthTestAVX128(__int64* set, int length)
{
	// Maximum Bandwidth Test: Just load 128 bits and do a single XOR.
	// 8M x 1,000   Book: x1: 13.5B, x2: 23.0B, x4: 21.7B. Beast: x1: 26.6B/s, x2: 45.2B, x4: 63.0B.
	__m128i accumulator = _mm_set1_epi8(0);

	for (int i = 0; i < length; i += 2)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[i]));
		accumulator = _mm_xor_si128(accumulator, block);
	}

	unsigned __int64 mask = _mm_movemask_epi8(accumulator);
	return _mm_popcnt_u64(mask);
}

#pragma managed
namespace V5
{
	__int64 Test::Bandwidth(array<Byte>^ values, int offset, int length)
	{
		if (offset + length > values->Length) throw gcnew IndexOutOfRangeException();
		pin_ptr<Byte> pValues = &values[offset];
		return BandwidthTestAVX256((__int64*)pValues, length / 8);
	}
}