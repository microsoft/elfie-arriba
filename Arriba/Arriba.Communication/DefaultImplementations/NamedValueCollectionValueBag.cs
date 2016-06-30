// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;

namespace Arriba.Communication
{
    /// <summary>
    /// Value bag implementation wrapping a NameValueCollection
    /// </summary>
    public class NameValueCollectionValueBag : IWritableValueBag
    {
        private NameValueCollection _context;
        public NameValueCollectionValueBag(NameValueCollection context)
        {
            _context = context;
        }

        public string this[string key]
        {
            get
            {
                return _context[key];
            }
        }

        public bool Contains(string key)
        {
            return _context[key] != null;
        }

        public bool TryGetValue(string key, out string value)
        {
            value = _context[key];
            return value != null;
        }

        public void Add(string key, string value)
        {
            _context.Add(key, value);
        }

        public void AddOrUpdate(string key, string value)
        {
            if (this.Contains(key))
            {
                _context[key] = value;
            }
            else
            {
                this.Add(key, value);
            }
        }

        public System.Collections.Generic.IEnumerable<System.Tuple<string, string>> ValuePairs
        {
            get
            {
                foreach (var key in _context.AllKeys)
                {
                    yield return Tuple.Create(key, _context[key]);
                }
            }
        }


        public bool TryGetValues(string key, out string[] values)
        {
            values = this.Contains(key) ? _context.GetValues(key) : null;
            return values != null && values.Length > 0;
        }
    }
}
