// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    /// <summary>
    ///  BlockTwoArgumentFunction converts a Func&lt;XArray, XArray, XArray&gt; into a function in XForm.
    ///  It reads the source columns and calls the transform function for each batch.
    /// </summary>
    public class BlockTwoArgumentFunction : IXColumn
    {
        private string _name;
        private IXColumn _column1;
        private IXColumn _column2;
        private Func<XArray, XArray, XArray> _function;
        private Action _beforeBatch;

        public ColumnDetails ColumnDetails { get; private set; }

        private BlockTwoArgumentFunction(string name, Type returnType, IXColumn column1, IXColumn column2, Func<XArray, XArray, XArray> function, Action beforeBatch = null)
        {
            _name = name;
            _column1 = column1;
            _column2 = column2;
            _function = function;
            _beforeBatch = beforeBatch;
            this.ColumnDetails = new ColumnDetails(name, returnType);
        }

        public static IXColumn Build(string name, Type returnType, IXTable source, IXColumn column1, IXColumn column2, Func<XArray, XArray, XArray> function, Action beforeBatch = null)
        {
            return new BlockTwoArgumentFunction(name, returnType, column1, column2, function, beforeBatch);
        }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> getter1 = _column1.CurrentGetter();
            Func<XArray> getter2 = _column2.CurrentGetter();

            return () =>
            {
                _beforeBatch?.Invoke();
                return _function(getter1(), getter2());
            };
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> getter1 = _column1.SeekGetter();
            Func<ArraySelector, XArray> getter2 = _column2.SeekGetter();

            // If either side doesn't support seek, we can't seek
            if (getter1 == null || getter2 == null) return null;

            // Otherwise, convert from the underlying current getter
            return (selector) =>
            {
                _beforeBatch?.Invoke();
                return _function(getter1(selector), getter2(selector));
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
            return $"{_name}({_column1}, {_column2})";
        }
    }
}
