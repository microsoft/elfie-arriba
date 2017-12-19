// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using XForm.Data;

namespace XForm.Functions
{
    internal class AsOfDateBuilder : IFunctionBuilder
    {
        public string Name => "AsOfDate";
        public string Usage => "AsOfDate() [returns as-of-date report is requested for]";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return new Constant(source, context.RequestedAsOfDateTime, typeof(DateTime));
        }
    }
}
