#pragma once
using namespace System;
using namespace System::Collections;

namespace V5
{
	namespace Collections
	{
		generic <typename T>
		public value struct Span : Generic::IEnumerable<T>
		{
		internal:
			property array<T>^ Array { array<T>^ get(); }
			property Int32 Index { Int32 get(); }

		public:
			array<T>^ _array;
			int _index;
			int _length;

			Span(array<T>^ array);
			Span(array<T>^ array, int index, int length);

			property Int32 Length { Int32 get(); void set(Int32 value); }
			property Int32 Capacity { Int32 get(); }

			property T default[Int32] { T get(Int32 index); void set(Int32 index, T value); }
			
			virtual IEnumerator^ GetBaseEnumerator() = IEnumerable::GetEnumerator;
			virtual Generic::IEnumerator<T>^ GetTypedEnumerator() = Generic::IEnumerable<T>::GetEnumerator;
		};

		generic <typename T>
		public ref struct SpanEnumerator : Generic::IEnumerator<T>
		{
		private:
			array<T>^ _array;
			int _index;
			int _end;
			int _current;

		internal:
			SpanEnumerator(array<T>^ array, int index, int length);

		public:
			virtual ~SpanEnumerator();

			virtual property Object^ CurrentBase { Object^ get() = IEnumerator::Current::get; }
			virtual property T CurrentTyped { T get() = Generic::IEnumerator<T>::Current::get; }

			virtual bool MoveNext();
			virtual void Reset();
		};
	}
}

