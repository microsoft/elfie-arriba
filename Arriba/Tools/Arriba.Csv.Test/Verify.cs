// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Csv.Test
{
    public class Verify
    {
        public static void Exception(Action action, Type expectedExceptionType, string expectedExceptionMessage)
        {
            try
            {
                action();
                Assert.Fail("Expected exception from action but no exception was thrown.");
            }
            catch (Exception ex)
            {
                if (expectedExceptionType != null)
                {
                    Assert.AreEqual(expectedExceptionType.Name, ex.GetType().Name);
                }

                if (expectedExceptionMessage != null)
                {
                    Assert.AreEqual(expectedExceptionMessage, ex.Message);
                }
            }
        }
    }
}
