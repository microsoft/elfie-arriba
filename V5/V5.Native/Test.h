#pragma once
using namespace System;

public enum class Scenario : char
{
	BandwidthAVX256,
	BandwidthAVX128,
	CompareAndCountAVX128,
	StretchCompareAndCountAVX128
};

namespace V5
{
	public ref class Test
	{
	public:
		static __int64 Bandwidth(Scenario scenario, array<Byte>^ values, int bitsPerValue, int index, int length);
	};
}

