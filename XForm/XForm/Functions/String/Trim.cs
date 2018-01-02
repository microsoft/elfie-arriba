// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    internal class TrimBuilder : IFunctionBuilder
    {
        public string Name => "Trim";
        public string Usage => "Trim([String8])";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            return SimpleTransformFunction<String8, String8>.Build(
                source,
                context.Parser.NextColumn(source, context),
                (string8) => string8.Trim());
        }
    }
}
