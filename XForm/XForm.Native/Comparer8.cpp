// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "Operator.h"
#include "ComparerSingle.cpp"
#include "Comparer.h"

#pragma unmanaged

template<CompareOperatorN cOp, SigningN sign>
static void WhereN(unsigned __int8* set, int length, unsigned __int8 value, BooleanOperatorN bOp, unsigned __int64* matchVector)
{
	int i = 0;
	unsigned __int64 result;

	// Load a mask to convert unsigned values for signed comparison
	__m256i unsignedToSigned = _mm256_set1_epi8(-128);

	// Load copies of the value to compare against
	__m256i blockOfValue = _mm256_set1_epi8(value);
	if (sign == SigningN::Unsigned) blockOfValue = _mm256_sub_epi8(blockOfValue, unsignedToSigned);

	// Compare 64-byte blocks and generate a 64-bit result while there's enough data
	int blockLength = length & ~63;
	for (; i < blockLength; i += 64)
	{
		// Load 64 bytes to compare
		__m256i block1 = _mm256_loadu_si256((__m256i*)(&set[i]));
		__m256i block2 = _mm256_loadu_si256((__m256i*)(&set[i + 32]));

		// Convert them to signed form, if needed
		if (sign == SigningN::Unsigned)
		{
			block1 = _mm256_sub_epi8(block1, unsignedToSigned);
			block2 = _mm256_sub_epi8(block2, unsignedToSigned);
		}

		// Compare them to the desired value, building a mask with 0xFFFF for matches and 0x0000 for non-matches
		__m256i matchMask1;
		__m256i matchMask2;

		switch (cOp)
		{
		case CompareOperatorN::GreaterThan:
		case CompareOperatorN::LessThanOrEqual:
			matchMask1 = _mm256_cmpgt_epi8(block1, blockOfValue);
			matchMask2 = _mm256_cmpgt_epi8(block2, blockOfValue);
			break;
		case CompareOperatorN::LessThan:
		case CompareOperatorN::GreaterThanOrEqual:
			matchMask1 = _mm256_cmpgt_epi8(blockOfValue, block1);
			matchMask2 = _mm256_cmpgt_epi8(blockOfValue, block2);
			break;
		case CompareOperatorN::Equal:
		case CompareOperatorN::NotEqual:
			matchMask1 = _mm256_cmpeq_epi8(block1, blockOfValue);
			matchMask2 = _mm256_cmpeq_epi8(block2, blockOfValue);
			break;
		}

		// Convert the masks into bits (one bit per byte)
		unsigned int matchBits1 = _mm256_movemask_epi8(matchMask1);
		unsigned int matchBits2 = _mm256_movemask_epi8(matchMask2);

		// Merge the result to get 64 bits for whether 64 rows matched
		result = ((unsigned __int64)matchBits2) << 32 | matchBits1;

		// Negate the result for operators we ran the opposites of
		if (cOp == CompareOperatorN::LessThanOrEqual || cOp == CompareOperatorN::GreaterThanOrEqual || cOp == CompareOperatorN::NotEqual)
		{
			result = ~result;
		}

		// Merge the result with the existing bit vector bits based on the boolean operator requested
		switch (bOp)
		{
		case BooleanOperatorN::And:
			matchVector[i >> 6] &= result;
			break;
		case BooleanOperatorN::Or:
			matchVector[i >> 6] |= result;
			break;
		}
	}

	// Match remaining values individually
	if (length & 63)
	{
		if (sign == SigningN::Unsigned)
			WhereSingle<cOp, unsigned __int8>(&set[i], length - i, value, bOp, &matchVector[i >> 6]);
		else
			WhereSingle<cOp, __int8>((__int8*)&set[i], length - i, (__int8)value, bOp, &matchVector[i >> 6]);
	}
}

#pragma managed

namespace XForm
{
	namespace Native
	{
		void Comparer::Where(array<Byte>^ left, Int32 index, Int32 length, Byte cOp, Byte right, Byte bOp, array<UInt64>^ vector, Int32 vectorIndex)
		{
			if (index < 0 || length < 0 || vectorIndex < 0) throw gcnew IndexOutOfRangeException();
			if (index + length > left->Length) throw gcnew IndexOutOfRangeException();
			if (vectorIndex + length >(vector->Length * 64)) throw gcnew IndexOutOfRangeException();
			if ((vectorIndex & 63) != 0) throw gcnew ArgumentException("Offset Where must run on a multiple of 64 offset.");

			pin_ptr<Byte> pLeft = &left[index];
			pin_ptr<UInt64> pVector = &vector[vectorIndex >> 6];

			switch ((CompareOperatorN)cOp)
			{
			case CompareOperatorN::Equal:
				WhereN<CompareOperatorN::Equal, SigningN::Unsigned>(pLeft, length, right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::NotEqual:
				WhereN<CompareOperatorN::NotEqual, SigningN::Unsigned>(pLeft, length, right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::LessThan:
				WhereN<CompareOperatorN::LessThan, SigningN::Unsigned>(pLeft, length, right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::LessThanOrEqual:
				WhereN<CompareOperatorN::LessThanOrEqual, SigningN::Unsigned>(pLeft, length, right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::GreaterThan:
				WhereN<CompareOperatorN::GreaterThan, SigningN::Unsigned>(pLeft, length, right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::GreaterThanOrEqual:
				WhereN<CompareOperatorN::GreaterThanOrEqual, SigningN::Unsigned>(pLeft, length, right, (BooleanOperatorN)bOp, pVector);
				break;
			default:
				throw gcnew ArgumentException("cOp");
			}
		}

		void Comparer::Where(array<SByte>^ left, Int32 index, Int32 length, Byte cOp, SByte right, Byte bOp, array<UInt64>^ vector, Int32 vectorIndex)
		{
			if (index < 0 || length < 0 || vectorIndex < 0) throw gcnew IndexOutOfRangeException();
			if (index + length > left->Length) throw gcnew IndexOutOfRangeException();
			if (vectorIndex + length >(vector->Length * 64)) throw gcnew IndexOutOfRangeException();
			if ((vectorIndex & 63) != 0) throw gcnew ArgumentException("Offset Where must run on a multiple of 64 offset.");

			pin_ptr<SByte> pLeft = &left[index];
			pin_ptr<UInt64> pVector = &vector[vectorIndex >> 6];

			switch ((CompareOperatorN)cOp)
			{
			case CompareOperatorN::Equal:
				WhereN<CompareOperatorN::Equal, SigningN::Signed>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::NotEqual:
				WhereN<CompareOperatorN::NotEqual, SigningN::Signed>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::LessThan:
				WhereN<CompareOperatorN::LessThan, SigningN::Signed>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::LessThanOrEqual:
				WhereN<CompareOperatorN::LessThanOrEqual, SigningN::Signed>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::GreaterThan:
				WhereN<CompareOperatorN::GreaterThan, SigningN::Signed>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::GreaterThanOrEqual:
				WhereN<CompareOperatorN::GreaterThanOrEqual, SigningN::Signed>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			default:
				throw gcnew ArgumentException("cOp");
			}
		}

		void Comparer::Where(array<Boolean>^ left, Int32 index, Int32 length, Byte cOp, Boolean right, Byte bOp, array<UInt64>^ vector, Int32 vectorIndex)
		{
			if (index < 0 || length < 0 || vectorIndex < 0) throw gcnew IndexOutOfRangeException();
			if (index + length > left->Length) throw gcnew IndexOutOfRangeException();
			if (vectorIndex + length >(vector->Length * 64)) throw gcnew IndexOutOfRangeException();
			if ((vectorIndex & 63) != 0) throw gcnew ArgumentException("Offset Where must run on a multiple of 64 offset.");

			pin_ptr<Boolean> pLeft = &left[index];
			pin_ptr<UInt64> pVector = &vector[vectorIndex >> 6];

			switch ((CompareOperatorN)cOp)
			{
			case CompareOperatorN::Equal:
				WhereN<CompareOperatorN::Equal, SigningN::Unsigned>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			case CompareOperatorN::NotEqual:
				WhereN<CompareOperatorN::NotEqual, SigningN::Unsigned>((unsigned __int8*)pLeft, length, (unsigned __int8)right, (BooleanOperatorN)bOp, pVector);
				break;
			default:
				throw gcnew ArgumentException("cOp");
			}
		}
	}
}
