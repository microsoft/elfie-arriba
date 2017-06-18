// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Xsv.Sanitize
{
    /// <summary>
    ///  IntMapper maps hashes to integers (trivially).
    /// </summary>
    public class IntMapper : ISanitizeMapper
    {
        public string Generate(uint hash)
        {
            return hash.ToString();
        }
    }
}
