// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace XForm.Functions
{
    public static class FunctionFactory
    {
        private static Dictionary<string, IFunctionBuilder> s_buildersByName;

        public static IEnumerable<string> SupportedFunctions(Type returnType = null)
        {
            EnsureLoaded();

            foreach (IFunctionBuilder builder in s_buildersByName.Values)
            {
                if (returnType == null || builder.ReturnType == null || builder.ReturnType == returnType) yield return builder.Name;
            }
        }

        public static bool TryGetBuilder(string functionName, out IFunctionBuilder builder)
        {
            EnsureLoaded();

            builder = null;
            return s_buildersByName.TryGetValue(functionName, out builder);
        }

        private static void EnsureLoaded()
        {
            if (s_buildersByName != null) return;

            // Initialize lookup Dictionaries
            s_buildersByName = new Dictionary<string, IFunctionBuilder>(StringComparer.OrdinalIgnoreCase);

            // Add configured type providers
            foreach (IFunctionBuilder provider in InterfaceLoader.BuildAll<IFunctionBuilder>())
            {
                s_buildersByName[provider.Name] = provider;
            }
        }
    }
}
