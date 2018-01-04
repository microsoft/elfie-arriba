// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using XForm.Data;

namespace XForm.Functions.Date
{
    internal class DateAddBuilder : IFunctionBuilder
    {
        public string Name => "DateAdd";
        public string Usage => "DateAdd([DateTime], [TimeSpan])";
        public Type ReturnType => typeof(DateTime);

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            IDataBatchColumn baseDateTime = context.Parser.NextColumn(source, context, typeof(DateTime));
            TimeSpan offsetSpan = context.Parser.NextTimeSpan();

            return SimpleTransformFunction<DateTime, DateTime>.Build(
                Name,
                source,
                baseDateTime,
                (dateTime) => dateTime.Add(offsetSpan)
            );
        }
    }
}
