#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "Test.h"

#pragma unmanaged
__int64 CountN(unsigned __int64* matchVector, int length)
{
	__int64 count = 0;
	
	for (int i = 0; i < length; ++i)
	{
		count += _mm_popcnt_u64(matchVector[i]);
	}

	return count;
}

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
		__m128i mask = _mm_cmpgt_epi8(value, block);
		unsigned __int64 bits = _mm_movemask_epi8(mask);
		count += _mm_popcnt_u64(bits);
	}

	return count;
}

void CompareToVectorTwoByteAVX128(__int8* set, int bitsPerValue, int length, __int8* vector)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi16(1);
	__m128i shuffleMask = _mm_set_epi8(-1, -1, -1, -1, -1, -1, -1, -1, 14, 12, 10, 8, 6, 4, 2, 0);

	int bytesPerBlock = (8 * bitsPerValue) / 8;
	int blockLength = length / 8;
	int sourceIndex = 0;

	for (int blockIndex = 0; blockIndex < blockLength; ++blockIndex, sourceIndex += bytesPerBlock)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[sourceIndex]));
		__m128i shortMask = _mm_cmpgt_epi16(value, block);
		__m128i mask = _mm_shuffle_epi8(shortMask, shuffleMask);
		__int8 bits = (__int8)(_mm_movemask_epi8(mask) & 0xFF);
		vector[blockIndex] = bits;
	}
}

void CompareToVectorAVX256(__int8* set, int bitsPerValue, int length, __int32* vector)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m256i value = _mm256_set1_epi8(1);

	int bytesPerBlock = (32 * bitsPerValue) / 8;
	int blockLength = length / 32;
	int sourceIndex = 0;

	for (int blockIndex = 0; blockIndex < blockLength; ++blockIndex, sourceIndex += bytesPerBlock)
	{
		__m256i block = _mm256_loadu_si256((__m256i*)(&set[sourceIndex]));
		__m256i mask = _mm256_cmpgt_epi8(value, block);
		__int32 bits = (__int32)(_mm256_movemask_epi8(mask) & 0xFFFFFFFF);
		vector[blockIndex] = bits;
	}
}

void CompareToVectorAVX128(__int8* set, int bitsPerValue, int length, __int16* vector)
{
	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(1);

	int bytesPerBlock = (16 * bitsPerValue) / 8;
	int blockLength = length / 16;
	int sourceIndex = 0;

	for (int blockIndex = 0; blockIndex < blockLength; ++blockIndex, sourceIndex += bytesPerBlock)
	{
		__m128i block = _mm_loadu_si128((__m128i*)(&set[sourceIndex]));
		__m128i mask = _mm_cmpgt_epi8(value, block);
		__int16 bits = (__int16)(_mm_movemask_epi8(mask) & 0xFFFF);
		vector[blockIndex] = bits;
	}
}

void Stretch4to8CompareToVectorAVX128(__int8* set, int bitsPerValue, int length, __int16* vector)
{
	__m128i shuffleMask = _mm_set_epi8(7, 7, 6, 6, 5, 5, 4, 4, 3, 3, 2, 2, 1, 1, 0, 0);
	__m128i and1 = _mm_set1_epi16(0b00000000'00001111);
	__m128i and2 = _mm_set1_epi16(0b00001111'00000000);

	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(1);
	__int64 count = 0;

	int bytesPerBlock = (bitsPerValue * 16) / 8;
	int byteLength = (bytesPerBlock * length) / 16;
	for (int byteIndex = 0, blockIndex = 0; byteIndex < byteLength; byteIndex += bytesPerBlock, blockIndex++)
	{
		// Load the next block to compare
		__m128i block = _mm_loadu_si128((__m128i*)(&set[byteIndex]));

		// Stretch four bit values to 8 [really inlined]
		//	R1: 0bHHHHGGGG'HHHHGGGG'FFFFEEEE'FFFFEEEE'DDDDCCCC'DDDDCCCC'BBBBAAAA'BBBBAAAA
		__m128i r1 = _mm_shuffle_epi8(block, shuffleMask);

		// In a copy, shift every word right four bits to align the alternating values
		//	R2: 0b0000HHHH'GGGGHHHH'0000FFFF'EEEEFFFF'0000DDDD'CCCCDDDD'0000BBBB'AAAABBBB			PSRLW		P01 L1 T0.5
		__m128i r2 = _mm_srli_epi16(r1, 4);

		// AND each value to get rid of the upper bits and get the correctly set bytes only
		//	   0b00000000'00001111'00000000'00001111'00000000'00001111'00000000'00001111			PAND		P015 L1 T0.33
		// R1: 0b00000000'0000GGGG'00000000'0000EEEE'00000000'0000CCCC'0000BBBB'0000AAAA
		r1 = _mm_and_si128(r1, and1);
		r2 = _mm_and_si128(r2, and2);

		// Finally, OR together the two results
		// OUT: 0b0000HHHH'0000GGGG'0000FFFF'0000EEEE'0000DDDD'0000CCCC'0000BBBB'0000AAAA			POR			P015 L1 T0.33
		block = _mm_or_si128(r1, r2);

		__m128i mask = _mm_cmpgt_epi8(value, block);
		__int16 bits = (__int16)(_mm_movemask_epi8(mask) & 0xFFFF);
		vector[blockIndex] = bits;
	}
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
		unsigned __int8 firstByteToGet = bitIndex / 8;

		// If item 'i' starts at the very first bit, get an earlier byte
		if (bitIndex % 8 == 0) firstByteToGet--;

		// Get two adjacent bytes containing the bits, but not at the first position
		shuffleMask.m128i_u8[maskIndex] = firstByteToGet;
		shuffleMask.m128i_u8[maskIndex + 1] = firstByteToGet + 1;
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
		if (offsetInByte == 0) offsetInByte += 8;

		// Shift even items to the beginning of the third byte, odd items to the beginning of the second byte
		unsigned __int8 bitsToShift = (start == 0 ? 16 - offsetInByte : 8 - offsetInByte);

		// To shift that many bits, multiply by (2^bitsToShift) or 1 << bitsToShift.
		unsigned __int16 multiplyBy = 1 << bitsToShift;
		shiftMask.m128i_u16[maskIndex] = multiplyBy;
	}

	return shiftMask;
}

__m128i GetAndMask(int bitsPerValue, int start)
{
	unsigned __int8 mask = (0xFF >> (8 - bitsPerValue));

	if (start == 0)
	{
		return _mm_set_epi8(0, mask, 0, mask, 0, mask, 0, mask, 0, mask, 0, mask, 0, mask, 0, mask);
	}
	else
	{
		return _mm_set_epi8(mask, 0, mask, 0, mask, 0, mask, 0, mask, 0, mask, 0, mask, 0, mask, 0);
	}
}

void StretchGenericCompareToVectorAVX128(__int8* set, int bitsPerValue, int length, __int16* vector)
{
	__m128i shuffleMask1 = GetShuffleMask(bitsPerValue, 0);
	__m128i shuffleMask2 = GetShuffleMask(bitsPerValue, 1);
	__m128i shiftMask1 = GetShiftMask(bitsPerValue, 0);
	__m128i shiftMask2 = GetShiftMask(bitsPerValue, 1);
	__m128i andMask1 = GetAndMask(bitsPerValue, 0);
	__m128i andMask2 = GetAndMask(bitsPerValue, 1);

	// Minimal Compare: Load, Compare, MoveMask, PopCount, add
	__m128i value = _mm_set1_epi8(1);

	int bytesPerBlock = (bitsPerValue * 16) / 8;
	int byteLength = (bytesPerBlock * length) / 16;
	for (int byteIndex = 0, blockIndex = 0; byteIndex < byteLength; byteIndex += bytesPerBlock, blockIndex++)
	{
		// Load the next block to compare
		__m128i block = _mm_loadu_si128((__m128i*)(&set[byteIndex]));

		// Use 'Shuffle' to get the two bytes containing the value into each 16-bit part.
		// R1: 0b...|HHHGGGFF'FEEEDDDC|FEEEDDDC'CCBBBAAA|CCBBBAAA'nnnnnnnn [A, C, E, ...]
		// R1: 0b...|HHHGGGFF'FEEEDDDC|HHHGGGFF'FEEEDDDC|FEEEDDDC'CCBBBAAA [B, D, F, ...]
		__m128i r1 = _mm_shuffle_epi8(block, shuffleMask1);
		__m128i r2 = _mm_shuffle_epi8(block, shuffleMask2);

		// Use 'Multiply' to get even items to the low byte and odd items to the high byte.
		// R1:  0b...|nnnnnnnn'nnnnnEEE|nnnnnnnn'nnnnnCCC|nnnnnnnn'nnnnnAAA
		// R2:  0b...|nnnnnFFF'nnnnnnnn|nnnnnDDD'nnnnnnnn|nnnnnBBB'nnnnnnnn
		r1 = _mm_mulhi_epi16(r1, shiftMask1);
		r2 = _mm_mullo_epi16(r2, shiftMask2);

		// AND with a mask to clear out the unused high bits and low byte.
		// R1:  0b...|00000EEE'00000000|00000CCC'00000000|00000AAA'00000000
		r1 = _mm_and_si128(r1, andMask1);
		r2 = _mm_and_si128(r2, andMask2);

		// OR the two registers together to merge the results
		// OUT: 0b...|00000FFF'00000EEE|00000DDD'00000CCC|00000BBB'00000AAA
		block = _mm_or_si128(r1, r2);

		__m128i mask = _mm_cmpgt_epi8(value, block);
		__int16 bits = (__int16)(_mm_movemask_epi8(mask) & 0xFFFF);
		vector[blockIndex] = bits;
	}
}

#pragma managed
namespace V5
{
	__int64 Test::Count(array<UInt64>^ vector)
	{
		pin_ptr<UInt64> pVector = &vector[0];
		return CountN((unsigned __int64*)pVector, vector->Length);
	}

	__int64 Test::Bandwidth(Scenario scenario, array<Byte>^ values, int bitsPerValue, int offset, int length, array<UInt64>^ vector)
	{
		int byteOffset = (offset * bitsPerValue) / 8;
		int byteLength = (length * bitsPerValue) / 8;

		if (byteOffset + byteLength > values->Length) throw gcnew IndexOutOfRangeException();
		pin_ptr<Byte> pValues = &values[byteOffset];
		pin_ptr<UInt64> pVector = &vector[offset / 64];

		switch (scenario)
		{
			case Scenario::BandwidthAVX256:
				return BandwidthTestAVX256((__int8*)pValues, bitsPerValue, length);
			case Scenario::BandwidthAVX128:
				return BandwidthTestAVX128((__int8*)pValues, bitsPerValue, length);
			case Scenario::CompareToVectorAVX256:
				CompareToVectorAVX256((__int8*)pValues, bitsPerValue, length, (__int32*)pVector);
				return 0;
			case Scenario::CompareToVectorAVX128:
				CompareToVectorAVX128((__int8*)pValues, bitsPerValue, length, (__int16*)pVector);
				return 0;
			case Scenario::CompareToVectorTwoByteAVX128:
				CompareToVectorTwoByteAVX128((__int8*)pValues, bitsPerValue, length, (__int8*)pVector);
				return 0;
			case Scenario::Stretch4to8CompareToVectorAVX128:
				Stretch4to8CompareToVectorAVX128((__int8*)pValues, bitsPerValue, length, (__int16*)pVector);
				return 0;
			case Scenario::StretchGenericCompareToVectorAVX128:
				StretchGenericCompareToVectorAVX128((__int8*)pValues, bitsPerValue, length, (__int16*)pVector);
				return 0;
			default:
				throw gcnew NotImplementedException(scenario.ToString());
		}
	}
}