#pragma once
#include "Operator.h"
using namespace System;

namespace ElfieNative
{
	namespace Collections
	{
		public ref class IndexSetN
		{
		public:
			static Int32 Count(array<UInt64>^ vector);

			static Int32 Page(array<UInt64>^ vector, array<Int32>^ page, Int32% fromIndex);

			generic <typename T>
			static void Where(array<UInt64>^ vector, ElfieNative::Query::BooleanOperator bOp, array<T>^ values, ElfieNative::Query::CompareOperator cOp, T value, int offset, int length);
		};
	}
}

