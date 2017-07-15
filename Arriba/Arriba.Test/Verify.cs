// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Arriba.Extensions;

namespace Arriba.Test
{
    public static class Verify
    {
        public static void Exception<T>(Action run, string expectedMessage = null) where T : Exception
        {
            try
            {
                run();
                Assert.Fail("Expected exception of type: '" + typeof(T).FullName + "' but no exception was thrown");
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    e = ((AggregateException)e).InnerException;
                }

                Assert.AreEqual(typeof(T), e.GetType(), "An exception was thrown but it was not of the expected type.");

                if (!String.IsNullOrEmpty(expectedMessage))
                {
                    Assert.AreEqual(expectedMessage, e.Message, "Exception didn't have expected mesage.");
                }
            }
        }

        public static void AreStringsEqual(string expected, string actual)
        {
            expected = expected.CanonicalizeNewlines();
            actual = actual.CanonicalizeNewlines();
            Assert.IsTrue(expected == actual, "{0} != {1}", expected, actual);
        }
    }
}
