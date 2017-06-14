#pragma once
using namespace System;

public ref class SortBucketColumnN
{
public:
	generic <typename T>
	static void Bucket(array<T>^ values, int index, int length, array<T>^ bucketMins, array<Byte>^ rowBucketIndex, array<Int32>^ countPerBucket, array<Boolean>^ isMultiValue);
	
	generic <typename T>
	static int BucketIndex(array<T>^ bucketMins, T value);
};

