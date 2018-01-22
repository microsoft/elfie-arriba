// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    internal class TrimBuilder : IFunctionBuilder
    {
        public string Name => "Trim";
        public string Usage => "Trim({String8})";
        public Type ReturnType => typeof(String8);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            return SimpleTransformFunction<String8, String8>.Build(
                Name,
                source,
                context.Parser.NextColumn(source, context, typeof(String8)),
                (string8) => string8.Trim());
        }
    }
}
