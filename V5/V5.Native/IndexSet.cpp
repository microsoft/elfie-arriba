#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "IndexSet.h"

/*
	Set operations using SSE vector instructions, using the XMM (16 byte) registers.

	_mm_set1_epi8     - Load an XMM register with 16 copies of the passed byte.
	_mm_loadu_si128   - Load an XMM register with 16 bytes from the unaligned source.

	_mm_cmpgt_epi8    - Per-byte Greater Than: Set an XMM mask with all one bits where XMM1[i] > XMM2[i].
	_mm_movemask_epi8 - Set lower 16 bits based on whether each byte in an XMM mask is all one.
*/

#pragma unmanaged

void AndWhereGreaterThanInternal(unsigned __int8* set, int length, unsigned __int8 value, unsigned __int64* matchVector)
{
	int i = 0;

	__m256i signedToUnsigned = _mm256_set1_epi8(-128);
	__m256i blockOfValue = _mm256_sub_epi8(_mm256_set1_epi8(value), signedToUnsigned);

	int blockLength = length - (length & 63);
	for (; i < blockLength; i += 64)
	{
		__m256i block1 = _mm256_loadu_si256((__m256i*)(&set[i]));
		block1 = _mm256_sub_epi8(block1, signedToUnsigned);

		__m256i matchMask1 = _mm256_cmpgt_epi8(block1, blockOfValue);
		unsigned int matchBits1 = _mm256_movemask_epi8(matchMask1);

		__m256i block2 = _mm256_loadu_si256((__m256i*)(&set[i + 32]));
		block2 = _mm256_sub_epi8(block2, signedToUnsigned);

		__m256i matchMask2 = _mm256_cmpgt_epi8(block2, blockOfValue);
		unsigned int matchBits2 = _mm256_movemask_epi8(matchMask2);

		unsigned __int64 result = matchBits2;
		result = result << 32;
		result |= matchBits1;

		matchVector[i >> 6] &= result;
	}

	// Match remaining values individually
	if ((length & 63) > 0)
	{
		unsigned __int64 last = 0;
		for (; i < length; ++i)
		{
			if (set[i] > value)
			{
				last |= ((unsigned __int64)(1) << (i & 63));
			}
		}
		matchVector[length >> 6] &= last;
	}
}

int CountInternal(unsigned __int64* matchVector, int length)
{
	__int64 count = 0;

	for (int i = 0; i < length; ++i)
	{
		count += _mm_popcnt_u64(matchVector[i]);
	}

	return (int)count;
}

#pragma managed

namespace V5
{
	namespace Collections
	{
		IndexSet::IndexSet()
		{ }

		IndexSet::IndexSet(UInt32 offset, UInt32 length)
		{
			this->offset = offset;
			this->length = length;
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
			return CountInternal(pVector, this->bitVector->Length);
		}

		Boolean IndexSet::Equals(Object^ o)
		{
			if (o == nullptr || GetType() != o->GetType()) return false;
			IndexSet^ other = dynamic_cast<IndexSet^>(o);

			if (this->offset != other->offset) return false;
			if (this->length != other->length) return false;

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				if (this->bitVector[i] != other->bitVector[i])
				{
					return false;
				}
			}

			return true;
		}

		IndexSet^ IndexSet::None()
		{
			System::Array::Clear(this->bitVector, 0, this->bitVector->Length);
			return this;
		}

		IndexSet^ IndexSet::All()
		{
			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] = ~0x0ULL;
			}

			if ((this->length & 63) > 0)
			{
				this->bitVector[this->bitVector->Length - 1] = (~0x0ULL) >> (64 - (this->length & 63));
			}

			return this;
		}

		IndexSet^ IndexSet::And(IndexSet^ other)
		{
			if (this->offset != other->offset) throw gcnew InvalidOperationException();
			if (this->length != other->length) throw gcnew InvalidOperationException();

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] &= other->bitVector[i];
			}

			return this;
		}

		IndexSet^ IndexSet::AndNot(IndexSet^ other)
		{
			if (this->offset != other->offset) throw gcnew InvalidOperationException();
			if (this->length != other->length) throw gcnew InvalidOperationException();

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] &= ~other->bitVector[i];
			}

			return this;
		}


		IndexSet^ IndexSet::Or(IndexSet^ other)
		{
			if (this->offset != other->offset) throw gcnew InvalidOperationException();
			if (this->length != other->length) throw gcnew InvalidOperationException();

			for (int i = 0; i < this->bitVector->Length; ++i)
			{
				this->bitVector[i] |= other->bitVector[i];
			}

			return this;
		}

		generic <typename T>
			IndexSet^ IndexSet::And(array<T>^ values, Operator op, T value)
			{
				pin_ptr<T> pValues = &values[0];
				pin_ptr<UInt64> pVector = &(this->bitVector[0]);

				if (T::typeid == System::Byte::typeid)
				{
					AndWhereGreaterThanInternal((unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
				}

				return this;
			}
	}
}