using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AntPanel.Tests
{
    [TestClass]
    public class PluginMainTests
    {
        [TestMethod]
        public void TestNew()
        {
            var plugin = new PluginMain();
            Assert.AreEqual(1, plugin.Api);
            Assert.AreEqual("AntPanel", plugin.Name);
            Assert.AreEqual("92d9a647-6cd3-4347-9db6-95f324292399", plugin.Guid);
            Assert.AreEqual("Canab, SlavaRa", plugin.Author);
            Assert.AreEqual("AntPanel Plugin For FlashDevelop", plugin.Description);
            Assert.AreEqual("http://www.flashdevelop.org/community/", plugin.Help);
            Assert.AreEqual(0, plugin.BuildFilesList.Count);
            Assert.AreEqual("antPanelData.txt", plugin.StorageFileName);
        }
    }
}