#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class Comparer
		{
		public:
			// AVX2 accelerated where comparing an array of bytes to a constant value
			static void Where(array<Byte>^ left, Int32 index, Int32 length, Byte compareOperator, Byte right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
			static void Where(array<SByte>^ left, Int32 index, Int32 length, Byte compareOperator, SByte right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);

			// AVX2 accelerated where comparing an array of ushort (two bytes) to a constant value
			static void Where(array<UInt16>^ left, Int32 index, Int32 length, Byte compareOperator, UInt16 right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
			static void Where(array<Int16>^ left, Int32 index, Int32 length, Byte compareOperator, Int16 right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);

			template<typename T>
			static void WhereSingle(T* set, int length, Byte compareOperator, T value, Byte booleanOperator, unsigned __int64* matchVector);
		};
	}
}
