#pragma once
using namespace System;
#include "Span.h"

namespace V5
{
	namespace Collections
	{
		generic <typename T>
		public ref class Heap
		{
		private:
			Comparison<T>^ _comparison;
			Span<T>^ _items;
			int _limit;

		public:
			Heap(Comparison<T>^ comparison, Span<T>^ items, int limit);

			property Int32 Length { Int32 get(); }

			T Peek();
			T Pop();
			bool Push(T item);
		};
	}
}

