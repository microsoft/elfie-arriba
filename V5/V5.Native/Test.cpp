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

__int64 CompareAndCountAVX128(__int8* set, int length)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(14);
	__int64 count = 0;

	for (int i = 0; i < length; i += 16)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[i]));
		count += CompareAndCount(block, value);
	}

	return count;
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

template<int bitsPerValue, int start>
__m128i GetShuffleMask()
{
	__m128i shuffleMask;

	int itemIndex = start;
	for (int maskIndex = 0; maskIndex < 16; maskIndex += 2, itemIndex += 2)
	{
		// Item 'i' starts at bit (i * bitsPerValue).
		int bitIndex = itemIndex * bitsPerValue;

		// Item 'i' starts in byte (bitIndex / 8)
		unsigned __int8 firstByteWithBits = bitIndex / 8;

		// Get the byte with the first bit and the byte right afterward to shift
		shuffleMask.m128i_u8[maskIndex] = firstByteWithBits;
		shuffleMask.m128i_u8[maskIndex + 1] = firstByteWithBits + 1;
	}

	return shuffleMask;
}

template<int bitsPerValue, int start>
__m128i GetShiftMask()
{
	__m128i shiftMask;

	int itemIndex = start;
	for (int maskIndex = 0; maskIndex < 8; maskIndex++, itemIndex += 2)
	{
		// Item 'i' starts at bit (i * bitsPerValue).
		int bitIndex = itemIndex * bitsPerValue;

		// Item 'i' is (bitIndex % 8) bits into the byte
		unsigned __int8 offsetInByte = bitIndex % 8;

		// Shift 8 bits to get to bottom of high byte, so 8 - offsetInByte
		unsigned __int8 bitsToShift = 8 - offsetInByte;

		// To shift that many bits, multiply by (2^bitsToShift) or 1 << bitsToShift.
		unsigned __int16 multiplyBy = 1 << bitsToShift;
		shiftMask.m128i_u16[maskIndex] = multiplyBy;
	}

	return shiftMask;
}

// TODO: Need to figure out how to properly declare __m128i constants. This isn't working.
const __m128i AndMasks[] =
{
	{ 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000 },
	{ 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100 },
	{ 0x0300, 0x0300, 0x0300, 0x0300, 0x0300, 0x0300, 0x0300, 0x0300 },
	{ 0x0700, 0x0700, 0x0700, 0x0700, 0x0700, 0x0700, 0x0700, 0x0700 },
	{ 0x0F00, 0x0F00, 0x0F00, 0x0F00, 0x0F00, 0x0F00, 0x0F00, 0x0F00 },
	{ 0x1F00, 0x1F00, 0x1F00, 0x1F00, 0x1F00, 0x1F00, 0x1F00, 0x1F00 },
	{ 0x3F00, 0x3F00, 0x3F00, 0x3F00, 0x3F00, 0x3F00, 0x3F00, 0x3F00 },
	{ 0x7F00, 0x7F00, 0x7F00, 0x7F00, 0x7F00, 0x7F00, 0x7F00, 0x7F00 },
};

__m128i GetAndMask(int bitsPerValue)
{
	return _mm_set1_epi16((0x00FF << bitsPerValue) & 0xFF00);
}

template<int bitsPerValue>
__m128i __inline StretchTo8From(__m128i block)
{
	// 'Stretch' n-bit values so that each is in the low bits of a separate byte for comparison.
	// IN<3>: 0b...'nnnnnnnn'PPPOOONN'NMMMLLLK'KKJJJIII'HHHGGGFF'FEEEDDDC'CCBBBAAA
	// OUT  : 0b...'00000GGG'00000FFF'00000EEE'00000DDD'00000CCC'00000BBB'00000AAA

	// Use two 128-bit registers to expand alternating values (A, C, E, ...) and (B, D, F, ...)
	// Because the method is templated, computation of the masks will be done at compile time only.
	__m128i shuffleMask1 = _mm_set_epi16(0x0807, 0x0706, 0x0605, 0x0504, 0x0403, 0x0302, 0x0201, 0x0100); // GetShuffleMask<bitsPerValue, 0>();
	__m128i shuffleMask2 = _mm_set_epi16(0x0807, 0x0706, 0x0605, 0x0504, 0x0403, 0x0302, 0x0201, 0x0100); // GetShuffleMask<bitsPerValue, 1>();
	__m128i shiftMask1 = _mm_set_epi16(0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100); // GetShiftMask<bitsPerValue, 0>();
	__m128i shiftMask2 = _mm_set_epi16(0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010); // GetShiftMask<bitsPerValue, 1>();
	__m128i andMask = _mm_set1_epi16(0b00001111'00000000); // GetAndMask(bitsPerValue);

	// Use 'Shuffle' to get the two bytes containing the value into each 16-bit part.
	// R1: 0b...|HHHGGGFF'FEEEDDDC|FEEEDDDC'CCBBBAAA|FEEEDDDC'CCBBBAAA [A, C, E, ...]
	__m128i r1 = _mm_shuffle_epi8(block, shuffleMask1);
	__m128i r2 = _mm_shuffle_epi8(block, shuffleMask2);

	// Use 'Multiply Low' to shift the two byte value so the desired part is at the low edge of the high byte.
	// R1:  0b...|nnnnnEEE'nnnnnnnn|nnnnnCCC'nnnnnnnn|nnnnnAAA'nnnnnnnn
	r1 = _mm_mullo_epi16(r1, shiftMask1); 
	r2 = _mm_mullo_epi16(r2, shiftMask2);

	// AND with a mask to clear out the unused high bits and low byte.
	// R1:  0b...|00000EEE'00000000|00000CCC'00000000|00000AAA'00000000
	r1 = _mm_and_si128(r1, andMask);
	r2 = _mm_and_si128(r2, andMask);

	// Shift *one* register right by a whole byte to get those values into the low bytes
	// R1:  0b...|00000000'00000EEE|00000000'00000CCC|00000000'00000AAA
	r1 = _mm_srli_epi16(r1, 8);

	// OR the two registers together to merge the results
	// OUT: 0b...|00000FFF'00000EEE|00000DDD'00000CCC|00000BBB'00000AAA
	return _mm_or_si128(r1, r2);
}

__int64 CompareTestStretchAVX128(__int8* set, int bitsPerValue, int length)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(14);
	__int64 count = 0;

	int bytesPerBlock = (bitsPerValue * 16) / 8;
	int byteLength = (bytesPerBlock * length) / 16;
	for (int byteIndex = 0; byteIndex < byteLength; byteIndex += bytesPerBlock)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[byteIndex]));
		//block = StretchBits4to8(block);
		block = StretchTo8From<4>(block);
		count += CompareAndCount(block, value);
	}

	return count;
}

#pragma managed
namespace V5
{
	__int64 Test::Bandwidth(Scenario scenario, array<Byte>^ values, int bitsPerValue, int offset, int length)
	{
		int byteOffset = (offset * bitsPerValue) / 8;
		int byteLength = (length * bitsPerValue) / 8;

		if (byteOffset + byteLength > values->Length) throw gcnew IndexOutOfRangeException();
		pin_ptr<Byte> pValues = &values[byteOffset];

		switch (scenario)
		{
			case Scenario::BandwidthAVX256:
				return BandwidthTestAVX256((__int8*)pValues, length);
			case Scenario::BandwidthAVX128:
				return BandwidthTestAVX128((__int8*)pValues, length);
			case Scenario::CompareAndCountAVX128:
				return CompareAndCountAVX128((__int8*)pValues, length);
			default:
				throw gcnew NotImplementedException(scenario.ToString());
		}
		//return CompareTestStretchAVX128((__int8*)pValues, bitsPerValue, length);
	}
}