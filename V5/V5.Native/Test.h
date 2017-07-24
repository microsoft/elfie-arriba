#pragma once
using namespace System;

public enum class Scenario : char
{
	BandwidthAVX256,
	BandwidthAVX128,
	CompareToVectorAVX256,
	CompareToVectorAVX128,
	CompareToVectorTwoByteAVX128,
	Stretch4to8CompareToVectorAVX128,
	StretchGenericCompareToVectorAVX128
};

namespace V5
{
	public ref class Test
	{
	public:
		static __int64 Bandwidth(Scenario scenario, array<Byte>^ values, int bitsPerValue, int index, int length, array<UInt64>^ vector);
	};
}

