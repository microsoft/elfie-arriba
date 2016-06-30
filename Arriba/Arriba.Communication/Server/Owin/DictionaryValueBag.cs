// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Arriba.Communication;

namespace Arriba.Server
{
    internal class DictionaryValueBag : IWritableValueBag
    {
        private IDictionary<string, string[]> _dictionary;

        public DictionaryValueBag(IDictionary<string, string[]> dictionary)
        {
            _dictionary = dictionary;
        }
        public void Add(string key, string value)
        {
            string[] original;

            if (_dictionary.TryGetValue(key, out original))
            {
                Array.Resize(ref original, original.Length + 1);
                original[original.Length - 1] = value;
            }
            else
            {
                _dictionary.Add(key, new string[] { value });
            }
        }

        public void AddOrUpdate(string key, string value)
        {
            if (_dictionary.ContainsKey(key))
            {
                _dictionary[key] = new string[] { value };
            }
            else
            {
                _dictionary.Add(key, new string[] { value });
            }
        }

        public string this[string key]
        {
            get
            {
                return _dictionary[key][0];
            }
        }

        public bool Contains(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            string[] values;

            if (!_dictionary.TryGetValue(key, out values))
            {
                value = null;
                return false;
            }

            value = values[0];
            return true;
        }

        public bool TryGetValues(string key, out string[] values)
        {
            return _dictionary.TryGetValue(key, out values);
        }

        public IEnumerable<Tuple<string, string>> ValuePairs
        {
            get
            {
                foreach (var item in _dictionary)
                {
                    yield return Tuple.Create(item.Key, item.Value[0]);
                }
            }
        }
    }
}
