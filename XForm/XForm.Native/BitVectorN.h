#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class BitVectorN
		{
		public:
			static Int32 Count(array<UInt64>^ vector);
			static Int32 Page(array<UInt64>^ vector, array<Int32>^ indicesFound, Int32% fromIndex);
		};
	}
}
