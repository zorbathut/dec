using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Reflection : Base
    {
        [Test]
        public void UserAssembliesContainTestAssembly()
        {
            // The test assembly directly references Dec and defines Converters / StaticReferences, so it
            // must be in the closure - otherwise nothing in this test suite could work.
            var result = Dec.UtilReflection.GetAllUserAssemblies().ToArray();
            var names = result.Select(a => a.GetName().Name).ToArray();

            Assert.IsTrue(names.Contains("dec-test-unit"), $"Expected dec-test-unit in user-assembly closure, got: {string.Join(", ", names)}");
        }

        [Test]
        public void UserAssembliesIncludeDecItself()
        {
            // Required for the embedded-source configuration (Dec compiled directly into user assembly).
            // If this invariant breaks, the dec-test-integration-unified project fails with confusing
            // "StaticReferences initialized at inappropriate time" errors rather than a clear failure.
            var result = Dec.UtilReflection.GetAllUserAssemblies().ToArray();
            Assert.Contains(typeof(Dec.Dec).Assembly, result);
        }

        [Test]
        public void UserAssembliesExcludeThirdParty()
        {
            // Dec's reference-closure scan should not surface NUnit or CoreLib, neither of which
            // references Dec. These were the motivating cases for replacing the substring blocklist.
            var result = Dec.UtilReflection.GetAllUserAssemblies().ToArray();
            var names = result.Select(a => a.GetName().Name).ToArray();

            Assert.IsFalse(names.Contains("nunit.framework"), $"Did not expect nunit.framework in closure; got: {string.Join(", ", names)}");
            Assert.IsFalse(names.Contains("System.Private.CoreLib"), $"Did not expect System.Private.CoreLib in closure; got: {string.Join(", ", names)}");
        }

        [Test]
        public void UserTypesExcludeDecInternals()
        {
            // Dec's own types (e.g., Dec.Parser, Dec.Recorder, Dec.UtilType) should never surface through
            // GetAllUserTypes even though Dec's assembly is in the user-assembly closure.
            var types = Dec.UtilReflection.GetAllUserTypes().ToArray();
            Assert.IsFalse(types.Contains(typeof(Dec.Parser)), "Dec.Parser should not be surfaced as a user type");
            Assert.IsFalse(types.Contains(typeof(Dec.Recorder)), "Dec.Recorder should not be surfaced as a user type");
        }
    }
}
