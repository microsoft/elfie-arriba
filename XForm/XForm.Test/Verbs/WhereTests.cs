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
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >= \"0\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] >= \"a\"").RunAndDispose());

            // String StartsWith
            Assert.AreEqual((long)111, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] |> \"1\"").RunAndDispose());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] |> \"999\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] |> \"10000\"").RunAndDispose());

            // String Contains
            Assert.AreEqual((long)19, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] : \"99\"").RunAndDispose());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] : \"999\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] : \"9999\"").RunAndDispose());

            // String Equals
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"9\"").RunAndDispose());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"999\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"9999\"").RunAndDispose());


            // EnumColumn
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] > \"2017\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] > \"2017-13\"").RunAndDispose());
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] < \"2017-13\"").RunAndDispose());

            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] |> \"2017-1\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] |> \"2117-1\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] |> \"017\"").RunAndDispose());

            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"017\"").RunAndDispose());
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"2017\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"2018\"").RunAndDispose());
            Assert.AreEqual((long)1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"-12-\"").RunAndDispose());

            // Matches Excel
            Assert.AreEqual((long)103, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0z\"").RunAndDispose());
            Assert.AreEqual((long)19, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"00Z\"").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0ZA\"").RunAndDispose());

            // Numeric
            Assert.AreEqual((long)999, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 0").RunAndDispose());
            Assert.AreEqual((long)998, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 1").RunAndDispose());
            Assert.AreEqual((long)500, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 499").RunAndDispose());
            Assert.AreEqual((long)1, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 998").RunAndDispose());
            Assert.AreEqual((long)0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 999").RunAndDispose());

            // Order shouldn't matter
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0z\" AND Cast([ID], Int32) > 499").RunAndDispose());
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 499 AND [EventTime] : \"0z\"").RunAndDispose());
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([ID], Int32) > 499\r\nwhere [EventTime] : \"0z\"").RunAndDispose());
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"0z\"\r\nwhere Cast([ID], Int32) > 499").RunAndDispose());
        }

        [TestMethod]
        public void Where_ContainsChaining()
        {
            // Useful, but want to find a test which causes IndexOutOfRangeException if bulk contains is used on the second clause.
            Assert.AreEqual((long)50, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [EventTime] : \"00:\"\r\nwhere [EventTime] : \"00\"\r\nlimit 50").RunAndDispose());
        }

        [TestMethod]
        public void Where_Cascading()
        {
            // Where all, then some
            Assert.AreEqual(58, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] != \"-1\"\r\nwhere [ClientBrowser]: \"Chrome 45\"").RunAndDispose());

            // Where none, then anything
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ID] = \"-1\"\r\nwhere [ClientBrowser]: \"Chrome 45\"").RunAndDispose());

            // Where some, then nothing
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ClientRegion] = \"US\"").RunAndDispose());

            // Where some, then same (all kept)
            Assert.AreEqual(235, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ClientRegion] = \"CN\"").RunAndDispose());

            // Where some, then all (all kept)
            Assert.AreEqual(235, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ID] != \"-1\"").RunAndDispose());

            // Where some, then narrowing (some kept)
            Assert.AreEqual(159, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientRegion] = \"CN\"\r\nwhere [ClientBrowser] : \"Chrome\"").RunAndDispose());
        }

        [TestMethod]
        public void Where_Enum()
        {
            int chrome45Count = 58;
            // Test queries with 'Where' on an enum column for varying 'Where enum' optimized paths.

            // Where for a value that's not found - optimizes to 'None'
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Not Found\"").RunAndDispose());

            // Where which includes all values - optimizes to 'All'
            Assert.AreEqual(1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] < \"ZZZ\"").RunAndDispose());

            // Where for a single value - optimizes to Equals a single enum
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] = \"Chrome 45\"").RunAndDispose());
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 45\"").RunAndDispose());
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]:: \"Chrome 45\"").RunAndDispose());
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] |> \"Chrome 45\"").RunAndDispose());

            // Where for all but one value - optimizes to Not Equals a single enum
            Assert.AreEqual(1000 - chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] > \"Chrome 45\"").RunAndDispose());
            Assert.AreEqual(1000 - chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] != \"Chrome 45\"").RunAndDispose());
            Assert.AreEqual(1000 - chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere not [ClientBrowser] : \"Chrome 45\"").RunAndDispose());

            // Where for a set - optimizes to a set contains check on enum values
            Assert.AreEqual(644, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] : \"Chrome\"").RunAndDispose());

            // Cascading Where with the same condition
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 45\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").RunAndDispose());

            // Cascading Where with a narrowing condition
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 4\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").RunAndDispose());

            // Cascading Where with conflicting conditions
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 5\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").RunAndDispose());
        }

        [TestMethod]
        public void Where_EmptyAndNull()
        {
            // Where Empty, enum and non-enum
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] = \"\"").RunAndDispose());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] != \"\"").RunAndDispose());

            // Where Not Empty, enum and non-enum
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] = \"\"").RunAndDispose());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] != \"\"").RunAndDispose());

            // Where Not Not Empty
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere not [IsPremiumUser] != \"\"").RunAndDispose());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere not [DaysSinceJoined] = \"\"").RunAndDispose());

            // Where Contains Empty, no matches (not an error so you can type without error)
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] : \"\"").RunAndDispose());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] : \"\"").RunAndDispose());

            // Where ContainsExact Empty, no matches, not an error
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] :: \"\"").RunAndDispose());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] :: \"\"").RunAndDispose());

            // Where StartsWith Empty, no matches, not an error
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] |> \"\"").RunAndDispose());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] |> \"\"").RunAndDispose());

            // Where null against strings (no matches)
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] = null").RunAndDispose());
            Assert.AreEqual(1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [IsPremiumUser] != null").RunAndDispose());
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] = null").RunAndDispose());
            Assert.AreEqual(1000, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [DaysSinceJoined] != null").RunAndDispose());

            // Where null after cast (matches)
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) = null").RunAndDispose());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) != null").RunAndDispose());

            // Where empty after cast (synonym for null)
            Assert.AreEqual(244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) = \"\"").RunAndDispose());
            Assert.AreEqual(1000 - 244, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) != \"\"").RunAndDispose());

            // Where invalid after cast (error)
            Verify.Exception<UsageException>(() => SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere Cast([IsPremiumUser], Boolean) != \"invalid\"").RunAndDispose());
        }
    }
}
