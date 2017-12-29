#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class Comparer16
		{
		public:
			static void WhereLessThan(array<UInt16>^ left, UInt16 right, Int32 index, Int32 length, array<UInt64>^ vector);
		};
	}
}
