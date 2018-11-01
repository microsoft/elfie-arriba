// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    internal class BetweenBuilder : IFunctionBuilder
    {
        public string Name => "Between";
        public string Usage => "Between({String8}, {StartDelimiter}, {EndDelimiter})";
        public Type ReturnType => typeof(String8);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn value = context.Parser.NextColumn(source, context, typeof(String8));
            string prefix = context.Parser.NextString();
            string suffix = context.Parser.NextString();

            String8 prefix8 = String8.Convert(prefix, new byte[String8.GetLength(prefix)]);
            String8 suffix8 = String8.Convert(suffix, new byte[String8.GetLength(suffix)]);

            return SimpleTransformFunction<String8, String8>.Build(
                Name,
                source,
                value,
                (string8) => Between(string8, prefix8, suffix8));
        }

        private String8 Between(String8 text, String8 prefix, String8 suffix)
        {
            // Find (first) prefix
            int index = text.IndexOf(prefix);

            // If prefix not found, return full text
            if (index == -1) return text;

            // If prefix at end of value, return empty
            int afterIndex = index + prefix.Length;
            if (afterIndex >= text.Length) return String8.Empty;

            // Find first suffix after prefix end
            int beforeIndex = text.IndexOf(suffix, afterIndex);

            // If suffix not found, return everything after prefix
            if (beforeIndex == -1) return text.Substring(afterIndex);

            // Otherwise, return between
            return text.Substring(afterIndex, beforeIndex - afterIndex);
        }
    }
}
