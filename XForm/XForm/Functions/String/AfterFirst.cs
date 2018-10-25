// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;

using XForm.Data;

namespace XForm.Functions.String
{
    internal class AfterFirstBuilder : IFunctionBuilder
    {
        public string Name => "AfterFirst";
        public string Usage => "AfterFirst({String8}, {Delimiter})";
        public Type ReturnType => typeof(String8);

        public IXColumn Build(IXTable source, XDatabaseContext context)
        {
            IXColumn value = context.Parser.NextColumn(source, context, typeof(String8));
            string prefix = context.Parser.NextString();
            String8 prefix8 = String8.Convert(prefix, new byte[String8.GetLength(prefix)]);

            return SimpleTransformFunction<String8, String8>.Build(
                Name,
                source,
                value,
                (string8) => AfterFirst(string8, prefix8));
        }

        private String8 AfterFirst(String8 text, String8 value)
        {
            int index = text.IndexOf(value);
            if (index == -1) return text;

            int afterIndex = index + value.Length;
            if (afterIndex >= text.Length) return String8.Empty;

            return text.Substring(afterIndex);
        }
    }
}
