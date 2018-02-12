// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    internal class IsNullBuilder : IFunctionBuilder
    {
        public string Name => "IsNull";
        public string Usage => "IsNull({Col|Func|Const})";
        public Type ReturnType => typeof(bool);


        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn column = context.Parser.NextColumn(source, context);
            return new NullTransformFunction(column);
        }
    }

    internal class NullTransformFunction : IXColumn
    {
        private IXColumn _column;

        public NullTransformFunction(IXColumn column)
        {
            _column = column;
            ColumnDetails = new ColumnDetails($"{column.ColumnDetails.Name}.IsNull", typeof(bool));
        }

        public ColumnDetails ColumnDetails { get; private set; }

        public Type IndicesType => _column.IndicesType;

        public Func<XArray> CurrentGetter()
        {
            Func<XArray> sourceGetter = _column.CurrentGetter();
            if (ValuesGetter() != null)
            {
                // Get values mapped and replace values array with the converted array 
                Func<XArray> valuesGetter = ValuesGetter();
                return () => sourceGetter().ReplaceValues(valuesGetter());
            }
            else
            {
                // Otherwise, convert from the underlying current getter
                return () => Convert(sourceGetter());
            }
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray> sourceGetter = _column.SeekGetter();
            if (ValuesGetter() != null)
            {
                // Get values mapped and replace values array with the converted array 
                Func<XArray> valuesGetter = ValuesGetter();
                return (selector) => sourceGetter(selector).ReplaceValues(valuesGetter());
            }
            else
            {
                // Otherwise, convert from the underlying current getter
                return (selector) => Convert(sourceGetter(selector));
            }
        }

        public Func<XArray> ValuesGetter()
        {
            Func<XArray> innerGetter = _column.ValuesGetter();

            if (innerGetter == null)
                return null;

            return () => Convert(innerGetter());
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            return _column.IndicesCurrentGetter();
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return _column.IndicesSeekGetter();
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        private XArray Convert(XArray array)
        {
            if (!array.HasNulls)
            {
                return XArray.AllFalse(array.Count);
            }

            return array.ReplaceValues(array.NullRows);
        }
    }
}
