using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XForm.Test.Query
{
    [TestClass]
    public class PipelineTests
    {
        [TestMethod]
        public void ConfigurationLineParsing()
        {
            Assert.AreEqual("", string.Join("|", PipelineFactory.SplitConfigurationLine("")));
            Assert.AreEqual("Simple", string.Join("|", PipelineFactory.SplitConfigurationLine("Simple")));
            Assert.AreEqual("Simple", string.Join("|", PipelineFactory.SplitConfigurationLine("  Simple ")));
            Assert.AreEqual("Simple|settings", string.Join("|", PipelineFactory.SplitConfigurationLine(" Simple   settings")));
            Assert.AreEqual(@"read|C:\Download\Sample.csv", string.Join("|", PipelineFactory.SplitConfigurationLine(@"read ""C:\Download\Sample.csv""")));
            Assert.AreEqual(@"read|C:\Download\Sample.csv", string.Join("|", PipelineFactory.SplitConfigurationLine(@"read ""C:\Download\Sample.csv"" ")));
            Assert.AreEqual(@"value|""Quoted""", string.Join("|", PipelineFactory.SplitConfigurationLine(@"value """"""Quoted""""""")));
        }
    }
}
