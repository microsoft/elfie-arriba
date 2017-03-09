using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Elfie.Diagnostics
{
    /// <summary>
    ///  SearchResult wraps a query result count and a (potentially smaller)
    ///  set of returned results.
    /// </summary>
    /// <typeparam name="T">Type of result items</typeparam>
    public class SearchResult<T> : IEnumerable<T>
    {
        public int Count { get; set; }
        public IEnumerator<T> Matches { get; set; }
        private static IEnumerator<T> Empty = Enumerable.Empty<T>().GetEnumerator();

        public SearchResult()
        {
            this.Count = 0;
            this.Matches = Empty;
        }

        public SearchResult(int count, IEnumerator<T> matches)
        {
            this.Count = count;
            this.Matches = matches;
        }

        public SearchResult(ICollection<T> matches)
        {
            this.Count = matches.Count;
            this.Matches = matches.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.Matches;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Matches;
        }
    }
}
