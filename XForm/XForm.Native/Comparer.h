// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once
using namespace System;

namespace XForm
{
	namespace Native
	{
		public ref class Comparer
		{
		public:
			// AVX2 accelerated where comparing [byte and short] (array to array) and (array to constant)
			static void Where(array<Byte>^ left, Int32 index, Int32 length, Byte compareOperator, Byte right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
			static void Where(array<SByte>^ left, Int32 index, Int32 length, Byte compareOperator, SByte right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
			static void Where(array<Boolean>^ left, Int32 index, Int32 length, Byte cOp, Boolean right, Byte bOp, array<UInt64>^ vector, Int32 vectorIndex);

			static void Where(array<UInt16>^ left, Int32 leftIndex, Int32 length, Byte compareOperator, UInt16 right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
			static void Where(array<UInt16>^ left, Int32 leftIndex, Byte compareOperator, array<UInt16>^ right, Int32 rightIndex, Int32 length, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
			static void Where(array<Int16>^ left, Int32 leftIndex, Int32 length, Byte compareOperator, Int16 right, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);
			static void Where(array<Int16>^ left, Int32 leftIndex, Byte compareOperator, array<Int16>^ right, Int32 rightIndex, Int32 length, Byte booleanOperator, array<UInt64>^ vector, Int32 vectorIndex);

			// Compare values to a constant [non-vector]
			template<typename T>
			static void WhereSingle(T* set, int length, Byte compareOperator, T value, Byte booleanOperator, unsigned __int64* matchVector);

			// Compare pairs of values [non-vector]
			template<typename T>
			static void WhereSingle(T* left, int length, Byte cOp, T* right, Byte bOp, unsigned __int64* matchVector);
		};
	}
}
