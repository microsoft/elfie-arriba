// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Serialization;
using Arriba.Structures;

namespace Arriba.Structures
{
    /// <summary>
    /// A value dictionary represents simple key value storage of values. 
    /// </summary>
    public class ValueDictionary : IBinarySerializable
    {
        private readonly Dictionary<string, Value> _dictionary = new Dictionary<string, Value>();

        public bool TryGet<T>(string key, out T value)
        {
            Value dictValue;
            if (!_dictionary.TryGetValue(key, out dictValue))
            {
                value = default(T);
                return false;
            }

            return dictValue.TryConvert<T>(out value);
        }


        public IDictionary<string, object> AsDictionary()
        {
            return _dictionary.ToDictionary(k => k.Key, v => (object)v.Value);
        }

        public bool AddOrUpdate<T>(string key, T value)
        {
            Value wrapped = Value.Create(value);
            if (_dictionary.ContainsKey(key))
            {
                _dictionary[key] = wrapped;
                return false;
            }

            _dictionary.Add(key, wrapped);
            return true;
        }

        public bool TryRemove(string key)
        {
            return _dictionary.Remove(key);
        }

        public void ReadBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            // Read the number of items in the dictionary 
            int itemCount = context.Reader.ReadInt32();

            for (int i = 0; i < itemCount; i++)
            {
                string key = context.Reader.ReadString();
                string value = context.Reader.ReadString();

                _dictionary.Add(key, Value.Create(value));
            }
        }

        public void WriteBinary(ISerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            // Write the dictionary size.
            context.Writer.Write(_dictionary.Count);

            // Foreach item, write a string key, and the value
            foreach (var item in _dictionary)
            {
                // Write the key 
                context.Writer.Write(item.Key);

                // Write the value as a string 
                context.Writer.Write(item.Value.ToString());
            }
        }
    }
}
