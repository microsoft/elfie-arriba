// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Data;

namespace XForm.Functions.String
{
    internal class ConcatBuilder : IFunctionBuilder
    {
        public string Name => "Concat";
        public string Usage => "Concat({String8}, ...)";
        public Type ReturnType => typeof(String8);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            List<IXColumn> columns = new List<IXColumn>();
            while (context.Parser.HasAnotherArgument)
            {
                columns.Add(context.Parser.NextColumn(source, context, typeof(String8)));
            }

            ColumnConcatenator columnConcatenator = new ColumnConcatenator();
            return SimpleMultiArgumentFunction<String8>.Build(
                Name,
                source,
                columns,
                columnConcatenator.Concatenate
            );
        }
    }

    internal class ColumnConcatenator
    {
        private String8[] _buffer;
        private bool[] _isNull;
        private String8Block _block;

        public ColumnConcatenator()
        {
            _block = new String8Block();
        }

        public XArray Concatenate(IList<XArray> columns)
        {
            _block.Clear();

            int count = columns.First().Count;
            Allocator.AllocateToSize(ref _buffer, count);
            Allocator.AllocateToSize(ref _isNull, count);

            bool couldBeNulls = columns.Any((col) => col.HasNulls);
            bool areAnyNulls = false;
            String8[][] arrays = columns.Select((xarray) => (String8[])xarray.Array).ToArray();

            if (!couldBeNulls)
            {
                for (int i = 0; i < count; ++i)
                {
                    String8 result = String8.Empty;

                    for (int columnIndex = 0; columnIndex < columns.Count; ++columnIndex)
                    {
                        int rowIndex = columns[columnIndex].Index(i);
                        result = _block.Concatenate(result, String8.Empty, arrays[columnIndex][rowIndex]);
                    }

                    _buffer[i] = result;
                }
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    String8 result = String8.Empty;
                    bool isNull = false;

                    for (int columnIndex = 0; columnIndex < columns.Count; ++columnIndex)
                    {
                        int rowIndex = columns[columnIndex].Index(i);
                        isNull |= columns[columnIndex].HasNulls && columns[columnIndex].NullRows[rowIndex];
                        result = _block.Concatenate(result, String8.Empty, arrays[columnIndex][rowIndex]);
                    }

                    _buffer[i] = result;
                    _isNull[i] = isNull;
                    areAnyNulls |= isNull;
                }
            }

            return XArray.All(_buffer, count, (areAnyNulls ? _isNull : null));
        }
    }
}
