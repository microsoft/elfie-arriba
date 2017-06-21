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
		private:
			array<T>^ _array;
			int _index;
			int _length;

		public:
			Span(array<T>^ array);
			Span(array<T>^ array, int index, int length);

			property Int32 Length { Int32 get(); }
			property T default[Int32] { T get(Int32 index); void set(Int32 index, T value); }
			
			virtual IEnumerator^ GetBaseEnumerator() = IEnumerable::GetEnumerator;
			virtual Generic::IEnumerator<T>^ GetTypedEnumerator() = Generic::IEnumerable<T>::GetEnumerator;
		};

		generic <typename T>
		public ref struct SpanEnumerator : Generic::IEnumerator<T>
		{
		private:
			Span<T>^ _span;
			int _index;

		public:
			SpanEnumerator(Span<T>^ span);
			virtual ~SpanEnumerator();

			virtual property Object^ CurrentBase { Object^ get() = IEnumerator::Current::get; }
			virtual property T CurrentTyped { T get() = Generic::IEnumerator<T>::Current::get; }

			virtual bool MoveNext();
			virtual void Reset();
		};
	}
}

