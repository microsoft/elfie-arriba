// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CodeAnalysis.Elfie.Test
{
    public static class AssertStrings
    {
        private const string AssertStringsDirectory = "AssertStrings";
        private static string s_expectDirectory = Path.Combine(AssertStringsDirectory, "Expect");
        private static string s_actualDirectory = Path.Combine(AssertStringsDirectory, "Actual");

        private static int s_failureCount = 0;

        public static bool AreEqual(string expected, string actual)
        {
            // If both null, success
            if (expected == null && actual == null) return true;

            // If one null, default message is fine
            if (expected == null || actual == null)
            {
                Assert.AreEqual(expected, actual);
            }

            // If equal, success
            if (expected.Equals(actual)) return true;

            // Write failures to a folder
            if (s_failureCount == 0)
            {
                s_failureCount++;

                if (Directory.Exists(AssertStringsDirectory)) Directory.Delete(AssertStringsDirectory, true);

                Directory.CreateDirectory(AssertStringsDirectory);
                Directory.CreateDirectory(s_expectDirectory);
                Directory.CreateDirectory(s_actualDirectory);

                string fileName = String.Format("Failure{0}.txt", s_failureCount);
                File.WriteAllText(Path.Combine(s_expectDirectory, fileName), expected);
                File.WriteAllText(Path.Combine(s_actualDirectory, fileName), actual);
            }

            // Log the failure with a diff command
            Assert.Fail(
                string.Format(
                    "AssertStrings.AreEqual failed\r\nDiff: windiff \"{0}\" \"{1}\"\r\nExpected:\r\n{2}\r\nActual:{3}\r\n",
                    Path.GetFullPath(s_expectDirectory),
                    Path.GetFullPath(s_actualDirectory),
                    expected,
                    actual));

            return false;
        }
    }
}
