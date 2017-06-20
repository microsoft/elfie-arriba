using Xsv.Sanitize;

namespace V5.ConsoleTest.Generators
{
    class EmailAddressMapper
    {
        private static string[] Providers = new string[] { "@gmail.com", "@hotmail.com", "@yahoo.com", "@facebook.com", "@verizon.net", "@comcast.net" };

        private AliasMapper AliasMapper { get; set; }

        public EmailAddressMapper()
        {
            this.AliasMapper = new AliasMapper();
        }

        public string Generate(uint hash)
        {
            return $"{this.AliasMapper.Generate(hash)}{Providers[Hashing.Extract(ref hash, Providers.Length)]}";
        }
    }
}
