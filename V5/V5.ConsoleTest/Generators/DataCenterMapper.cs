using System;
using Xsv.Sanitize;

namespace V5.ConsoleTest.Generators
{
    class DataCenterMapper
    {
        private static string[] NorthAmericaDataCenters = new string[] { "West US 2", "Central US", "East US 2" };

        public string Generate(string clientRegion, uint hash)
        {
            switch (clientRegion)
            {
                case "US":
                case "CA":
                    return NorthAmericaDataCenters[Hashing.Extract(ref hash, NorthAmericaDataCenters.Length)];
                case "CN":
                    return "China East";
                case "JP":
                    return "Japan West";
                case "GB":
                case "DE":
                    return "Europe West";
                case "IN":
                    return "Central India";
                case "AU":
                    return "Australia East";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
