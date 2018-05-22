// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions.Date
{
    internal class DateSubtractBuilder : IFunctionBuilder
    {
        public string Name => "DateSubtract";
        public string Usage => "DateSubtract({DateTimeEnd}, {DateTimeStart})";
        public Type ReturnType => typeof(DateTime);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn endColumn = context.Parser.NextColumn(source, context, typeof(DateTime));
            IXColumn startColumn = context.Parser.NextColumn(source, context, typeof(DateTime));

            return SimpleTwoArgumentFunction<DateTime, DateTime, TimeSpan>.Build(
                Name,
                source,
                endColumn,
                startColumn,
                (end, start) => end - start
            );
        }
    }
}
