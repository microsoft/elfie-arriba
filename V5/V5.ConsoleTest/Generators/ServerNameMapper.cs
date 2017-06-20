using Xsv.Sanitize;

namespace V5.ConsoleTest.Generators
{
    class ServerNameMapper
    {
        private static string[] NameBases = { "WS-FRONT-V2", "WS-FRONT-BIG", "WS-FRONT" };
        public static string Generate(uint hash)
        {
            return $"{NameBases[Hashing.Extract(ref hash, 3)]}-{Hashing.Extract(ref hash, 32)}";
        }
    }
}
