#include "stdafx.h"
#include "Heap.h"

namespace V5
{
	namespace Collections
	{
		generic <typename T>
		Heap<T>::Heap(Comparison<T>^ comparison, Span<T>^ items, int limit)
		{
			this->_comparison = comparison;
			this->_items = items;
			this->_limit = limit;
		}

		generic <typename T>
		int Heap<T>::Length::get()
		{
			return this->_items->Length;
		}

		generic <typename T>
		T Heap<T>::Peek()
		{
			if (this->_items->Length == 0) return T();
			return this->_items[0];
		}

		generic <typename T>
		T Heap<T>::Pop()
		{
			if (this->_items->Length == 0) return T();
			
			// Get the first item
			T min = this->_items[0];

			// Remove the last item
			T valueToPlace = this->_items[this->_items->Length - 1];
			this->_items[this->_items->Length] = T();
			this->_items->Length--;
			
			// Find the correct place to re-insert it
			int hole = 0;
			int child = hole * 2 + 1;
			int length = this->Length;

			while (child < length)
			{
				// Find the smaller of the two children below the hole
				int cmp = this->_comparison(this->_items[child], this->_items[child + 1]);
				if (cmp > 0) child++;

				// Copy the smaller item into the hole and continue
				this->_items[hole] = this->_items[child];
				hole = child;
				child = child * 2 + 1;
			}
			
			// If the last item had a single child, copy it up
			if (child == length) this->_items[hole] = this->_items[child - 1];
			
			this->_items[hole] = valueToPlace;

			return min;
		}

		generic <typename T>
		bool Heap<T>::Push(T item)
		{
			if(this->Length >= this->_limit) return false;

			// Percolate Up: Find where the new element goes between the new place at the end and the root
			int hole = this->Length;
			while (hole > 0)
			{
				// If the new item is smaller than the parent, stop
				int parent = (hole - 1) / 2;
				int cmp = this->_comparison(item, this->_items[parent]);
				if (cmp > 0) break;

				// Copy the parent into the hole and continue
				this->_items[hole] = this->_items[parent];
				hole = parent;
			}

			this->_items[hole] = item;
			this->_items->Length++;

			return true;
		}
	}
}
