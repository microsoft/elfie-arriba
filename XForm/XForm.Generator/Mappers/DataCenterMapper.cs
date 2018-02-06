// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Generator.Mappers
{
    internal class DataCenterMapper
    {
        private static string[] s_northAmericaDataCenters = new string[] { "West US 2", "Central US", "East US 2" };

        public string Generate(string clientRegion, uint hash)
        {
            switch (clientRegion)
            {
                case "US":
                case "CA":
                    return s_northAmericaDataCenters[Hashing.Extract(ref hash, s_northAmericaDataCenters.Length)];
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
