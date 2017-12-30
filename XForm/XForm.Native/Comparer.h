#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class Comparer
		{
		public:
			static void Where(array<UInt16>^ left, Int32 index, Int32 length, Byte compareOperator, UInt16 right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);

			template<typename T>
			static void WhereSingle(T* set, int length, Byte compareOperator, T value, Byte booleanOperator, unsigned __int64* matchVector);
		};
	}
}
