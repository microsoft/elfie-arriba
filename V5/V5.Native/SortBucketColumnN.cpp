#include "stdafx.h"
#include "SortBucketColumnN.h"

#pragma unmanaged

template <typename T>
int BucketIndexInternal(T* bucketMins, int bucketCount, T value)
{
	// Binary search for the last value less than the search value [the bucket the value should go into]
	T* base = bucketMins;

	int count = bucketCount;
	while (count > 1)
	{
		int half = count >> 1;
		base = (base[half] <= value ? &base[half] : base);
		count -= half;
	}

	int index = (int)(base - bucketMins);
	if (value != *base) return ~index;

	return index;
}

template <typename T, typename U>
void BucketInternal(T* values, int index, int length, T* bucketMins, int bucketCount, U* rowBucketIndex, int* countPerBucket, bool* isMultiValue)
{
	int end = index + length;
	for (int i = index; i < end; ++i)
	{
		int index = BucketIndexInternal<T>(bucketMins, bucketCount, values[i]);

		if (index < 0)
		{
			index = ~index;
			isMultiValue[index] = true;
			if (index == 0) bucketMins[0] = (values[i] < bucketMins[0] ? values[i] : bucketMins[0]);
		}
		
		if (index >= bucketCount - 1)
		{
			bucketMins[bucketCount - 1] = (values[i] > bucketMins[bucketCount - 1] ? values[i] : bucketMins[bucketCount - 1]);
			index = bucketCount - 2;
		}

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

	if (T::typeid == System::Int64::typeid)
	{
		BucketInternal<__int64, unsigned __int8>((__int64*)pValues, index, length, (__int64*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::Int32::typeid)
	{
		BucketInternal<__int32, unsigned __int8>((__int32*)pValues, index, length, (__int32*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
	else if (T::typeid == System::Int16::typeid)
	{
		BucketInternal<__int16, unsigned __int8>((__int16*)pValues, index, length, (__int16*)pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket, pIsMultiValue);
	}
}

generic <typename T>
int SortBucketColumnN::BucketIndex(array<T>^ bucketMins, T value)
{
	pin_ptr<T> pBucketMins = &bucketMins[0];

	if (T::typeid == System::Int64::typeid)
	{
		return BucketIndexInternal<__int64>((__int64*)pBucketMins, bucketMins->Length, (__int64)value);
	}
	else if (T::typeid == System::Int32::typeid)
	{
		return BucketIndexInternal<__int32>((__int32*)pBucketMins, bucketMins->Length, (__int32)value);
	}
	else if (T::typeid == System::Int16::typeid)
	{
		return BucketIndexInternal<__int16>((__int16*)pBucketMins, bucketMins->Length, (__int16)value);
	}

	return -2;
}

