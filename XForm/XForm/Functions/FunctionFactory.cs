// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using XForm.Extensions;

namespace XForm.Functions
{
    public static class FunctionFactory
    {
        private static Dictionary<string, IFunctionBuilder> s_buildersByName;

        public static IEnumerable<string> SupportedFunctions
        {
            get
            {
                EnsureLoaded();
                return s_buildersByName.Keys;
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
