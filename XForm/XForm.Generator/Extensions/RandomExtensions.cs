// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace XForm.Generator.Extensions
{
    public static class RandomExtensions
    {
        public static int NormalDistribution(this Random r, int mean, int stdDev)
        {
            double u1 = r.NextDouble();
            double u2 = r.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + (int)(stdDev * randStdNormal);
        }

        public static float NormalDistribution(this Random r, float mean, float stdDev)
        {
            double u1 = r.NextDouble();
            double u2 = r.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + (int)(stdDev * randStdNormal);
        }
    }
}
