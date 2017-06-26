#include "stdafx.h"
#include "SortBucketColumnN.h"

#pragma unmanaged

template <typename T>
int BucketIndexInternal(T* bucketMins, int bucketCount, T value)
{
	// Binary search for the last value less than the search value [the bucket the value should go into]
	T* base = bucketMins;

	// Search *except the last bucket*, which stores the maximum seen
	int count = bucketCount - 1;

	while (count > 1)
	{
		int half = count >> 1;
		base = (base[half] <= value ? &base[half] : base);
		count -= half;
	}

	// Find the index matched
	int index = (int)(base - bucketMins);

	// Return the complement of the index for non-exact matches
	if (value != *base) index = ~index;

	return index;
}

template <typename T, typename U>
void BucketInternal(T* values, int index, int length, T* bucketMins, int bucketCount, U* rowBucketIndex, int* countPerBucket, bool* isMultiValue)
{
	int end = index + length;
	for (int i = index; i < end; ++i)
	{
		// Find the last bucket with a boundary less than or equal to the value
		int index = BucketIndexInternal<T>(bucketMins, bucketCount, values[i]);

		// If this value didn't exactly match a bucket...
		if (index < 0)
		{
			// Find the bucket into which it should be inserted
			index = ~index;
			
			// If this is the first bucket, capture a new minimum if seen
			if (index == 0) bucketMins[0] = (values[i] < bucketMins[0] ? values[i] : bucketMins[0]);
			
			// If this is the last bucket, capture a new maximum if seen
			if (index == bucketCount - 2) bucketMins[bucketCount - 1] = (values[i] > bucketMins[bucketCount - 1] ? values[i] : bucketMins[bucketCount - 1]);

			// Set the target bucket to multi-value
			isMultiValue[index] = true;
		}
		
		// Put the item in the bucket and count the row
		rowBucketIndex[i] = index;
		countPerBucket[index]++;
	}
}

#pragma managed

generic <typename T>
void SortBucketColumnN::Bucket(array<T>^ values, int index, int length, array<T>^ bucketMins, array<Byte>^ rowBucketIndex, array<Int32>^ countPerBucket, array<Boolean>^ isMultiValue)
{
	if (values->Length < (index + length)) return;
	if (rowBucketIndex->Length < values->Length) return;
	if (countPerBucket->Length != bucketMins->Length) return;

	pin_ptr<T> pValues = &values[0];
	pin_ptr<T> pBucketMins = &bucketMins[0];
	pin_ptr<Byte> pRowBucketIndex = &rowBucketIndex[0];
	pin_ptr<Int32> pCountPerBucket = &countPerBucket[0];
	pin_ptr<Boolean> pIsMultiValue = &isMultiValue[0];

	// Bucket the items
	if (T::typeid == System::Byte::typeid)
	{
		BucketInternal<unsigned __int8, unsigned __int8>((unsigned __int8*)pValues, index, length, (unsigned __int8*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::SByte::typeid)
	{
		BucketInternal<__int8, unsigned __int8>((__int8*)pValues, index, length, (__int8*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::UInt16::typeid)
	{
		BucketInternal<unsigned __int16, unsigned __int8>((unsigned __int16*)pValues, index, length, (unsigned __int16*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::Int16::typeid)
	{
		BucketInternal<__int16, unsigned __int8>((__int16*)pValues, index, length, (__int16*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::UInt32::typeid)
	{
		BucketInternal<unsigned __int32, unsigned __int8>((unsigned __int32*)pValues, index, length, (unsigned __int32*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::Int32::typeid)
	{
		BucketInternal<__int32, unsigned __int8>((__int32*)pValues, index, length, (__int32*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::UInt64::typeid)
	{
		BucketInternal<unsigned __int64, unsigned __int8>((unsigned __int64*)pValues, index, length, (unsigned __int64*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::Int64::typeid)
	{
		BucketInternal<__int64, unsigned __int8>((__int64*)pValues, index, length, (__int64*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::Single::typeid)
	{
		BucketInternal<float, unsigned __int8>((float*)pValues, index, length, (float*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::Double::typeid)
	{
		BucketInternal<double, unsigned __int8>((double*)pValues, index, length, (double*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else
	{
		throw gcnew NotImplementedException();
	}

	// Write the row total as the last countPerBucket value
	int bucketSum = 0;
	for (int i = 0; i < countPerBucket->Length - 1; ++i)
	{
		bucketSum += countPerBucket[i];
	}

	countPerBucket[countPerBucket->Length - 1] = bucketSum;
}

generic <typename T>
int SortBucketColumnN::BucketIndex(array<T>^ bucketMins, T value)
{
	pin_ptr<T> pBucketMins = &bucketMins[0];

	if (T::typeid == System::Byte::typeid)
	{
		return BucketIndexInternal<__int8>((__int8*)pBucketMins, bucketMins->Length, (__int8)value);
	}
	else if (T::typeid == System::SByte::typeid)
	{
		return BucketIndexInternal<unsigned __int8>((unsigned __int8*)pBucketMins, bucketMins->Length, (unsigned __int8)value);
	}
	else if (T::typeid == System::UInt16::typeid)
	{
		return BucketIndexInternal<unsigned __int16>((unsigned __int16*)pBucketMins, bucketMins->Length, (unsigned __int16)value);
	}
	else if (T::typeid == System::Int16::typeid)
	{
		return BucketIndexInternal<__int16>((__int16*)pBucketMins, bucketMins->Length, (__int16)value);
	}
	else if (T::typeid == System::UInt32::typeid)
	{
		return BucketIndexInternal<unsigned __int32>((unsigned __int32*)pBucketMins, bucketMins->Length, (unsigned __int32)value);
	}
	else if (T::typeid == System::Int32::typeid)
	{
		return BucketIndexInternal<__int32>((__int32*)pBucketMins, bucketMins->Length, (__int32)value);
	}
	else if (T::typeid == System::UInt64::typeid)
	{
		return BucketIndexInternal<unsigned __int64>((unsigned __int64*)pBucketMins, bucketMins->Length, (unsigned __int64)value);
	}
	else if (T::typeid == System::Int64::typeid)
	{
		return BucketIndexInternal<__int64>((__int64*)pBucketMins, bucketMins->Length, (__int64)value);
	}
	else if (T::typeid == System::Single::typeid)
	{
		return BucketIndexInternal<float>((float*)pBucketMins, bucketMins->Length, (float)value);
	}
	else if (T::typeid == System::Double::typeid)
	{
		return BucketIndexInternal<double>((double*)pBucketMins, bucketMins->Length, (double)value);
	}
	else
	{
		throw gcnew NotImplementedException();
	}
}

