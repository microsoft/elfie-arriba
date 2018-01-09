﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Extensions;
using XForm.Types;

namespace XForm.Test.Extensions
{
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void ParseTimeSpanFriendly()
        { 
            //Assert.AreEqual(null, TypeConverterFactory.ConvertSingle((string)null, typeof(TimeSpan)));
            //Assert.AreEqual(null, TypeConverterFactory.ConvertSingle("", typeof(TimeSpan)));
            Assert.AreEqual(TimeSpan.FromSeconds(5.5), TypeConverterFactory.ConvertSingle("5.5s", typeof(TimeSpan)));
            Assert.AreEqual(TimeSpan.FromMinutes(15), TypeConverterFactory.ConvertSingle("15m", typeof(TimeSpan)));
            Assert.AreEqual(TimeSpan.FromHours(0.5), TypeConverterFactory.ConvertSingle("0.5h", typeof(TimeSpan)));
            Assert.AreEqual(TimeSpan.FromDays(7), TypeConverterFactory.ConvertSingle("7d", typeof(TimeSpan)));
        }
    }
}
