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
	if (value < *base) return index - 1;
	return index;
}

template <typename T, typename U>
void BucketInternal(T* values, int index, int length, T* bucketMins, int bucketCount, U* rowBucketIndex, int* countPerBucket)
{
	int end = index + length;
	for (int i = index; i < end; ++i)
	{
		int index = BucketIndexInternal<T>(bucketMins, bucketCount, values[i]);

		if (index < 0)
		{
			bucketMins[0] = values[i];
			index = 0;
		}
		else if (index >= bucketCount)
		{
			bucketMins[bucketCount - 1] = values[i];
			index = bucketCount - 1;
		}

		rowBucketIndex[i] = index;
		countPerBucket[index]++;
	}
}

#pragma managed

void SortBucketColumnN::Bucket(array<Int64>^ values, int index, int length, array<Int64>^ bucketMins, array<Byte>^ rowBucketIndex, array<Int32>^ countPerBucket)
{
	if (values->Length < (index + length)) return;
	if (rowBucketIndex->Length < values->Length) return;
	if (countPerBucket->Length != bucketMins->Length) return;

	pin_ptr<Int64> pValues = &values[0];
	pin_ptr<Int64> pBucketMins = &bucketMins[0];
	pin_ptr<Byte> pRowBucketIndex = &rowBucketIndex[0];
	pin_ptr<Int32> pCountPerBucket = &countPerBucket[0];
	BucketInternal<long long, unsigned char>(pValues, index, length, pBucketMins, bucketMins->Length, pRowBucketIndex, pCountPerBucket);
}

int SortBucketColumnN::BucketIndex(array<Int64>^ bucketMins, Int64 value)
{
	pin_ptr<Int64> pBucketMins = &bucketMins[0];
	return BucketIndexInternal<long long>(pBucketMins, bucketMins->Length, value);
}

