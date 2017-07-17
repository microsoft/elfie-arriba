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

__int64 __inline CompareAndCount(__m128i block, __m128i value)
{
	__m128i mask = _mm_cmpgt_epi8(block, value);
	unsigned __int64 bits = _mm_movemask_epi8(mask);
	return _mm_popcnt_u64(bits);
}

__m128i __inline StretchBits4to8(__m128i block)
{
	// NOTE: This one is fast! 4x variations still at 59B out of 64B with no stretch and 50% overlap and 67B scan and XOR only.
	// Other bit-nesses will need either more registers, parallel multiply, or variable shift, all of which will likely be slower.
	// A general-purpose algorithm which would work for 2-7 would also be nice (template and look up from arrays with same steps).

	// IN:  0bnnnnnnnn'nnnnnnnn'nnnnnnnn'nnnnnnnn'HHHHGGGG'FFFFEEEE'DDDDCCCC'BBBBAAAA 
	// OUT: 0b0000HHHH'0000GGGG'0000FFFF'0000EEEE'0000DDDD'0000CCCC'0000BBBB'0000AAAA

	// First, stretch the bytes out so each byte contains the bits it needs to end up with
	//			     3        3        2        2        1        1        0        0			PSHUFB		P5 L1 T1
	//	R1: 0bHHHHGGGG'HHHHGGGG'FFFFEEEE'FFFFEEEE'DDDDCCCC'DDDDCCCC'BBBBAAAA'BBBBAAAA
	__m128i shuffleMask = _mm_set_epi8(7, 7, 6, 6, 5, 5, 4, 4, 3, 3, 2, 2, 1, 1, 0, 0);
	__m128i r1 = _mm_shuffle_epi8(block, shuffleMask);

	// In a copy, shift every word right four bits to align the alternating values
	//	R2: 0b0000HHHH'GGGGHHHH'0000FFFF'EEEEFFFF'0000DDDD'CCCCDDDD'0000BBBB'AAAABBBB			PSRLW		P01 L1 T0.5
	__m128i r2 = _mm_srli_epi16(r1, 4);

	// AND each value to get rid of the upper bits and get the correctly set bytes only
	//	   0b00000000'00001111'00000000'00001111'00000000'00001111'00000000'00001111			PAND		P015 L1 T0.33
	// R1: 0b00000000'0000GGGG'00000000'0000EEEE'00000000'0000CCCC'0000BBBB'0000AAAA
	r1 = _mm_and_si128(r1, _mm_set1_epi16(0b00000000'00001111));

	//	   0b00001111'00000000'00001111'00000000'00001111'00000000'00001111'00000000			PAND		P015 L1 T0.33
	// R2: 0b0000HHHH'00000000'0000FFFF'00000000'0000DDDD'00000000'0000BBBB'00000000
	r2 = _mm_and_si128(r2, _mm_set1_epi16(0b00001111'00000000));

	// Finally, OR together the two results
	// OUT: 0b0000HHHH'0000GGGG'0000FFFF'0000EEEE'0000DDDD'0000CCCC'0000BBBB'0000AAAA			POR			P015 L1 T0.33
	return _mm_or_si128(r1, r2);
}

__int64 CompareTestAVX128(__int8* set, int length)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(14);
	__int64 count = 0;

	for (int i = 0; i < length; i += 32)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[i >> 1]));
		block = StretchBits4to8(block);
		count += CompareAndCount(block, value);
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