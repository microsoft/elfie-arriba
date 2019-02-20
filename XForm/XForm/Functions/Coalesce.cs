using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;
using XForm.Query;

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
        }
    }

    public class CoalesceTransformFunction : IXColumn
    {
        private List<IXColumn> _columns = new List<IXColumn>();
        private ICoalescer _coalescer;

        public CoalesceTransformFunction(IXTable source, XDatabaseContext context)
        {
            while (context.Parser.HasAnotherArgument)
            {
                IXColumn column = context.Parser.NextColumn(source, context);
                _columns.Add(column);
            }

            ValidateAllTypesMatch(_columns);
            IXColumn firstColumn = _columns[0];
            IndicesType = firstColumn.IndicesType;
            ColumnDetails = new ColumnDetails(nameof(Coalesce), firstColumn.ColumnDetails.Type);
            _coalescer = (ICoalescer)Allocator.ConstructGenericOf(typeof(Coalescer<>), firstColumn.ColumnDetails.Type);
        }

        public ColumnDetails ColumnDetails { get; private set; }

        public Type IndicesType { get; private set; }

        public Func<XArray> CurrentGetter()
        {
            Func<XArray>[] getters = _columns.Select((column) => column.CurrentGetter()).ToArray();
            List<Func<ArraySelector, XArray>> selectedGetters = new List<Func<ArraySelector, XArray>>();

            foreach (Func<XArray> func in getters)
            {
                selectedGetters.Add((selector) => func());
            }

            return () => _coalescer.Convert(selectedGetters.ToArray(), null);
        }

        public Func<ArraySelector, XArray> SeekGetter()
        {
            Func<ArraySelector, XArray>[] getters = _columns
                .Select((column) => column.SeekGetter())
                .ToArray();

            return (selector) => _coalescer.Convert(getters, selector);
        }

        public Func<XArray> IndicesCurrentGetter()
        {
            return null;
        }

        public Func<ArraySelector, XArray> IndicesSeekGetter()
        {
            return null;
        }

        public Func<XArray> ValuesGetter()
        {
            return null;
        }

        public Func<object> ComponentGetter(string componentName)
        {
            return null;
        }

        private void ValidateAllTypesMatch(List<IXColumn> columns)
        {
            Type previousColumnType = null;
            foreach (IXColumn column in columns)
            {
                if (previousColumnType != null && previousColumnType != column.ColumnDetails.Type)
                {
                    throw new UsageException($"[{column.ColumnDetails.Name}] type of '{column.ColumnDetails.Type.ToString()}' does not match the previous columns type of '{previousColumnType.ToString()}'");
                }

                previousColumnType = column.ColumnDetails.Type;
            }
        }
    }

    internal interface ICoalescer
    {
        XArray Convert(Func<ArraySelector, XArray>[] getters, ArraySelector selector);
    }

    internal class Coalescer<T> : ICoalescer
    {
        T[] _buffer;
        bool[] _nullRowsBuffer;

        public XArray Convert(Func<ArraySelector, XArray>[] getters, ArraySelector selector)
        {
            XArray currentArray = getters[0](selector);
            T[] currentTyped = (T[])currentArray.Array;

            // If there are no nulls or only one column, return as-is
            if (!currentArray.HasNulls || getters.Length == 1) { return currentArray; }

            // Allocate result arrays for the coalesced results
            int rowCount = currentArray.Selector.Count;
            Allocator.AllocateToSize(ref _buffer, rowCount);
            Allocator.AllocateToSize(ref _nullRowsBuffer, rowCount);

            // Copy non-null values from the first column and count remaining nulls
            int nullCount = rowCount;
            for (int row = 0; row < rowCount; ++row)
            {
                _nullRowsBuffer[row] = true;
                int index = currentArray.Index(row);

                if (!currentArray.NullRows[index])
                {
                    _buffer[row] = currentTyped[index];
                    _nullRowsBuffer[row] = false;
                    nullCount--;
                }
            }

            // Replace nulls with non-nulls from each column
            for (int column = 1; column < getters.Length; column++)
            {
                currentArray = getters[column](selector);
                currentTyped = (T[])currentArray.Array;

                for (int row = 0; row < currentArray.Count; row++)
                {
                    int index = currentArray.Index(row);
                    if (_nullRowsBuffer[row] && (!currentArray.HasNulls || !currentArray.NullRows[index]))
                    {
                        _buffer[row] = currentTyped[index];
                        _nullRowsBuffer[row] = false;
                        nullCount--;
                    }
                }

                // If there are no nulls left, we can return
                if (nullCount <= 0) { return XArray.All(_buffer, rowCount); }
            }

            // If we got through all columns, return the values with any remaining nulls marked
            return XArray.All(_buffer, rowCount, _nullRowsBuffer);
        }
    }
}
