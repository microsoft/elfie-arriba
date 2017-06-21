#include "stdafx.h"
#include "Span.h"

#pragma managed
namespace V5
{
	namespace Collections
	{
		generic <typename T>
		Span<T>::Span(array<T>^ array)
		{
			if (array == nullptr) throw gcnew ArgumentNullException("array");
			this->_array = array;
			this->_index = 0;
			this->_length = array->Length;
		}

		generic <typename T>
		Span<T>::Span(array<T>^ array, int index, int length)
		{
			if (array == nullptr) throw gcnew ArgumentNullException("array");
			if (index < 0 || index >= array->Length) throw gcnew ArgumentOutOfRangeException("index");
			if (length < 0) throw gcnew ArgumentOutOfRangeException("length");
			if (index + length > array->Length) throw gcnew ArgumentOutOfRangeException("length");

			this->_array = array;
			this->_index = index;
			this->_length = length;
		}

		generic <typename T>
		array<T>^ Span<T>::Array::get()
		{
			return this->_array;
		}

		generic <typename T>
		Int32 Span<T>::Index::get()
		{
			return this->_index;
		}

		generic <typename T>
		Int32 Span<T>::Length::get()
		{
			return this->_length;
		}

		generic <typename T>
		void Span<T>::Length::set(Int32 value)
		{
			if (value > this->Capacity) throw gcnew ArgumentOutOfRangeException("value");
			this->_length = value;
		}

		generic <typename T>
		Int32 Span<T>::Capacity::get()
		{
			return this->_array->Length - this->_index;
		}

		generic <typename T>
		T Span<T>::default::get(Int32 index)
		{
			return this->_array[this->_index + index];
		}

		generic <typename T>
		void Span<T>::default::set(Int32 index, T value)
		{
			this->_array[this->_index + index] = value;
		}

		generic <typename T>
		IEnumerator^ Span<T>::GetBaseEnumerator()
		{
			return this->GetTypedEnumerator();
		}

		generic <typename T>
		Generic::IEnumerator<T>^ Span<T>::GetTypedEnumerator()
		{
			return gcnew SpanEnumerator<T>(this->_array, this->_index, this->_length);
		}

		/* --- SpanEnumerator --- */

		generic <typename T>
		SpanEnumerator<T>::SpanEnumerator(array<T>^ array, int index, int length)
		{
			this->_array = array;
			this->_index = index;
			this->_end = index + length;
			this->_current = index - 1;
		}

		generic <typename T>
		Object^ SpanEnumerator<T>::CurrentBase::get()
		{
			return this->_array[this->_current];
		}

		generic <typename T>
		T SpanEnumerator<T>::CurrentTyped::get()
		{
			return this->_array[this->_current];
		}

		generic <typename T>
		bool SpanEnumerator<T>::MoveNext()
		{
			this->_current++;
			return this->_current < this->_end;
		}

		generic <typename T>
		void SpanEnumerator<T>::Reset()
		{
			this->_current = this->_index - 1;
		}

		generic <typename T>
		SpanEnumerator<T>::~SpanEnumerator()
		{ }
	}
}

