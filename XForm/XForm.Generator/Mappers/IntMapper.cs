// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace XForm.Generator.Mappers
{
    /// <summary>
    ///  IntMapper maps hashes to integers (trivially).
    /// </summary>
    public class IntMapper : IValueMapper
    {
        public string Generate(uint hash)
        {
            return hash.ToString();
        }
    }
}
