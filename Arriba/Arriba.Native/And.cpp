#include "stdafx.h"

#include <intrin.h>
#include <nmmintrin.h>

void Align256(UINT64** value)
{
	int offset = (uintptr_t)(*value) % 32;
	if (offset == 0) return;
	*value += ((32 - offset) / 8);
}

extern "C" __declspec(dllexport) bool IsParallelAndSupported()
{
	int cpuinfo[4];
	__cpuid(cpuinfo, 1);

	return (cpuinfo[2] & (0x1 << 23));
}

//// V2: AVX 256 Attempt 2 (Hack: Force Alignment and use aligned instructions) (540 for 3M)
//extern "C" __declspec(dllexport) void AndSets(UINT64* result, UINT64* left, UINT64* right, INT32 length)
//{
//	__m256i leftA;
//	__m256i rightA;
//	__m256i resultA;
//
//	// Skip any unaligned end
//	length = length - 4;
//
//	// Force each array to the next aligned segment
//	Align256(&result);
//	Align256(&left);
//	Align256(&right);
//
//	// Use AVX2 aligned load, and, store
//	for (int i = 0; i < length; i += 4)
//	{
//		leftA = _mm256_load_si256((__m256i*)left);
//		rightA = _mm256_load_si256((__m256i*)right);
//		resultA = _mm256_and_si256(leftA, rightA);
//		_mm256_store_si256((__m256i*)result, resultA);
//
//		left += 4;
//		right += 4;
//		result += 4;
//	}
//}

//// V2: AVX 256 Attempt 1 (Needed unaligned load/store) (740 for 3M)
//extern "C" __declspec(dllexport) void AndSets(UINT64* result, UINT64* left, UINT64* right, INT32 length)
//{
//	__m256i leftA;
//	__m256i rightA;
//	__m256i resultA;
//
//	for (int i = 0; i < length; i += 4)
//	{
//		leftA = _mm256_loadu_si256((__m256i*)left);
//		rightA = _mm256_loadu_si256((__m256i*)right);
//		resultA = _mm256_and_si256(leftA, rightA);
//		_mm256_storeu_si256((__m256i*)result, resultA);
//
//		left += 4;
//		right += 4;
//		result += 4;
//	}
//}

// V1: Normal C++ AND (1,125 for 3M)
extern "C" __declspec(dllexport) void AndSets(UINT64* result, UINT64* left, UINT64* right, INT32 length)
{
	for (int i = 0; i < length; ++i)
	{
		result[i] = left[i] & right[i];
	}
}