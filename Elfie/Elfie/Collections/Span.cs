using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Elfie.Collections
{
    public class Span<T> : IEnumerable<T>
    {
        public T[] Array { get; private set; }
        public int Index { get; private set; }
        private int _length;

        public Span(T[] array) : this(array, 0, (array != null ? array.Length : 0))
        { }

        public Span(T[] array, int index, int length)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (index < 0) throw new ArgumentOutOfRangeException("index");
            if (length < 0 || index + length > array.Length) throw new ArgumentOutOfRangeException("length");

            this.Array = array;
            this.Index = index;
            this.Length = length;
        }

        public int Length
        {
            get => this._length;
            set
            {
                if (this.Index + value > this.Array.Length) throw new ArgumentOutOfRangeException();
                this._length = value;
            }
        }

        public int Capacity => this.Array.Length - this.Index;


        public T this[int index]
        {
            get => this.Array[this.Index + index];
            set => this.Array[this.Index + index] = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new SpanEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SpanEnumerator<T>(this);
        }

        private class SpanEnumerator<U> : IEnumerator<U>
        {
            private Span<U> _span;
            private int _index;

            public SpanEnumerator(Span<U> span)
            {
                this._span = span;
                this._index = -1;
            }

            public U Current => this._span[_index];
            object IEnumerator.Current => this._span[_index];

            public bool MoveNext()
            {
                return (++this._index < this._span.Length);
            }

            public void Reset()
            {
                this._index = -1;
            }

            public void Dispose()
            { }
        }
    }
}
