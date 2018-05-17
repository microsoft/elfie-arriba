using Microsoft.VisualStudio.TestTools.UnitTesting;
using XForm.Extensions;

namespace XForm.Test.Verbs
{
    [TestClass]
    public class WhereTests
    {
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
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 45\"").RunAndDispose());

            // Where for all but one value - optimizes to Not Equals a single enum
            Assert.AreEqual(1000 - chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] > \"Chrome 45\"").RunAndDispose());

            // Where for a set - optimizes to a set contains check on enum values
            Assert.AreEqual(644, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser] : \"Chrome\"").RunAndDispose());

            // Cascading Where with the same condition
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 45\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").RunAndDispose());

            // Cascading Where with a narrowing condition
            Assert.AreEqual(chrome45Count, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 4\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").RunAndDispose());

            // Cascading Where with conflicting conditions
            Assert.AreEqual(0, SampleDatabase.XDatabaseContext.Query("read WebRequest\r\nwhere [ClientBrowser]: \"Chrome 5\"\r\nwhere [ClientBrowser] = \"Chrome 45\"").RunAndDispose());
        }
    }
}
