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
            // The test assembly directly references Dec and defines Converters / StaticReferences, so it must be in the closure - otherwise nothing in this test suite could work.
            var result = Dec.UtilReflection.GetAllUserAssemblies().ToArray();
            var names = result.Select(a => a.GetName().Name).ToArray();

            Assert.IsTrue(names.Contains("dec-test-unit"), $"Expected dec-test-unit in user-assembly closure, got: {string.Join(", ", names)}");
        }

        [Test]
        public void UserAssembliesIncludeDecItself()
        {
            // Required for the embedded-source configuration (Dec compiled directly into user assembly). If this invariant breaks, the dec-test-integration-unified project fails with confusing "StaticReferences initialized at inappropriate time" errors rather than a clear failure.
            var result = Dec.UtilReflection.GetAllUserAssemblies().ToArray();
            Assert.Contains(typeof(Dec.Dec).Assembly, result);
        }

        [Test]
        public void UserAssembliesExcludeThirdParty()
        {
            // Dec's reference-closure scan should not surface NUnit or CoreLib, neither of which references Dec. These were the motivating cases for replacing the substring blocklist.
            var result = Dec.UtilReflection.GetAllUserAssemblies().ToArray();
            var names = result.Select(a => a.GetName().Name).ToArray();

            Assert.IsFalse(names.Contains("nunit.framework"), $"Did not expect nunit.framework in closure; got: {string.Join(", ", names)}");
            Assert.IsFalse(names.Contains("System.Private.CoreLib"), $"Did not expect System.Private.CoreLib in closure; got: {string.Join(", ", names)}");
        }

        [Test]
        public void UserTypesExcludeDecConverterHierarchy()
        {
            // Discover every Converter-derived type in Dec's own assembly and assert it's excluded from GetAllUserTypes. Discovering reflectively (rather than listing the known types) means adding a new abstract Converter base or Nullable<> helper to Dec without updating UtilReflection.DecInternalNonUserTypes fails here immediately. Note this test relies on the non-embedded configuration: Dec.Dec's assembly is dec.dll, which contains only Dec's own Converter subclasses. In the embedded-source configuration, that assembly would also contain user Converters, so this test would need rework there; integration_unified doesn't duplicate it for that reason.
            var decAssembly = typeof(Dec.Dec).Assembly;
            var decConverters = decAssembly.GetTypes().Where(t => typeof(Dec.Converter).IsAssignableFrom(t)).ToArray();
            Assert.IsNotEmpty(decConverters, "Expected Dec's assembly to contain at least one Converter type; reflection lookup likely broken.");
            var surfaced = new System.Collections.Generic.HashSet<System.Type>(Dec.UtilReflection.GetAllUserTypes());
            foreach (var t in decConverters)
            {
                Assert.IsFalse(surfaced.Contains(t), $"Dec-internal Converter type {t} should not be surfaced as a user type. If this is a newly-added abstract base or Nullable<> helper, add it to UtilReflection.DecInternalNonUserTypes.");
            }
        }

        private class ClassWithAutoProperty
        {
            public int RegularField;
            public int AutoProperty { get; set; }
        }

        [Test]
        public void SerializableFieldsExcludesAutoPropertyBackingField()
        {
            // Auto-property backing fields are emitted by the compiler with [CompilerGenerated] and a "<Name>k__BackingField" name. GetSerializableFieldsFromHierarchy must skip them so the user's auto-properties aren't silently serialized as data members.
            var fields = Dec.UtilReflection.GetSerializableFieldsFromHierarchy(typeof(ClassWithAutoProperty));
            var names = fields.Select(f => f.Name).ToArray();

            Assert.AreEqual(new[] { "RegularField" }, names);
        }

        public class FieldOrderBase
        {
            public int baseFirst;
            public string baseSecond;
        }

        public class FieldOrderDerived : FieldOrderBase
        {
            public string derivedFirst;
            public int derivedSecond;
            public object derivedThird;
        }

        [Test]
        public void SerializableFieldsInDeclarationOrder()
        {
            // GetFields order is unspecified, so serialization walks fields in declaration order, derived class first, keeping composed output stable.
            var names = Dec.UtilReflection.GetSerializableFieldsFromHierarchy(typeof(FieldOrderDerived)).Select(f => f.Name).ToArray();

            Assert.AreEqual(new[] { "derivedFirst", "derivedSecond", "derivedThird", "baseFirst", "baseSecond" }, names);
        }
    }
}
