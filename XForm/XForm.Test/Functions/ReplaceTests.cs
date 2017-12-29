using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using XForm.Functions;

namespace XForm.Test.Functions
{
    [TestClass]
    public class ReplaceTests
    {
        [TestMethod]
        public void ReplaceFunction()
        {
            List<Tuple<String8, String8, String8, String8>> testCases = new List<Tuple<String8, String8, String8, String8>>();
            testCases.Add(Tuple.Create(String8.Empty, String8.Empty, "find".ToString8(), "replace".ToString8()));
            testCases.Add(Tuple.Create("N/A".ToString8(), String8.Empty, "N/A".ToString8(), String8.Empty));
            testCases.Add(Tuple.Create("N/A".ToString8(), "Not Applicable".ToString8(), "N/A".ToString8(), "Not Applicable".ToString8()));
            testCases.Add(Tuple.Create("the quick brown fox".ToString8(), "the quick red fox".ToString8(), "brown".ToString8(), "red".ToString8()));

            foreach (var testCase in testCases)
            {
                Debug.WriteLine("Testing " + testCase.ToString());
                Assert.AreEqual(testCase.Item2, ReplaceColumn.Replace(testCase.Item1, testCase.Item3, testCase.Item4, new String8Block()));
            }
        }
    }

    public static class String8Extensions
    {
        public static String8 ToString8(this string text)
        {
            return String8.Convert(text, new byte[String8.GetLength(text)]);
        }
    }
}
