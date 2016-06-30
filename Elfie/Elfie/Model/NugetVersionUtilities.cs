// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Elfie.Model
{
    public static class NugetVersionUtilities
    {
        public static bool IsPrereleaseVersion(this string version)
        {
            return version != null && version.Contains("-");
        }
    }
}
