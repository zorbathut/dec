using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DecTestIntegration
{
    [TestFixture]
    public class Program
    {
        [Test]
        public void Integration()
        {
            Directory.SetCurrentDirectory(new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent.Parent.Parent.FullName);

            Dec.Config.UsingNamespaces = new string[] { "DecTestIntegration" };

            var parser = new Dec.Parser();
            parser.AddDirectory("data");
            parser.Finish();

            Assert.IsNotNull(Dec.Database<IntegrationDec>.Get("ItemAlpha"));
            Assert.IsNotNull(Dec.Database<IntegrationDec>.Get("ItemBeta"));

            Assert.AreSame(IntegrationDecs.ItemAlpha, Dec.Database<IntegrationDec>.Get("ItemAlpha"));
        }
    }
}
