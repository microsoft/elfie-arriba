#pragma once
#include "Operator.h"
#include "Span.h"
using namespace System;

namespace V5
{
	namespace Collections
	{
		public ref class IndexSet
		{
		private:
			array<UInt64>^ bitVector;

		public:
			IndexSet();
			IndexSet(UInt32 length);

			// Get/Set bits and see Count set
			property Boolean default[Int32] { bool get(Int32 index); void set(Int32 index, Boolean value); }
			property Int32 Count { Int32 get(); }
			property Int32 Capacity { Int32 get(); }

			virtual Boolean Equals(Object^ other) override;

			Int32 Page(Span<Int32>% page, Int32 fromIndex);

			// Set to None/All quickly
			IndexSet^ None();
			IndexSet^ All(UInt32 length);

			// Set operations
			IndexSet^ And(IndexSet^ other);
			IndexSet^ AndNot(IndexSet^ other);
			IndexSet^ Or(IndexSet^ other);

			// Native speed Where with array values
			generic <typename T>
			IndexSet^ Where(BooleanOperator bOp, array<T>^ values, CompareOperator cOp, T value);
		};
	}
}

