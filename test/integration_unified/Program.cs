using System.IO;
using System.Linq;
using NUnit.Framework;

namespace DecTestIntegrationUnified
{
    [TestFixture]
    public class Program
    {
        [Test]
        public void Integration()
        {
            Directory.SetCurrentDirectory(new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent.Parent.Parent.FullName);

            Dec.Config.UsingNamespaces = new string[] { "DecTestIntegrationUnified" };

            var parser = new Dec.Parser();
            parser.AddDirectory("data");
            parser.Finish();

            Assert.IsNotNull(Dec.Database<IntegrationDec>.Get("ItemAlpha"));
            Assert.IsNotNull(Dec.Database<IntegrationDec>.Get("ItemBeta"));

            Assert.AreSame(IntegrationDecs.ItemAlpha, Dec.Database<IntegrationDec>.Get("ItemAlpha"));
        }

        [Test]
        public void RootNamespaceTypesInDecAssemblyAreSurfaced()
        {
            // In the embedded-source configuration, decAssembly IS the user's assembly, so user types at file scope (no namespace) need to be surfaced by GetAllUserTypes. Regression guard against the per-assembly namespace filter over-excluding null-namespace types.
            Assert.AreSame(typeof(Dec.Dec).Assembly, typeof(RootNamespaceType).Assembly);
            Assert.Contains(typeof(RootNamespaceType), Dec.UtilReflection.GetAllUserTypes().ToArray());
        }

        [Test]
        public void DecNamespaceUserTypesAreSurfaced()
        {
            // Someone copying Dec's source into their own assembly might put user code in the Dec namespace itself. Identity-based filtering (rather than namespace-based) lets those types through; the old namespace filter silently dropped them.
            Assert.AreSame(typeof(Dec.Dec).Assembly, typeof(Dec.DecNamespaceUserType).Assembly);
            Assert.Contains(typeof(Dec.DecNamespaceUserType), Dec.UtilReflection.GetAllUserTypes().ToArray());
        }
    }
}
