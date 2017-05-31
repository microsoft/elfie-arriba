// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private static IEnumerator<T> s_empty = Enumerable.Empty<T>().GetEnumerator();

        public SearchResult()
        {
            this.Count = 0;
            this.Matches = s_empty;
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
            // Give an enumerator pointing to the first item again. Breaks multiple simultaneous use. (Design Issue)
            this.Matches.Reset();
            return this.Matches;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // Give an enumerator pointing to the first item again. Breaks multiple simultaneous use. (Design Issue)
            this.Matches.Reset();
            return this.Matches;
        }
    }
}
