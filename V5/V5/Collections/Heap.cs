using System;

namespace V5.Collections
{
    public class Heap<T>
    {
        private Comparison<T> _comparison;
        private Span<T> _items;
        private int _limit;

        public Heap(Comparison<T> comparison, Span<T> items, int limit)
        {
            if (limit > items.Capacity || limit < 0) throw new ArgumentOutOfRangeException("limit");

            this._comparison = comparison;
            this._items = items;
            this._limit = limit;
        }

        public Heap(Comparison<T> comparison, Span<T> items, int limit, int initialCount)
        {
            if (limit > items.Capacity || limit < 0) throw new ArgumentOutOfRangeException("limit");
            if (initialCount > items.Length || initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");

            this._comparison = comparison;
            this._items = items;
            this._limit = limit;

            // Order the items into heap order
            for (int i = 0; i < initialCount; ++i)
            {
                this.PercolateUp(items[i], i);
            }

            // Set the length to the count pre-added
            this._items.Length = initialCount;
        }

        public int Length => _items.Length;

        public T Peek()
        {
            if (this._items.Length == 0) throw new InvalidOperationException();
            return this._items[0];
        }

        public T Pop()
        {
            if (this._items.Length == 0) throw new InvalidOperationException();

            // Get the first item
            T min = this._items[0];

            // Remove the last item
            this._items.Length--;

            if (this._items.Length > 0)
            {
                // Find the correct place to re-insert it
                T valueToPlace = this._items[this._items.Length];
                int hole = 0;
                int child = hole * 2 + 1;
                int length = this.Length;

                while (child < length)
                {
                    // Find the smaller of the two children below the hole
                    int cmp = this._comparison(this._items[child], this._items[child + 1]);
                    if (cmp > 0) child++;

                    // Copy the smaller item into the hole and continue
                    this._items[hole] = this._items[child];
                    hole = child;
                    child = child * 2 + 1;
                }

                // If the last item had a single child, copy it up
                if (child == length) this._items[hole] = this._items[child - 1];

                this._items[hole] = valueToPlace;
            }

            // Clear the removed item [debuggability]
            this._items[this._items.Length] = default(T);

            return min;
        }

        public bool Push(T item)
        {
            if (this.Length >= this._limit) return false;

            this.PercolateUp(item, this.Length);
            this._items.Length++;

            return true;
        }

        private void PercolateUp(T item, int hole)
        {
            while (hole > 0)
            {
                // If the new item is smaller than the parent, stop
                int parent = (hole - 1) / 2;
                int cmp = this._comparison(item, this._items[parent]);
                if (cmp > 0) break;

                // Copy the parent into the hole and continue
                this._items[hole] = this._items[parent];
                hole = parent;
            }

            this._items[hole] = item;
        }
    }
}
