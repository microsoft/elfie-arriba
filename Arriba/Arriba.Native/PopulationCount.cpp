#include "stdafx.h"

#include <intrin.h>
#include <nmmintrin.h>

extern "C" __declspec(dllexport) int CallOverheadTest()
{
	return 1;
}

extern "C" __declspec(dllexport) bool IsPopulationCountSupported()
{
	int cpuinfo[4];
	__cpuid(cpuinfo, 1);

	return (cpuinfo[2] & (0x1 << 23));
}

// V4. [Remove output data dependency]; 1,300ms [6.15x]
extern "C" __declspec(dllexport) int PopulationCount(UINT64* values, INT32 length)
{
	int total1 = 0;
	int total2 = 0;

	UINT64* current = values;
	INT32 remaining = length / 2;

	for (; remaining != 0; --remaining)
	{
		total1 += _mm_popcnt_u64(*current++);		
		total2 += _mm_popcnt_u64(*current++);
	}

	remaining = length % 2;
	for (; remaining != 0; --remaining)
	{
		total1 += _mm_popcnt_u64(*current++);
	}

	return total1 + total2;
}

//// V5. [More unrolling and use constant offsets]; 2,200ms
//extern "C" __declspec(dllexport) int PopulationCount(UINT64* values, INT32 length)
//{
//	int total1 = 0;
//	int total2 = 0;
//	int total3 = 0;
//	int total4 = 0;
//
//	UINT64* current = values;
//	INT32 remaining = length / 4;
//
//	for (; remaining != 0; --remaining)
//	{
//		total1 += _mm_popcnt_u64(*(current));
//		total2 += _mm_popcnt_u64(*(current + 1));
//		total3 += _mm_popcnt_u64(*(current + 2));
//		total4 += _mm_popcnt_u64(*(current + 4));
//		current += 4;
//	}
//
//	remaining = length % 4;
//	for (; remaining != 0; --remaining)
//	{
//		total1 += _mm_popcnt_u64(*current++);
//	}
//
//	return total1 + total2 + total3 + total4;
//}

// V3. [Unroll 2]; 2,200ms
//extern "C" __declspec(dllexport) int PopulationCount(UINT64* values, INT32 length)
//{
//	int total = 0;
//
//	UINT64* current = values;
//	INT32 remaining = length / 2;
//
//	for (; remaining != 0; --remaining)
//	{
//		total += _mm_popcnt_u64(*current++);
//		total += _mm_popcnt_u64(*current++);
//	}
//
//	remaining = length % 2;
//	for (; remaining != 0; --remaining)
//	{
//		total += _mm_popcnt_u64(*current++);
//	}
//
//	return total;
//}

// V2 [Better condition]; 3,300ms (same)
//extern "C" __declspec(dllexport) int PopulationCount(UINT64* values, INT32 length)
//{
//	int total = 0;
//
//	UINT64* current = values;
//	INT32 remaining = length;
//
//	for (; remaining != 0; --remaining)
//	{
//		total += _mm_popcnt_u64(*current);
//		current++;
//	}
//
//	return total;
//}

// V1. 3,300ms (2.4x)
//extern "C" __declspec(dllexport) int PopulationCount(UINT64* values, INT32 length)
//{
//	int total = 0;
//	for (int i = 0; i < length; ++i)
//	{
//		total += _mm_popcnt_u64(values[i]);
//	}
//
//	return total;
//}

// V0. C#. 8,000ms
//// Count using the hamming weight algorithm [http://en.wikipedia.org/wiki/Hamming_weight]
//const ulong m1 = 0x5555555555555555UL;
//const ulong m2 = 0x3333333333333333UL;
//const ulong m4 = 0x0f0f0f0f0f0f0f0fUL;
//const ulong h1 = 0x0101010101010101UL;
//
//ushort count = 0;
//
//int length = this.bitVector.Length;
//for (int i = 0; i < length; ++i)
//{
//	ulong x = this.bitVector[i];
//
//	x -= (x >> 1) & m1;
//	x = (x & m2) + ((x >> 2) & m2);
//	x = (x + (x >> 4)) & m4;
//
//	count += (ushort)((x * h1) >> 56);
//}
//
//return count;