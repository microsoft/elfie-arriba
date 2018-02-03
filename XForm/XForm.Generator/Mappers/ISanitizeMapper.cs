// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Generator.Mappers
{
    /// <summary>
    ///  IValueMappers are classes which can convert a uint hash into sample values of different logical types.
    /// </summary>
    public interface IValueMapper
    {
        /// <summary>
        ///  Generate a sanitized value for a given hash.
        /// </summary>
        /// <param name="hash">Hash value from which to generate result</param>
        /// <returns>Output value of the type the mapper produces</returns>
        string Generate(uint hash);
    }
}
