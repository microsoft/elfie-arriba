// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "BitVectorN.h"

#pragma unmanaged
int CountN(unsigned __int64* matchVector, int length)
{
	__int64 count = 0;

	int i = 0;
	int end = length & ~3;
	for (; i < end; i += 4)
	{
		count += _mm_popcnt_u64(matchVector[i]);
		count += _mm_popcnt_u64(matchVector[i + 1]);
		count += _mm_popcnt_u64(matchVector[i + 2]);
		count += _mm_popcnt_u64(matchVector[i + 3]);
	}

	for (; i < length; ++i)
	{
		count += _mm_popcnt_u64(matchVector[i]);
	}

	return (int)(count);
}

unsigned int __inline ctz(unsigned __int64 value)
{
	unsigned long trailingZero = 0;
	_BitScanForward64(&trailingZero, value);
	return trailingZero;
}

int PageN(unsigned __int64* matchVector, int length, int* start, int* result, int resultLength)
{
	// Get pointers to the next index and the end of the array
	int* resultNext = result;
	int* resultEnd = result + resultLength;

	// Separate the block and bit to start on
	int base = *start & ~63;
	int end = length << 6;
	int matchWithinBlock = *start & 63;

	// Get the first block
	unsigned __int64 block = matchVector[base >> 6];

	// If we're resuming within this block, clear already checked bits
	if (matchWithinBlock > 0) block &= (~0x0ULL << matchWithinBlock);

	// Look for matches in each block
	while (resultNext < resultEnd)
	{
		while (block != 0 && resultNext != resultEnd)
		{
			// The index of the next match is the same as the number of trailing zero bits
			matchWithinBlock = ctz(block);

			// Add the match
			*(resultNext++) = base + matchWithinBlock;

			// Unset the last bit (mathematical identity) and continue [Note: _blsr_u64 faster for dense but slower for sparse sets]
			block &= block - 1;
		}

		// If the result Span is full, stop
		if (resultNext == resultEnd) break;

		// If the vector is done, stop, otherwise get the next block
		base += 64;
		if (base >= end) break;
		block = matchVector[base >> 6];
	}

	// Set start to -1 if we finished scanning, or the next start index otherwise
	*start = (base >= end ? -1 : base + matchWithinBlock + 1);

	// Return the match count found
	return (int)(resultNext - result);
}
#pragma managed

namespace XForm
{
	namespace Native
	{
		Int32 BitVectorN::Count(array<UInt64>^ vector)
		{
			pin_ptr<UInt64> pVector = &vector[0];
			return CountN(pVector, vector->Length);
		}

		Int32 BitVectorN::Page(array<UInt64>^ vector, array<Int32>^ indicesFound, Int32% fromIndex, Int32 countLimit)
		{
			pin_ptr<UInt64> pVector = &vector[0];
			pin_ptr<Int32> pIndices = &indicesFound[0];
			if (countLimit > indicesFound->Length) throw gcnew ArgumentOutOfRangeException("countLimit");

			int nextIndex = fromIndex;
			int countFound = PageN(pVector, vector->Length, &nextIndex, pIndices, countLimit);
			fromIndex = nextIndex;
			return countFound;  
		}
	}
}
