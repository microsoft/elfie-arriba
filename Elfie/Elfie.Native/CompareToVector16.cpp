#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "Operator.h"
#include "CompareToVector.h"

#include "CompareToSingle.cpp"

/*
Parallel set comparison operations on two-byte values using AVX vector instructions, which compare 32 bytes (16 shorts) in parallel.
These can compare ~12GB/s per core.

Instructions are only provided for signed values, and only for greater than and equals.
Comparisons on unsigned types are done by subtracting first.
[If -128 is the lowest value, shift 0 to -128 so it'll compare as lowest].
Other operators are done by swapping the operands.
[ !(A > B) == (A <= B); !(A == B) == (A != B) ]

__mm256_loadu_si256    - Load 32 bytes of unaligned data.

__mm256_cmpgt_epi16    - Compare 16 shorts in parallel. Set a mask to 0x0000 if A <= B or 0xFFFF if A > B
__mm256_cmpeq_epi16    - Compare 16 shorts in parallel. Set a mask to 0x0000 if A != B or 0xFFFF if A == B

__mm256_movemask_epi8  - Set 32 bits to the first bit of each mask byte (convert byte mask to bit mask).

__mm256_set1_epi16     - Set all bytes of a register to the same one short value.
__mm256_sub_epi16      - Subtract from each short in parallel.

_pext_u32              - Parallel Bit Extract: For each bit set on the mask, copy that bit from the input to the next adjacent bit on the output. ("Squish" bits together)
_pdep_u64              - Parallel Bit Deposit: For each bit set on the mask, copy the next adjacent bit from the input to the mask bit position on the output. ("Stretch" bits apart)
*/

#pragma unmanaged

template<CompareOperatorN cOp, BooleanOperatorN bOp, SigningN sign>
static void WhereN(unsigned __int16* set, int length, unsigned __int16 value, unsigned __int64* matchVector)
{
	int i = 0;
	unsigned __int64 result;

	// Load a mask to convert unsigned values for signed comparison
	__m256i unsignedToSigned = _mm256_set1_epi16(-32768);

	// Load copies of the value to compare against
	__m256i blockOfValue = _mm256_set1_epi16(value);
	if (sign == SigningN::Unsigned) blockOfValue = _mm256_sub_epi16(blockOfValue, unsignedToSigned);

	// Build a PEXT mask asking for every other bit (1010 = A)
	unsigned int everyOtherBit = 0xAAAAAAAA;

	// Compare 64-byte blocks and generate a 64-bit result while there's enough data
	int blockLength = length & ~63;
	for (; i < blockLength; i += 64)
	{
		__m256i block1 = _mm256_loadu_si256((__m256i*)(&set[i]));
		__m256i block2 = _mm256_loadu_si256((__m256i*)(&set[i + 16]));
		__m256i block3 = _mm256_loadu_si256((__m256i*)(&set[i + 32]));
		__m256i block4 = _mm256_loadu_si256((__m256i*)(&set[i + 48]));

		// Convert them to signed form, if needed
		if (sign == SigningN::Unsigned)
		{
			block1 = _mm256_sub_epi16(block1, unsignedToSigned);
			block2 = _mm256_sub_epi16(block2, unsignedToSigned);
			block3 = _mm256_sub_epi16(block3, unsignedToSigned);
			block4 = _mm256_sub_epi16(block4, unsignedToSigned);
		}

		// Compare them to the desired value, building a mask with 0xFFFF for matches and 0x0000 for non-matches
		__m256i matchMask1;
		__m256i matchMask2;
		__m256i matchMask3;
		__m256i matchMask4;

		switch (cOp)
		{
		case CompareOperatorN::GreaterThan:
		case CompareOperatorN::LessThanOrEqual:
			matchMask1 = _mm256_cmpgt_epi16(block1, blockOfValue);
			matchMask2 = _mm256_cmpgt_epi16(block2, blockOfValue);
			matchMask3 = _mm256_cmpgt_epi16(block3, blockOfValue);
			matchMask4 = _mm256_cmpgt_epi16(block4, blockOfValue);
			break;
		case CompareOperatorN::LessThan:
		case CompareOperatorN::GreaterThanOrEqual:
			matchMask1 = _mm256_cmpgt_epi16(blockOfValue, block1);
			matchMask2 = _mm256_cmpgt_epi16(blockOfValue, block2);
			matchMask3 = _mm256_cmpgt_epi16(blockOfValue, block3);
			matchMask4 = _mm256_cmpgt_epi16(blockOfValue, block4);
			break;
		case CompareOperatorN::Equals:
		case CompareOperatorN::NotEquals:
			matchMask1 = _mm256_cmpeq_epi16(block1, blockOfValue);
			matchMask2 = _mm256_cmpeq_epi16(block2, blockOfValue);
			matchMask3 = _mm256_cmpeq_epi16(block3, blockOfValue);
			matchMask4 = _mm256_cmpeq_epi16(block4, blockOfValue);
			break;
		}

		// Convert the masks into bits (one bit per byte, so still two duplicate bits per row matched)
		unsigned int matchBits1 = _mm256_movemask_epi8(matchMask1);
		unsigned int matchBits2 = _mm256_movemask_epi8(matchMask2);
		unsigned int matchBits3 = _mm256_movemask_epi8(matchMask3);
		unsigned int matchBits4 = _mm256_movemask_epi8(matchMask4);

		// Get every other bit (so it's one per row) and merge together pairs
		unsigned int matchBits2_1 = _pext_u32(matchBits2, everyOtherBit) << 16 | _pext_u32(matchBits1, everyOtherBit);
		unsigned int matchBits4_3 = _pext_u32(matchBits4, everyOtherBit) << 16 | _pext_u32(matchBits3, everyOtherBit);

		// Merge the result to get 64 bits for whether 64 rows matched
		result = ((unsigned __int64)matchBits4_3) << 32 | matchBits2_1;

		// Negate the result for operators we ran the opposites of
		if (cOp == CompareOperatorN::LessThanOrEqual || cOp == CompareOperatorN::GreaterThanOrEqual || cOp == CompareOperatorN::NotEquals)
		{
			result = ~result;
		}

		// Merge the result with the existing bit vector bits based on the boolean operator requested
		switch (bOp)
		{
		case BooleanOperatorN::Set:
			matchVector[i >> 6] = result;
			break;
		case BooleanOperatorN::And:
			matchVector[i >> 6] &= result;
			break;
		case BooleanOperatorN::Or:
			matchVector[i >> 6] |= result;
			break;
		}
	}

	// Match remaining values individually
	if (length & 63) WhereSingle<cOp, bOp, unsigned __int16>(&set[i], length - i, value, &matchVector[i >> 6]);
}

template<BooleanOperatorN bOp, SigningN signing>
void WhereB(CompareOperatorN cOp, unsigned __int16* set, int length, unsigned __int16 value, unsigned __int64* matchVector)
{
	switch (cOp)
	{
	case CompareOperatorN::GreaterThan:
		WhereN<CompareOperatorN::GreaterThan, bOp, signing>(set, length, value, matchVector);
		break;
	case CompareOperatorN::GreaterThanOrEqual:
		WhereN<CompareOperatorN::GreaterThanOrEqual, bOp, signing>(set, length, value, matchVector);
		break;
	case CompareOperatorN::LessThan:
		WhereN<CompareOperatorN::LessThan, bOp, signing>(set, length, value, matchVector);
		break;
	case CompareOperatorN::LessThanOrEqual:
		WhereN<CompareOperatorN::LessThanOrEqual, bOp, signing>(set, length, value, matchVector);
		break;
	case CompareOperatorN::Equals:
		WhereN<CompareOperatorN::Equals, bOp, signing>(set, length, value, matchVector);
		break;
	case CompareOperatorN::NotEquals:
		WhereN<CompareOperatorN::NotEquals, bOp, signing>(set, length, value, matchVector);
		break;
	}
}

template<SigningN signing>
void WhereS(CompareOperatorN cOp, BooleanOperatorN bOp, unsigned __int16* set, int length, unsigned __int16 value, unsigned __int64* matchVector)
{
	switch (bOp)
	{
	case BooleanOperatorN::Set:
		WhereB<BooleanOperatorN::Set, signing>(cOp, set, length, value, matchVector);
		break;
	case BooleanOperatorN::And:
		WhereB<BooleanOperatorN::And, signing>(cOp, set, length, value, matchVector);
		break;
	case BooleanOperatorN::Or:
		WhereB<BooleanOperatorN::Or, signing>(cOp, set, length, value, matchVector);
		break;
	}
}

void CompareToVector::Where(CompareOperatorN cOp, BooleanOperatorN bOp, SigningN signing, unsigned __int16* set, int length, unsigned __int16 value, unsigned __int64* matchVector)
{
	switch (signing)
	{
	case SigningN::Signed:
		WhereS<SigningN::Signed>(cOp, bOp, set, length, value, matchVector);
		break;
	case SigningN::Unsigned:
		WhereS<SigningN::Unsigned>(cOp, bOp, set, length, value, matchVector);
		break;
	}
}