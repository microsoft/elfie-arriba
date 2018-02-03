// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Generator.Mappers
{
    internal class ServerNameMapper
    {
        private static string[] s_nameBases = { "WS-FRONT-V2", "WS-FRONT-BIG", "WS-FRONT" };
        public static string Generate(uint hash)
        {
            return $"{s_nameBases[Hashing.Extract(ref hash, 3)]}-{Hashing.Extract(ref hash, 32)}";
        }
    }
}
