using System;
using System.Collections.Generic;
using XForm.Data;

namespace XForm.Functions
{
    public class Coalesce : IFunctionBuilder
    {
        public Type ReturnType => null;

        public string Usage => "Coalesce({Col|Func|Const}, ...)";

        public string Name => "Coalesce";

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            return new CoalesceTransformFunction(source, context);
            // Start with first column
            // If non nulls, return first column
            // Else, take all non-nulls from first, and replace nulls with non-nulls from second.
            throw new NotImplementedException();
        }
    }

    public class CoalesceTransformFunction : IXColumn
    {
        private Type _indiciesType;
        private List<IXColumn> _columns = new List<IXColumn>();

        public CoalesceTransformFunction(IXTable source, XDatabaseContext context)
        {
            do
            {
                IXColumn column = context.Parser.NextColumn(source, context);
                _columns.Add(column);
                if (System.String.IsNullOrEmpty(column.ColumnDetails.Name)) throw new ArgumentException($"Column {_columns.Count} passed to 'Column' wasn't assigned a name. Use 'AS [Name]' to assign names to every column selected.");
            } while (context.Parser.HasAnotherPart);
        }

        public ColumnDetails ColumnDetails => throw new NotImplementedException();

        public Type IndicesType
        {
            get
            {
                if (_indiciesType != null)
                    return _indiciesType;

                foreach (IXColumn column in _columns)
                {
                    // if more than one type exists, the output will be string.
                    if (_indiciesType != null && _indiciesType != column.ColumnDetails.Type)
                        return typeof(string);

                    _indiciesType = column.ColumnDetails.Type;
                }

                return _indiciesType;
            }
        }

        public Func<object> ComponentGetter(string componentName)
        {
            throw new NotImplementedException();
        }

        public Func<XArray> CurrentGetter()
        {
            return () => Convert();
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            throw new NotImplementedException();
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            throw new NotImplementedException();
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            throw new NotImplementedException();
        }

        public Func<XArray> ValuesGetter()
        {
            throw new NotImplementedException();
        }

        public XArray Convert()
        {
            XArray xArray = _columns[0].CurrentGetter()();
            if (!xArray.HasNulls)
                return xArray;

            xArray = XArray.All(xArray.Array, xArray.Count, xArray.NullRows);
            for (int column = 1; column < _columns.Count; column++)
            {
                XArray currentArray = _columns[column].CurrentGetter()();
                for (int row = 0; row < xArray.Count; row++)
                {
                    if (!xArray.NullRows[row])
                        continue;

                    if (currentArray.NullRows[row])
                        continue;

                    xArray.NullRows[row] = false;
                    xArray.Array.SetValue(currentArray.Array.GetValue(row), row);
                }

                if (!currentArray.HasNulls)
                    return xArray;
            }

            return xArray;
        }
    }
}
