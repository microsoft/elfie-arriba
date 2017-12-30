#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class Comparer16
		{
		public:
			static void Where(array<UInt16>^ left, Int32 index, Int32 length, Byte compareOperator, UInt16 right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
		};
	}
}
