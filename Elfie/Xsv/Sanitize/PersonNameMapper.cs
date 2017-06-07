// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Xsv.Sanitize
{
    public class PersonName
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public override string ToString()
        {
            return $"{this.FirstName} {this.MiddleName} {this.LastName}";
        }
    }

    /// <summary>
    ///  PersonNameMapper maps hashes to plausible person names using US
    ///  common name data from:
    ///   https://www.ssa.gov/oact/babynames/limits.html
    ///   https://www.census.gov/topics/population/genealogy/data/2010_surnames.html
    /// </summary>
    public class PersonNameMapper : ISanitizeMapper
    {
        private string[] FirstAndMiddleNames { get; set; }
        private string[] LastNames { get; set; }

        public PersonNameMapper()
        {
            FirstAndMiddleNames = Resource.ReadAllStreamLines(@"Xsv.Sanitize.Data.FirstNames.txt");
            LastNames = Resource.ReadAllStreamLines(@"Xsv.Sanitize.Data.LastNames.txt");

            int middleNamesNeeded = int.MaxValue / this.LastNames.Length / this.FirstAndMiddleNames.Length;
            if (middleNamesNeeded > this.FirstAndMiddleNames.Length) throw new InvalidOperationException("Name sources didn't contain enough unique values.");
        }

        public string Generate(uint hash)
        {
            return GenerateName(hash).ToString();
        }

        public PersonName GenerateName(uint hash)
        {
            uint hashRemaining = hash;

            PersonName name = new PersonName();

            name.LastName = this.LastNames[Hashing.Extract(ref hashRemaining, this.LastNames.Length)];
            name.FirstName = this.FirstAndMiddleNames[Hashing.Extract(ref hashRemaining, this.FirstAndMiddleNames.Length)];
            name.MiddleName = this.FirstAndMiddleNames[Hashing.Extract(ref hashRemaining, this.FirstAndMiddleNames.Length)];

            return name;
        }
    }
}
