#include "stdafx.h"
#include <intrin.h>
#include <nmmintrin.h>
#include "IndexSet.h"
#include "CompareToVector.h"

#pragma unmanaged

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
			return CountInternal(pVector, this->bitVector->Length);
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
			pin_ptr<T> pValues = &values[0];
			pin_ptr<UInt64> pVector = &(this->bitVector[0]);

			if (T::typeid == System::Byte::typeid)
			{
				switch (op)
				{
					case CompareOperator::GreaterThan:
						CompareToVector::WhereGreaterThan(true, true, (unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
						break;
					case CompareOperator::LessThan:
						CompareToVector::WhereLessThan(true, true, (unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
						break;
					case CompareOperator::Equals:
						CompareToVector::WhereEquals(true, true, (unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
						break;
					case CompareOperator::LessThanOrEqual:
						CompareToVector::WhereGreaterThan(false, true, (unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
						break;
					case CompareOperator::GreaterThanOrEqual:
						CompareToVector::WhereLessThan(false, true, (unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
						break;
					case CompareOperator::NotEquals:
						CompareToVector::WhereEquals(false, true, (unsigned __int8*)pValues, values->Length, (unsigned char)value, pVector);
						break;
				}
			}

			return this;
		}
	}
}