using System;

using Elfie.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using XForm.Extensions;
using XForm.Query;

namespace XForm.Test.Verbs
{
    [TestClass]
    public class WhereTests
    {
        [TestMethod]
        public void Where_Variations()
        {
            // String >
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >= \"0\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >= \"a\"").Count());

            // String StartsWith
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] |> \"\"").Count());
            Assert.AreEqual((long)111, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] |> \"1\"").Count());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] |> \"999\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] |> \"10000\"").Count());

            // String EndsWith
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >| \"\"").Count());
            Assert.AreEqual((long)99, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >| \"9\"").Count());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >| \"999\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >| \"10000\"").Count());

            // String Contains
            Assert.AreEqual((long)19, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] : \"99\"").Count());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] : \"999\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] : \"9999\"").Count());

            // String Equals
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"9\"").Count());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"999\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"9999\"").Count());


            // EnumColumn
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] > \"2017\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] > \"2017-13\"").Count());
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] < \"2017-13\"").Count());

            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] |> \"2017-1\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] |> \"2117-1\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] |> \"017\"").Count());

            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] >| \"Z\"").Count());
            Assert.AreEqual((long)103, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] >| \"0Z\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] >| \":00\"").Count());

            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"017\"").Count());
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"2017\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"2018\"").Count());
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"-12-\"").Count());

            // Matches Excel
            Assert.AreEqual((long)103, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0z\"").Count());
            Assert.AreEqual((long)19, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"00Z\"").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0ZA\"").Count());

            // Numeric
            Assert.AreEqual((long)999, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 0").Count());
            Assert.AreEqual((long)998, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 1").Count());
            Assert.AreEqual((long)500, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 499").Count());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 998").Count());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 999").Count());

            // Order shouldn't matter
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0z\" AND Cast([ID], Int32) > 499").Count());
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 499 AND [EventTime] : \"0z\"").Count());
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 499\r\nwhere [EventTime] : \"0z\"").Count());
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0z\"\r\nwhere Cast([ID], Int32) > 499").Count());
        }

        [TestMethod]
        public void Where_ContainsChaining()
        {
            // Useful, but want to find a test which causes IndexOutOfRangeException if bulk contains is used on the second clause.
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"00:\"\r\nwhere [EventTime] : \"00\"\r\nlimit 50").Count());
        }

        [TestMethod]
        public void Where_Cascading()
        {
            // Where all, then some
            Assert.AreEqual(58, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] != \"-1\"\r\nwhere [ClientBrowser]: \"Chrome 45\"").Count());

            // Where none, then anything
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"-1\"\r\nwhere [ClientBrowser]: \"Chrome 45\"").Count());

            // Where some, then nothing
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ClientRegion] = \"US\"").Count());

            // Where some, then same (all kept)
            Assert.AreEqual(235, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ClientRegion] = \"CN\"").Count());

            // Where some, then all (all kept)
            Assert.AreEqual(235, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ID] != \"-1\"").Count());

            // Where some, then narrowing (some kept)
            Assert.AreEqual(159, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ClientBrowser] : \"Chrome\"").Count());
        }

        [TestMethod]
        public void Where_Enum()
        {
            int chrome45Count = 58;
            // Test queries with 'Where' on an enum column for varying 'Where enum' optimized paths.

            // Where for a value that's not found - optimizes to 'None'
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Not Found\"").Count());

            // Where which includes all values - optimizes to 'All'
            Assert.AreEqual(1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] < \"ZZZ\"").Count());

            // Where for a single value - optimizes to Equals a single enum
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] = \"Chrome 45\"").Count());
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 45\"").Count());
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]:: \"Chrome 45\"").Count());
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] |> \"Chrome 45\"").Count());

            // Where for all but one value - optimizes to Not Equals a single enum
            Assert.AreEqual(1000 - chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] > \"Chrome 45\"").Count());
            Assert.AreEqual(1000 - chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] != \"Chrome 45\"").Count());
            Assert.AreEqual(1000 - chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere not [ClientBrowser] : \"Chrome 45\"").Count());

            // Where for a set - optimizes to a set contains check on enum values
            Assert.AreEqual(644, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] : \"Chrome\"").Count());

            // Cascading Where with the same condition
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 45\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").Count());

            // Cascading Where with a narrowing condition
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 4\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").Count());

            // Cascading Where with conflicting conditions
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 5\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").Count());
        }

        [TestMethod]
        public void Where_EmptyAndNull()
        {
            // Where Empty, enum and non-enum
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] = \"\"").Count());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] != \"\"").Count());

            // Where Not Empty, enum and non-enum
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] = \"\"").Count());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] != \"\"").Count());

            // Where Not Not Empty
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere not [IsPremiumUser] != \"\"").Count());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere not [DaysSinceJoined] = \"\"").Count());

            // Where Contains Empty, no matches (not an error so you can type without error)
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] : \"\"").Count());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] : \"\"").Count());

            // Where ContainsExact Empty, no matches, not an error
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] :: \"\"").Count());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] :: \"\"").Count());

            // Where StartsWith Empty, no matches, not an error
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] |> \"\"").Count());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] |> \"\"").Count());

            // Where null against strings (no matches)
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] = null").Count());
            Assert.AreEqual(1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] != null").Count());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] = null").Count());
            Assert.AreEqual(1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] != null").Count());

            // Where null after cast (matches)
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) = null").Count());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) != null").Count());

            // Where empty after cast (synonym for null)
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) = \"\"").Count());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) != \"\"").Count());

            // Where invalid after cast (error)
            Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) != \"invalid\"").Count());
        }

        [TestMethod]
        public void Where_Parallel()
        {
            XDatabaseContext historicalContext = new XDatabaseContext(SampleDatabase.XDatabaseContext) { RequestedAsOfDateTime = new DateTime(2017, 12, 04, 00, 00, 00, DateTimeKind.Utc) };
            Assert.AreEqual(3000, historicalContext.Query("readRange \"3d\" WebRequest\r\nwhere [ID] != \"\"").Count());
            Assert.AreEqual(732, historicalContext.Query("readRange \"3d\" WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) = \"\"").Count());
        }
    }
}
