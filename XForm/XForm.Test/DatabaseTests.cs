using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XForm.Test
{
    [TestClass]
    public class DatabaseTests
    {
        [TestMethod]
        public void Database_Add()
        {
            SampleDatabase.Build();
        }
    }
}
