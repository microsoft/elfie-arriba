// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Core;
using XForm.Data;

namespace XForm.Test.Core
{
    [TestClass]
    public class SamplerTests
    {
        [TestMethod]
        public void Sampler_Basics()
        {
            Random r = new Random(8);
            ArraySelector all = ArraySelector.All(10240);

            int[] eighthArray = null;
            ArraySelector eighth = Sampler.Eighth(all, r, ref eighthArray);
            AssertClose(all.Count / 8, eighth.Count, 0.2f);

            int[] sixtyfourthArray = null;
            ArraySelector sixtyfourth = Sampler.Eighth(eighth, r, ref sixtyfourthArray);
            AssertClose(eighth.Count / 8, sixtyfourth.Count, 0.2f);
        }

        public static void AssertClose(int expected, int actual, float errorAllowed)
        {
            float percentageError = Math.Abs((float)(actual - expected) / (float)expected);
            if (percentageError > errorAllowed) Assert.Fail($"Value {actual:n0} was not within {errorAllowed:p0} of expected value {expected:n0}.");
        }
    }
}
