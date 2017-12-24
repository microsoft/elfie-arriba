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
	}
}
