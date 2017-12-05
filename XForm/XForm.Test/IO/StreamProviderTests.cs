using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using XForm.IO;
using XForm.Test.Query;

namespace XForm.Test.IO
{
    [TestClass]
    public class StreamProviderTests
    {
        [TestMethod]
        public void LocalFileStreamProvider_Basics()
        {
            DataBatchEnumeratorTests.WriteSamples();

            // TODO: Come up with a full local database folder structure as a test sample instead; enumerate it and check operations.
            LocalFileStreamProvider provider = new LocalFileStreamProvider(".");
            Assert.AreEqual("", String.Join("\r\n", provider.Enumerate(".", true)));
        }
    }
}
