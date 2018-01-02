﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Extensions;

namespace XForm.Test.Extensions
{
    [TestClass]
    public class StringExtensionsTests
    {
        [TestMethod]
        public void String_ParseTimeSpanFriendly()
        {
            Verify.Exception<ArgumentException>(() => ((string)null).ParseTimeSpanFriendly());
            Verify.Exception<ArgumentException>(() => ("").ParseTimeSpanFriendly());

            Assert.AreEqual(TimeSpan.FromSeconds(5.5), "5.5s".ParseTimeSpanFriendly());
            Assert.AreEqual(TimeSpan.FromMinutes(15), "15m".ParseTimeSpanFriendly());
            Assert.AreEqual(TimeSpan.FromHours(0.5), "0.5h".ParseTimeSpanFriendly());
            Assert.AreEqual(TimeSpan.FromDays(7), "7d".ParseTimeSpanFriendly());
        }
    }
}
