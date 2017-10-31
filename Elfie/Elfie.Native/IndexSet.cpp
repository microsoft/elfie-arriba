#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "IndexSet.h"

#include "CompareToVector.h"

// Must include templated method implementations so that specific typed versions can compile.
#include "CompareToSingle.cpp"

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

namespace ElfieNative
{
	namespace Collections
	{
		Int32 IndexSetN::Count(array<UInt64>^ vector)
		{
			pin_ptr<UInt64> pVector = &vector[0];
			return CountN(pVector, vector->Length);
		}

		Int32 IndexSetN::Page(array<UInt64>^ vector, array<Int32>^ page, Int32% fromIndex)
		{
			pin_ptr<UInt64> pVector = &vector[0];
			pin_ptr<Int32> pPage = &page[0];
				
			int nextIndex = fromIndex;
			int countFound = PageN(pVector, vector->Length, &nextIndex, pPage, page->Length);
			fromIndex = nextIndex;
			return countFound;
		}

		generic <typename T>
		void IndexSetN::Where(array<UInt64>^ vector, ElfieNative::Query::BooleanOperator bOp, array<T>^ values, ElfieNative::Query::CompareOperator cOp, T value, int offset, int length)
		{
			if (offset + length > values->Length) throw gcnew IndexOutOfRangeException();
			if (offset + length > (vector->Length * 64)) throw gcnew IndexOutOfRangeException();
			if ((offset & 63) != 0) throw gcnew ArgumentException("Offset Where must run on a multiple of 64 offset.");

			pin_ptr<T> pValues = &values[offset];
			pin_ptr<UInt64> pVector = &vector[offset >> 6];

			if (T::typeid == System::Byte::typeid)
			{
				CompareToVector::Where((CompareOperatorN)cOp, (BooleanOperatorN)bOp, SigningN::Unsigned, (unsigned __int8*)pValues, length, (unsigned __int8)value, pVector);
			}
			else if (T::typeid == System::SByte::typeid)
			{
				CompareToVector::Where((CompareOperatorN)cOp, (BooleanOperatorN)bOp, SigningN::Signed, (unsigned __int8*)(__int8*)pValues, length, (unsigned __int8)(__int8)value, pVector);
			}
			else if (T::typeid == System::UInt16::typeid)
			{
				CompareToVector::Where((CompareOperatorN)cOp, (BooleanOperatorN)bOp, SigningN::Unsigned, (unsigned __int16*)pValues, length, (unsigned __int16)value, pVector);
			}
			else if (T::typeid == System::Int16::typeid)
			{
				CompareToVector::Where((CompareOperatorN)cOp, (BooleanOperatorN)bOp, SigningN::Signed, (unsigned __int16*)(__int16*)pValues, length, (unsigned __int16)(__int16)value, pVector);
			}
			else if (T::typeid == System::UInt32::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)cOp, (BooleanOperatorN)bOp, (unsigned __int32*)pValues, length, (unsigned __int32)value, pVector);
			}
			else if (T::typeid == System::Int32::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)cOp, (BooleanOperatorN)bOp, (__int32*)pValues, length, (__int32)value, pVector);
			}
			else if (T::typeid == System::UInt64::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)cOp, (BooleanOperatorN)bOp, (unsigned __int64*)pValues, length, (unsigned __int64)value, pVector);
			}
			else if (T::typeid == System::Int64::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)cOp, (BooleanOperatorN)bOp, (__int64*)pValues, length, (__int64)value, pVector);
			}
			else if (T::typeid == System::Single::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)cOp, (BooleanOperatorN)bOp, (float*)pValues, length, (float)value, pVector);
			}
			else if (T::typeid == System::Double::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)cOp, (BooleanOperatorN)bOp, (double*)pValues, length, (double)value, pVector);
			}
			else
			{
				throw gcnew NotImplementedException();
			}
		}
	}
}