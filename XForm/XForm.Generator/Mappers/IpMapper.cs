// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace XForm.Generator.Mappers
{
    /// <summary>
    ///  IpMapper maps hashes into IPv4 addresses.
    /// </summary>
    public class IpMapper : IValueMapper
    {
        public string Generate(uint hash)
        {
            StringBuilder result = new StringBuilder();
            uint hashRemaining = hash;

            for (int i = 0; i < 4; ++i)
            {
                if (i > 0) result.Append(".");
                result.Append(Hashing.Extract(ref hashRemaining, 256));
            }

            return result.ToString();
        }
    }
}
