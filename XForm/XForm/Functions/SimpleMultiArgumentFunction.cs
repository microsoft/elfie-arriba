// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;

namespace XForm.Functions
{
    /// <summary>
    ///  SimpleThreeArgumentFunction converts a Func&lt;T, U, V, W&gt; into a function in XForm.
    ///  It reads the source columns, allocates result and null arrays, and passes non-null
    ///  values to the function.
    ///  
    ///  If your function requires an addition buffer for transformation (like a String8Block
    ///  to hold changed copies of strings), you can declare it in a scope the Func can see
    ///  and clear it in the 'beforexarray' action. See XForm.Functions.String.ToUpper.
    /// </summary>
    /// <typeparam name="T">Type output by the function</typeparam>
    public class SimpleMultiArgumentFunction<T> : IXColumn
    {
        private string _name;
        private IEnumerable<IXColumn> _columns;
        private Func<IList<XArray>, XArray> _function;
        public ColumnDetails ColumnDetails { get; private set; }

        private SimpleMultiArgumentFunction(string name, IEnumerable<IXColumn> columns, Func<IList<XArray>, XArray> function)
        {
            _name = name;
            _columns = columns;
            _function = function;
            this.ColumnDetails = new ColumnDetails(name, typeof(T));
        }

        public static IXColumn Build(string name, IXTable source, IEnumerable<IXColumn> columns, Func<IList<XArray>, XArray> function)
        {
            return new SimpleMultiArgumentFunction<T>(name, columns, function);
        }

        public Func<XArray> CurrentGetter()
        {
            IList<Func<XArray>> getters = _columns.Select((col) => col.CurrentGetter()).ToList();

            return () =>
            {
                IList<XArray> batches = getters.Select((getter) => getter()).ToList();
                return _function(batches);
            };
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            IList<Func<ArraySelector, XArray>> getters = _columns.Select((col) => col.SeekGetter()).ToList();

            // If either side doesn't support seek, we can't seek
            foreach (var getter in getters)
            {
                if (getter == null) { return null; }
            }

            // Otherwise, convert from the underlying current getter
            return (selector) =>
            {
                IList<XArray> batches = getters.Select((getter) => getter(selector)).ToList();
                return _function(batches);
            };
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        public Type IndicesType => null;

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        public override string ToString()
        {
            return $"{_name}({string.Join(", ", _columns)})";
        }
    }
}
