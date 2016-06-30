// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arriba.Model
{
    /// <summary>
    /// This class creates a thread-safe memory pool wrapper for a given type
    /// </summary>
    /// <remarks>
    /// This class should be in cases where pooling/reusing temporary structures is needed to eliminate
    /// large storage or mass temporary allocations.  This is particularly good for use when there are per-partition 
    /// intermediate results being created in parallel operations.  The parallel operations only expand to # of CPUs
    /// while partition count can number in the thousands.
    /// </remarks>
    /// <typeparam name="T">type of object to pool</typeparam>
    public class ObjectCache<T>
    {
        private readonly object _lockObj = new object();
        private Queue<T> _freeList = new Queue<T>();
        //private int poolSize;

        private Func<T> _createNewFunc;

        /// <summary>
        /// Create a new instance of an ObjectCache using the specified function as factory (constructor)
        /// </summary>
        /// <param name="CreateNewFunction">[Optional] factory for new T's, uses default(T) if not specified</param>
        public ObjectCache(Func<T> CreateNewFunction)
        {
            _createNewFunc = CreateNewFunction;
        }

        /// <summary>
        /// Tries to get a value from the pool and returns a new instance if the pool is empty
        /// </summary>
        /// <param name="item">returned item</param>
        /// <returns>true if a value was retrieved from the pool or false otherwise</returns>
        public bool TryGet(out T item)
        {
            lock (_lockObj)
            {
                if (_freeList.Count == 0)
                {
                    if (_createNewFunc != null)
                    {
                        //poolSize++;
                        //Trace.WriteLine("Workspace pool increased to: " + poolSize);
                        T createNewFunc = _createNewFunc();

                        if (createNewFunc == null)
                        {
                            throw new InvalidOperationException("createNewFunc returned null");
                        }

                        item = createNewFunc;
                        return false;
                    }
                    else
                    {
                        item = default(T);
                        return false;
                    }
                }

                item = _freeList.Dequeue();
                return true;
            }
        }

        public void Put(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            lock (_lockObj)
            {
                _freeList.Enqueue(obj);
            }
        }

        public T[] ClearAndReturnAllItems()
        {
            lock (_lockObj)
            {
                T[] allItems = _freeList.ToArray();
                _freeList.Clear();
                return allItems;
            }
        }

        public int Count
        {
            get
            {
                lock (_lockObj)
                {
                    return _freeList.Count;
                }
            }
        }
    }
}
