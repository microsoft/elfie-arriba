// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XForm.Generator.Mappers
{
    internal class EmailAddressMapper
    {
        private static string[] s_providers = new string[] { "@gmail.com", "@hotmail.com", "@yahoo.com", "@facebook.com", "@verizon.net", "@comcast.net" };

        private AliasMapper AliasMapper { get; set; }

        public EmailAddressMapper()
        {
            this.AliasMapper = new AliasMapper();
        }

        public string Generate(uint hash)
        {
            return $"{this.AliasMapper.Generate(hash)}{s_providers[Hashing.Extract(ref hash, s_providers.Length)]}";
        }
    }
}
