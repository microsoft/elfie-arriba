using Elfie.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;

namespace Xsv.Test.OnlyLatest
{
    [TestClass]
    public class OnlyLatestTests
    {
        [TestMethod]
        public void OnlyLatest_Basic()
        {
            Assembly xsvTest = Assembly.GetExecutingAssembly();
            Resource.SaveStreamFolderTo("Xsv.Test.OnlyLatest.Inputs", "OnlyLatestInputs", xsvTest);
            Resource.SaveStreamTo("Xsv.Test.OnlyLatest.OnlyLatest.Merged.Expected.csv", "OnlyLatest.Merged.Expected.csv", xsvTest);

            Program.Main(new string[] { "onlyLatest", "OnlyLatestInputs", "OnlyLatest.Merged.csv", "ID" });

            string expected = File.ReadAllText("OnlyLatest.Merged.Expected.csv");
            string actual = File.ReadAllText("OnlyLatest.Merged.csv");
            Assert.AreEqual(expected, actual);
        }
    }
}
