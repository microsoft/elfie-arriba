// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace XForm.Generator.Mappers
{
    /// <summary>
    ///  AliasMapper maps hashes to aliases appropriate for people or groups.
    ///  [Up to 8 letters, dashed prefix sometimes]
    /// </summary>
    public class AliasMapper : IValueMapper
    {
        public AliasMapper()
        { }

        public string Generate(uint hash)
        {
            StringBuilder result = new StringBuilder();
            uint hashRemaining = hash;

            while (hashRemaining > 0)
            {
                result.Append((char)('A' + Hashing.Extract(ref hashRemaining, 26)));
            }

            return result.ToString();
        }
    }
}
