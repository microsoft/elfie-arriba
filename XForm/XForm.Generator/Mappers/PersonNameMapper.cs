// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Elfie.Serialization;

namespace XForm.Generator.Mappers
{
    /// <summary>
    ///  PersonNameMapper maps hashes to plausible person names using US
    ///  common name data from:
    ///   https://www.ssa.gov/oact/babynames/limits.html
    ///   https://www.census.gov/topics/population/genealogy/data/2010_surnames.html
    /// </summary>
    public class PersonNameMapper : IValueMapper
    {
        private string[] FirstAndMiddleNames { get; set; }
        private string[] LastNames { get; set; }

        public PersonNameMapper()
        {
            FirstAndMiddleNames = Resource.ReadAllStreamLines(@"XForm.Generator.Data.FirstNames.txt");
            LastNames = Resource.ReadAllStreamLines(@"XForm.Generator.Data.LastNames.txt");

            int middleNamesNeeded = int.MaxValue / this.LastNames.Length / this.FirstAndMiddleNames.Length;
            if (middleNamesNeeded > this.FirstAndMiddleNames.Length) throw new InvalidOperationException("Name sources didn't contain enough unique values.");
        }

        public string Generate(uint hash)
        {
            uint hashRemaining = hash;

            string last = this.LastNames[Hashing.Extract(ref hashRemaining, this.LastNames.Length)];
            string first = this.FirstAndMiddleNames[Hashing.Extract(ref hashRemaining, this.FirstAndMiddleNames.Length)];
            string middle = this.FirstAndMiddleNames[Hashing.Extract(ref hashRemaining, this.FirstAndMiddleNames.Length)];

            string result = $"{first} {middle} {last}";
            return result;
        }
    }
}
