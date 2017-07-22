#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "Test.h"

#pragma unmanaged

__int64 BandwidthTestAVX256(__int8* set, int bitsPerValue, int length)
{
	// Maximum Bandwidth Test: Just load 256 bits and do a single XOR.
	__m256i accumulator = _mm256_set1_epi8(0);

	int bytesPerBlock = (32 * bitsPerValue) / 8;
	int sourceIndex = 0;

	for (int rowIndex = 0; rowIndex < length; rowIndex += 32, sourceIndex += bytesPerBlock)
	{
		__m256i block = _mm256_loadu_si256((__m256i*)(&set[sourceIndex]));
		accumulator = _mm256_xor_si256(accumulator, block);
	}

	unsigned __int64 mask = _mm256_movemask_epi8(accumulator);
	return _mm_popcnt_u64(mask);
}

__int64 BandwidthTestAVX128(__int8* set, int bitsPerValue, int length)
{
	// Maximum Bandwidth Test: Just load 128 bits and do a single XOR.
	__m128i accumulator = _mm_set1_epi8(0);

	int bytesPerBlock = (16 * bitsPerValue) / 8;
	int sourceIndex = 0;

	for (int rowIndex = 0; rowIndex < length; rowIndex += 16, sourceIndex += bytesPerBlock)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[sourceIndex]));
		accumulator = _mm_xor_si128(accumulator, block);
	}

	unsigned __int64 mask = _mm_movemask_epi8(accumulator);
	return _mm_popcnt_u64(mask);
}

__int64 __inline CompareAndCount(__m128i block, __m128i value)
{
	__m128i mask = _mm_cmpgt_epi8(value, block);
	unsigned __int64 bits = _mm_movemask_epi8(mask);
	return _mm_popcnt_u64(bits);
}

__int64 CompareAndCountAVX128(__int8* set, int bitsPerValue, int length)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(1);
	__int64 count = 0;

	int bytesPerBlock = (16 * bitsPerValue) / 8;
	int sourceIndex = 0;

	for (int rowIndex = 0; rowIndex < length; rowIndex += 16, sourceIndex += bytesPerBlock)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[sourceIndex]));
		count += CompareAndCount(block, value);
	}

	return count;
}

__m128i __inline StretchBits4to8_V2(__m128i block)
{
	__m128i shuffleMask = _mm_set_epi8(7, 7, 6, 6, 5, 5, 4, 4, 3, 3, 2, 2, 1, 1, 0, 0);
	__m128i andMask = _mm_set1_epi16(0b11110000'00001111);

	// IN:  0bnnnnnnnn'nnnnnnnn'nnnnnnnn'nnnnnnnn'HHHHGGGG'FFFFEEEE'DDDDCCCC'BBBBAAAA 
	// OUT: 0b0000HHHH'0000GGGG'0000FFFF'0000EEEE'0000DDDD'0000CCCC'0000BBBB'0000AAAA

	// First, stretch the bytes out so each byte contains the bits it needs to end up with
	//			     3        3        2        2        1        1        0        0			PSHUFB		P5 L1 T1
	//	R1: 0bHHHHGGGG'HHHHGGGG'FFFFEEEE'FFFFEEEE'DDDDCCCC'DDDDCCCC'BBBBAAAA'BBBBAAAA
	__m128i r1 = _mm_shuffle_epi8(block, shuffleMask);

	// AND to get only the desired bits in each byte
	//	R1: 0bHHHH0000'0000GGGG'FFFF0000'0000EEEE'DDDD0000'0000CCCC'BBBB0000'0000AAAA			PAND		P015 L1 T0.33
	r1 = _mm_and_si128(r1, andMask);

	// In a copy, shift every word right four bits to align the high values and empty the low copies
	//	R1: 0b0000HHHH'00000000'0000FFFF'00000000'0000DDDD'00000000'0000BBBB'00000000 			PSRLW		P01 L1 T0.5
	__m128i r2 = _mm_srli_epi16(r1, 4);

	// Finally, OR together the two results
	// WRONG: 0bHHHHHHHH
	// OUT: 0b0000HHHH'0000GGGG'0000FFFF'0000EEEE'0000DDDD'0000CCCC'0000BBBB'0000AAAA			POR			P015 L1 T0.33
	return _mm_or_si128(r1, r2);
}

__m128i __inline StretchBits4to8(__m128i block)
{
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
	__m128i and1 = _mm_set1_epi16(0b00000000'00001111);
	r1 = _mm_and_si128(r1, and1);

	//	   0b00001111'00000000'00001111'00000000'00001111'00000000'00001111'00000000			PAND		P015 L1 T0.33
	// R2: 0b0000HHHH'00000000'0000FFFF'00000000'0000DDDD'00000000'0000BBBB'00000000
	__m128i and2 = _mm_set1_epi16(0b00001111'00000000);
	r2 = _mm_and_si128(r2, and2);

	// Finally, OR together the two results
	// OUT: 0b0000HHHH'0000GGGG'0000FFFF'0000EEEE'0000DDDD'0000CCCC'0000BBBB'0000AAAA			POR			P015 L1 T0.33
	return _mm_or_si128(r1, r2);
}

__int64 Stretch2to8CompareAndCountAVX128(__int8* set, int bitsPerValue, int length)
{
	// Get four copies of every byte
	__m128i shuffleMask = _mm_set_epi8(3, 3, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0);

	// Get only the "in use" two bits for each byte
	__m128i andMask = _mm_set1_epi32(0b11000000'00110000'00001100'00000011);

	// Get copies of the desired value shifted two bits each
	__m128i value = _mm_set_epi8(1 << 6, 1 << 4, 1 << 2, 1, 1 << 6, 1 << 4, 1 << 2, 1, 1 << 6, 1 << 4, 1 << 2, 1, 1 << 6, 1 << 4, 1 << 2, 1);

	// Subtract only the values which use the high bit
	__m128i subtract = _mm_set_epi8(128, 0, 0, 0, 128, 0, 0, 0, 128, 0, 0, 0, 128, 0, 0, 0);
	value = _mm_sub_epi8(value, subtract);

	__int64 count = 0;

	int bytesPerBlock = (bitsPerValue * 16) / 8;
	int byteLength = (bytesPerBlock * length) / 16;
	for (int byteIndex = 0; byteIndex < byteLength; byteIndex += bytesPerBlock)
	{
		// Load the next block to compare
		__m128i block = _mm_loadu_si128((__m128i*)(&set[byteIndex]));

		// Get four copies of every byte
		block = _mm_shuffle_epi8(block, shuffleMask);

		// Get only the "in use" two bits for each byte
		block = _mm_and_si128(block, andMask);
		
		// Subtract only the values using the high bit
		block = _mm_sub_epi8(block, subtract);

		// Compare and count the results
		count += CompareAndCount(block, value);
	}

	return count;
}

__int64 Stretch4to8CompareAndCountAVX128(__int8* set, int bitsPerValue, int length)
{
	__m128i shuffleMask = _mm_set_epi8(7, 7, 6, 6, 5, 5, 4, 4, 3, 3, 2, 2, 1, 1, 0, 0);
	__m128i and1 = _mm_set1_epi16(0b00000000'00001111);
	__m128i and2 = _mm_set1_epi16(0b00001111'00000000);

	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(1);
	__int64 count = 0;

	int bytesPerBlock = (bitsPerValue * 16) / 8;
	int byteLength = (bytesPerBlock * length) / 16;
	for (int byteIndex = 0; byteIndex < byteLength; byteIndex += bytesPerBlock)
	{
		// Load the next block to compare
		__m128i block = _mm_loadu_si128((__m128i*)(&set[byteIndex]));

		// Stretch four bit values to 8 [really inlined]
		__m128i r1 = _mm_shuffle_epi8(block, shuffleMask);
		__m128i r2 = _mm_srli_epi16(r1, 4);
		r1 = _mm_and_si128(r1, and1);
		r2 = _mm_and_si128(r2, and2);
		block = _mm_or_si128(r1, r2);

		count += CompareAndCount(block, value);
	}

	return count;
}

__m128i GetShuffleMask(int bitsPerValue, int start)
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

__m128i GetShiftMask(int bitsPerValue, int start)
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

__int64 StretchGenericCompareAndCountAVX128(__int8* set, int bitsPerValue, int length)
{
	__m128i shuffleMask1 = GetShuffleMask(bitsPerValue, 0);
	__m128i shuffleMask2 = GetShuffleMask(bitsPerValue, 1);
	__m128i shiftMask1 = GetShiftMask(bitsPerValue, 0);
	__m128i shiftMask2 = GetShiftMask(bitsPerValue, 1);
	__m128i andMask = GetAndMask(bitsPerValue);

	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(1);
	__int64 count = 0;

	int bytesPerBlock = (bitsPerValue * 16) / 8;
	int byteLength = (bytesPerBlock * length) / 16;
	for (int byteIndex = 0; byteIndex < byteLength; byteIndex += bytesPerBlock)
	{
		// Load the next block to compare
		__m128i block = _mm_loadu_si128((__m128i*)(&set[byteIndex]));

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
		block = _mm_or_si128(r1, r2);

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
				return BandwidthTestAVX256((__int8*)pValues, bitsPerValue, length);
			case Scenario::BandwidthAVX128:
				return BandwidthTestAVX128((__int8*)pValues, bitsPerValue, length);
			case Scenario::CompareAndCountAVX128:
				return CompareAndCountAVX128((__int8*)pValues, bitsPerValue, length);
			case Scenario::Stretch4to8CompareAndCountAVX128:
				return Stretch4to8CompareAndCountAVX128((__int8*)pValues, bitsPerValue, length);
			case Scenario::StretchGenericCompareAndCountAVX128:
				return StretchGenericCompareAndCountAVX128((__int8*)pValues, bitsPerValue, length);
			case Scenario::Stretch2to8CompareAndCountAVX128:
				return Stretch2to8CompareAndCountAVX128((__int8*)pValues, bitsPerValue, length);
			default:
				throw gcnew NotImplementedException(scenario.ToString());
		}
	}
}