// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Xsv.Sanitize
{
    /// <summary>
    ///  ISanitizeMappers are classes which can generate sanitized values of
    ///  a particular type (ComputerName, IP, Phrase) given a uint hash.
    /// </summary>
    public interface ISanitizeMapper
    {
        /// <summary>
        ///  Generate a sanitized value for a given hash.
        /// </summary>
        /// <param name="hash">Hash value from which to generate result</param>
        /// <returns>Output value of the type the mapper produces</returns>
        string Generate(uint hash);
    }
}
