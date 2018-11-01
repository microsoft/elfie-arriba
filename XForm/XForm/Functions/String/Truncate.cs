// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    internal class TruncateBuilder : IFunctionBuilder
    {
        public string Name => "Truncate";
        public string Usage => "Truncate({String8}, {LengthLimit})";
        public Type ReturnType => typeof(String8);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn value = context.Parser.NextColumn(source, context, typeof(String8));
            int limit = context.Parser.NextInteger();

            return SimpleTransformFunction<String8, String8>.Build(
                Name,
                source,
                value,
                (string8) => Truncate(string8, limit));
        }

        private String8 Truncate(String8 text, int limit)
        {
            if(text.Length < limit)
            {
                return text;
            }
            else
            {
                return text.Substring(0, limit);
            }
        }
    }
}
