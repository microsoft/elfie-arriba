#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "IndexSet.h"
#include "CompareToVector.h"

#pragma unmanaged

int CountN(unsigned __int64* matchVector, int length)
{
	__int64 count = 0;

	for (int i = 0; i < length; ++i)
	{
		count += _mm_popcnt_u64(matchVector[i]);
	}

	return (int)count;
}

unsigned int __inline ctz(unsigned __int64 value)
{
	unsigned long trailingZero = 0;
	_BitScanForward64(&trailingZero, value);
	return trailingZero;
}

int PageN(unsigned __int64* matchVector, int length, int start, int* result, int resultLength, int* countSet)
{
	// Clear the page initially
	*countSet = 0;
	if (length == 0) return -1;

	// Get pointers to the next index and the end of the array
	int* resultNext = result;
	int* resultEnd = result + resultLength;

	// Separate the block and bit to start on
	int base = start & ~63;
	int end = length << 6;
	int matchWithinBlock = start & 63;

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

			// Unset the last bit (mathematical identity) and continue
			block &= block - 1;
		}

		// If the result Span is full, stop
		if (resultNext == resultEnd) break;

		// If the vector is done, stop, otherwise get the next block
		base += 64;
		if (base >= end) break;
		block = matchVector[base >> 6];
	}

	// Set the match count found
	*countSet = (int)(resultNext - result);

	// Return -1 if we finished scanning, the next start index otherwise
	return (base >= end ? -1 : base + matchWithinBlock + 1);
}

#pragma managed

namespace V5
{
	namespace Collections
	{
		IndexSet::IndexSet()
		{ }

		IndexSet::IndexSet(UInt32 length)
		{
			this->bitVector = gcnew array<UInt64>((length + 63) >> 6);
		}

		Boolean IndexSet::default::get(Int32 index)
		{
			return this->bitVector[index >> 6] & (0x1ULL << (index & 63));
		}

		void IndexSet::default::set(Int32 index, Boolean value)
		{
			if (value)
			{
				this->bitVector[index >> 6] |= (0x1ULL << (index & 63));
			}
			else
			{
				this->bitVector[index >> 6] &= ~(0x1ULL << (index & 63));
			}
		}

		Int32 IndexSet::Count::get()
		{
			pin_ptr<UInt64> pVector = &(this->bitVector)[0];
			return CountN(pVector, this->bitVector->Length);
		}

		Int32 IndexSet::Capacity::get()
		{
			return this->bitVector->Length << 6;
		}

		Boolean IndexSet::Equals(Object^ o)
		{
			if (o == nullptr || GetType() != o->GetType()) return false;
			IndexSet^ other = dynamic_cast<IndexSet^>(o);

			if (this->bitVector->Length != other->bitVector->Length) return false;

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				if (this->bitVector[i] != other->bitVector[i])
				{
					return false;
				}
			}

			return true;
		}

		Int32 IndexSet::Page(Span<Int32>% page, Int32 fromIndex)
		{
			array<Int32>^ array = page.Array;

			pin_ptr<UInt64> pVector = &(this->bitVector)[0];
			pin_ptr<Int32> pPage = &(array[page.Index]);
			int countSet = 0;
			
			int nextIndex = PageN(pVector, this->bitVector->Length, fromIndex, pPage, page.Capacity, &countSet);
			page.Length = countSet;

			return nextIndex;
		}

		IndexSet^ IndexSet::None()
		{
			System::Array::Clear(this->bitVector, 0, this->bitVector->Length);
			return this;
		}

		IndexSet^ IndexSet::All(UInt32 length)
		{
			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] = ~0x0ULL;
			}

			if ((length & 63) > 0)
			{
				this->bitVector[this->bitVector->Length - 1] = (~0x0ULL) >> (64 - (length & 63));
			}

			return this;
		}

		IndexSet^ IndexSet::And(IndexSet^ other)
		{
			if (this->bitVector->Length != other->bitVector->Length) throw gcnew InvalidOperationException();

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] &= other->bitVector[i];
			}

			return this;
		}

		IndexSet^ IndexSet::AndNot(IndexSet^ other)
		{
			if (this->bitVector->Length != other->bitVector->Length) throw gcnew InvalidOperationException();

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] &= ~other->bitVector[i];
			}

			return this;
		}


		IndexSet^ IndexSet::Or(IndexSet^ other)
		{
			if (this->bitVector->Length != other->bitVector->Length) throw gcnew InvalidOperationException();

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] |= other->bitVector[i];
			}

			return this;
		}

		generic <typename T>
		IndexSet^ IndexSet::And(array<T>^ values, CompareOperator op, T value)
		{
			if (this->bitVector->Length * 64 > values->Length) throw gcnew IndexOutOfRangeException();

			pin_ptr<T> pValues = &values[0];
			pin_ptr<UInt64> pVector = &(this->bitVector[0]);

			if (T::typeid == System::Byte::typeid)
			{
				CompareToVector::Where((CompareOperatorN)op, BooleanOperatorN::And, (unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
			}
			else if (T::typeid == System::UInt16::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)op, BooleanOperatorN::And, (unsigned __int16*)pValues, values->Length, (unsigned __int16)value, pVector);
			}
			/*else if (T::typeid == System::UInt64::typeid)
			{
				CompareToVector::WhereSingle((CompareOperatorN)op, BooleanOperatorN::And, (unsigned __int64*)pValues, values->Length, (unsigned __int64)value, pVector);
			}*/
			else
			{
				throw gcnew NotImplementedException();
			}

			return this;
		}
	}
}