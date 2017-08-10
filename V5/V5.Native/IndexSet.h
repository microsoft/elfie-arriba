#pragma once
#include "Operator.h"
using namespace System;

namespace V5
{
	namespace Native
	{
		namespace Collections
		{
			public ref class IndexSetN
			{
			public:
				static Int32 Count(array<UInt64>^ vector);
				
				static Int32 Page(array<UInt64>^ vector, array<Int32>^ page, Int32% fromIndex);

				generic <typename T>
				static void Where(array<UInt64>^ vector, BooleanOperator bOp, array<T>^ values, CompareOperator cOp, T value, int offset, int length);
			};
		}
	}
}

