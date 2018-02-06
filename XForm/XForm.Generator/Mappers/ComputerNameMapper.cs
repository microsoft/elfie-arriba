// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

using Elfie.Serialization;

namespace XForm.Generator.Mappers
{
    /// <summary>
    ///  ComputerNameMapper maps hashes to plausible computer names.
    /// </summary>
    public class ComputerNameMapper : IValueMapper
    {
        private string[] ComputerNameWords { get; set; }

        public ComputerNameMapper()
        {
            this.ComputerNameWords = Resource.ReadAllStreamLines(@"XForm.Generator.Mappers.Data.ComputerNames.txt");
        }

        public string Generate(uint hash)
        {
            StringBuilder result = new StringBuilder();
            uint hashRemaining = hash;

            // Acronym
            for (int i = 0; i < 3; ++i)
            {
                result.Append((char)('a' + Hashing.Extract(ref hashRemaining, 26)));
            }

            // -?
            if (Hashing.Extract(ref hashRemaining, 2) == 1) result.Append("-");

            // Word
            result.Append(this.ComputerNameWords[Hashing.Extract(ref hashRemaining, this.ComputerNameWords.Length)]);

            // -?
            if (Hashing.Extract(ref hashRemaining, 2) == 1) result.Append("-");

            // Number Suffix
            result.Append(hashRemaining);

            return result.ToString();
        }
    }
}
