// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using XForm.Columns;
using XForm.Data;
using XForm.Types;
using XForm.Types.Computers;

namespace XForm.Functions.String
{
    internal class MultiplyBuilder : IFunctionBuilder
    {
        public string Name => "Multiply";
        public string Usage => "Multiply({Left}, {Right})";
        public Type ReturnType => typeof(long);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn left = CastedColumn.Build(source, context.Parser.NextColumn(source, context), typeof(long));
            IXColumn right = CastedColumn.Build(source, context.Parser.NextColumn(source, context), typeof(long));
            IXArrayComputer computer = new LongComputer();

            return BlockTwoArgumentFunction.Build("Product", ReturnType, source, left, right, computer.Multiply);
        }
    }
}
