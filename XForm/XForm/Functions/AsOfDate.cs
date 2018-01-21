// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Columns;
using XForm.Data;

namespace XForm.Functions
{
    internal class AsOfDateBuilder : IFunctionBuilder
    {
        public string Name => "AsOfDate";
        public string Usage => "AsOfDate() [returns as-of-date report is requested for]";
        public Type ReturnType => typeof(DateTime);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            return new ConstantColumn(source, context.RequestedAsOfDateTime, typeof(DateTime));
        }
    }
}
