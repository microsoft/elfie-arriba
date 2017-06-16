#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "CompareToVector.h"

/*
  Parallel set comparison operations using AVX vector instructions, which compare 32 bytes in parallel.
  These can compare ~12GB/s per core.

  Instructions are only provided for signed values, and only for greater than and equals.
  Comparisons on unsigned types are done by subtracting first. 
	[If -128 is the lowest value, shift 0 to -128 so it'll compare as lowest].
  Other operators are done by swapping the operands.
    [ !(A > B) == (A <= B); !(A == B) == (A != B) ]

  __mm256_loadu_si128   - Load 32 bytes of unaligned data.

  __mm256_cmpgt_epi8    - Compare 32 bytes in parallel. Set a mask to 0x00 if A <= B or 0xFF if A > B
  __mm256_cmpeq_epi8    - Compare 32 bytes in parallel. Set a mask to 0x00 if A != B or 0xFF if A == B

  __mm256_movemask_epi8 - Set 32 bits to the first bit of each mask byte (convert byte mask to bit mask).

  __mm256_set1_epi8   - Set all bytes of a register to the same one byte value.
  __mm256_sub_epi8    - Subtract from each byte in parallel.

*/

#pragma unmanaged

void CompareToVector::WhereGreaterThan(bool positive, bool and, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector)
{
	int i = 0;

	// Load a mask to convert unsigned values for signed comparison, and copies of the value to compare
	__m256i signedToUnsigned = _mm256_set1_epi8(-128);
	__m256i blockOfValue = _mm256_sub_epi8(_mm256_set1_epi8(value), signedToUnsigned);

	// Compare 64-byte blocks and generate a 64-bit result while there's enough data
	int blockLength = length - (length & 63);
	for (; i < blockLength; i += 64)
	{
		__m256i block1 = _mm256_sub_epi8(_mm256_loadu_si256((__m256i*)(&set[i])), signedToUnsigned);
		__m256i matchMask1 = _mm256_cmpgt_epi8(block1, blockOfValue);
		unsigned int matchBits1 = _mm256_movemask_epi8(matchMask1);

		__m256i block2 = _mm256_sub_epi8(_mm256_loadu_si256((__m256i*)(&set[i + 32])), signedToUnsigned);
		__m256i matchMask2 = _mm256_cmpgt_epi8(block2, blockOfValue);
		unsigned int matchBits2 = _mm256_movemask_epi8(matchMask2);

		unsigned __int64 result = ((unsigned __int64)matchBits2) << 32 | matchBits1;

		if (!positive) result = ~result;
		if(and) matchVector[i >> 6] &= result; else matchVector[i >> 6] |= result;
	}

	// Match remaining values individually
	if ((length & 63) > 0)
	{
		unsigned __int64 last = 0;
		for (; i < length; ++i)
		{
			if ((set[i] > value) == positive)
			{
				last |= (0x1ULL << (i & 63));
			}
		}
		if(and) matchVector[length >> 6] &= last; else matchVector[length >> 6] |= last;
	}
}

// *** EXACTLY *** the same as AndWhereGreaterThan with the compare operands swapped
void CompareToVector::WhereLessThan(bool positive, bool and, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector)
{
	int i = 0;

	// Load a mask to convert unsigned values for signed comparison, and copies of the value to compare
	__m256i signedToUnsigned = _mm256_set1_epi8(-128);
	__m256i blockOfValue = _mm256_sub_epi8(_mm256_set1_epi8(value), signedToUnsigned);

	// Compare 64-byte blocks and generate a 64-bit result while there's enough data
	int blockLength = length - (length & 63);
	for (; i < blockLength; i += 64)
	{
		__m256i block1 = _mm256_sub_epi8(_mm256_loadu_si256((__m256i*)(&set[i])), signedToUnsigned);
		__m256i matchMask1 = _mm256_cmpgt_epi8(blockOfValue, block1);  // Operands swapped
		unsigned int matchBits1 = _mm256_movemask_epi8(matchMask1);

		__m256i block2 = _mm256_sub_epi8(_mm256_loadu_si256((__m256i*)(&set[i + 32])), signedToUnsigned);
		__m256i matchMask2 = _mm256_cmpgt_epi8(blockOfValue, block2);  // Operands swapped
		unsigned int matchBits2 = _mm256_movemask_epi8(matchMask2);

		unsigned __int64 result = ((unsigned __int64)matchBits2) << 32 | matchBits1;

		if (!positive) result = ~result;
		if (and) matchVector[i >> 6] &= result; else matchVector[i >> 6] |= result;
	}

	// Match remaining values individually
	if ((length & 63) > 0)
	{
		unsigned __int64 last = 0;
		for (; i < length; ++i)
		{
			if ((value > set[i]) == positive)  // Operands swapped
			{
				last |= (0x1ULL << (i & 63));
			}
		}
		if (and) matchVector[length >> 6] &= last; else matchVector[length >> 6] |= last;
	}
}

// *** EXACTLY *** the same as AndWhereGreaterThan with the compare operations changed
void CompareToVector::WhereEquals(bool positive, bool and, unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector)
{
	int i = 0;

	// Load a mask to convert unsigned values for signed comparison, and copies of the value to compare
	__m256i signedToUnsigned = _mm256_set1_epi8(-128);
	__m256i blockOfValue = _mm256_sub_epi8(_mm256_set1_epi8(value), signedToUnsigned);

	// Compare 64-byte blocks and generate a 64-bit result while there's enough data
	int blockLength = length - (length & 63);
	for (; i < blockLength; i += 64)
	{
		__m256i block1 = _mm256_sub_epi8(_mm256_loadu_si256((__m256i*)(&set[i])), signedToUnsigned);
		__m256i matchMask1 = _mm256_cmpeq_epi8(block1, blockOfValue); // gt to eq
		unsigned int matchBits1 = _mm256_movemask_epi8(matchMask1);

		__m256i block2 = _mm256_sub_epi8(_mm256_loadu_si256((__m256i*)(&set[i + 32])), signedToUnsigned);
		__m256i matchMask2 = _mm256_cmpeq_epi8(block2, blockOfValue); // gt to eq
		unsigned int matchBits2 = _mm256_movemask_epi8(matchMask2);

		unsigned __int64 result = ((unsigned __int64)matchBits2) << 32 | matchBits1;

		if (!positive) result = ~result;
		if (and) matchVector[i >> 6] &= result; else matchVector[i >> 6] |= result;
	}

	// Match remaining values individually
	if ((length & 63) > 0)
	{
		unsigned __int64 last = 0;
		for (; i < length; ++i)
		{
			if ((set[i] == value) == positive) // > to ==
			{
				last |= (0x1ULL << (i & 63));
			}
		}
		if (and) matchVector[length >> 6] &= last; else matchVector[length >> 6] |= last;
	}
}