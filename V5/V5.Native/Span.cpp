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
			this->_array = array;
			this->_index = 0;
			this->_length = array->Length;
		}

		generic <typename T>
		Span<T>::Span(array<T>^ array, int index, int length)
		{
			if (index < 0) throw gcnew ArgumentOutOfRangeException("index");
			if (index + length > array->Length) throw gcnew ArgumentOutOfRangeException("length");

			this->_array = array;
			this->_index = index;
			this->_length = length;
		}

		generic <typename T>
		Int32 Span<T>::Length::get()
		{
			return this->_length;
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
			return nullptr;
		}

		generic <typename T>
		Generic::IEnumerator<T>^ Span<T>::GetTypedEnumerator()
		{
			return nullptr;
		}
	}
}

