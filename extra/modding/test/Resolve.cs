using System.Collections.Generic;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Resolve : Base
    {
        private static ModStatus Status(ModResolution resolution, string id)
        {
            return resolution.GetStatus(resolution.ModsInstalled.Single(mod => mod.Id == id));
        }

        [Test]
        public void BaseOnly()
        {
            var resolution = ResolvePure();
            CollectionAssert.AreEqual(new[] { BaseId }, Ids(resolution));
            AssertKinds(resolution);
        }

        [Test]
        public void RequiredInRange()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", version: "^1.2.0")), Mod("B", "1.3.0"));
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(resolution));
            AssertKinds(resolution);
        }

        [Test]
        public void RequiredMissingCascades()
        {
            var resolution = ResolvePure(
                Mod("A", "1.0.0", Dependency("B")),
                Mod("B", "1.0.0", Dependency("C")),
                Mod("Unrelated", "1.0.0"));

            AssertKinds(resolution, ModProblemKind.DependencyMissing, ModProblemKind.DependencyDisabled);
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "A"));
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "B"));
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "Unrelated"));
        }

        [Test]
        public void RequiredOutOfRange()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", version: "^2.0.0")), Mod("B", "1.5.0"));
            AssertKinds(resolution, ModProblemKind.DependencyVersion);
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "A"));
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "B"));
        }

        [TestCase(">=1.0.0", "2.0.0-beta")]
        [TestCase("^1.0.0", "1.5.0-beta")]
        [TestCase("=1.0.0", "1.0.0+build")]
        public void RequiredSatisfied(string range, string version)
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", version: range)), Mod("B", version));
            AssertKinds(resolution);
        }

        [TestCase("^1.0.0", "2.0.0-beta")]
        [TestCase("^1.0.0", "1.0.0-beta")]
        public void RequiredUnsatisfied(string range, string version)
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", version: range)), Mod("B", version));
            AssertKinds(resolution, ModProblemKind.DependencyVersion);
        }

        [TestCase("Required")]
        [TestCase("Recommended")]
        [TestCase("Accommodated")]
        public void WrongCase(string kind)
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("other.lib", kind: kind)), Mod("Other.Lib", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.DependencyIdCase);
            Assert.IsTrue(resolution.Problems[0].Message.Contains("Other.Lib"));
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "A"));
        }

        [Test]
        public void IncompatibleInRange()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible", version: "<2.0.0")), Mod("B", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.Incompatible);
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "A"));
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "B"));
        }

        [Test]
        public void IncompatibleOutOfRange()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible", version: "<2.0.0")), Mod("B", "2.0.0"));
            AssertKinds(resolution);
        }

        [Test]
        public void IncompatibleAbsent()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible")));
            AssertKinds(resolution);
        }

        [Test]
        public void IncompatibleMutual()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible")), Mod("B", "1.0.0", Dependency("A", kind: "Incompatible")));
            AssertKinds(resolution, ModProblemKind.Incompatible, ModProblemKind.Incompatible);
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "A"));
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "B"));
        }

        [Test]
        public void IncompatibleTargetAlreadyDisabled()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible")), Mod("B", "1.0.0", Dependency("Missing")));
            AssertKinds(resolution, ModProblemKind.DependencyMissing);
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "A"));
        }

        [Test]
        public void IncompatibleCascades()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible")), Mod("B", "1.0.0"), Mod("C", "1.0.0", Dependency("A")));
            AssertKinds(resolution, ModProblemKind.Incompatible, ModProblemKind.DependencyDisabled);
            Assert.AreEqual(ModStatus.Disabled, Status(resolution, "C"));
        }

        [Test]
        public void IncompatibleWithCycleMember()
        {
            // B is ruled out by its cycle before incompatibilities are checked, so A survives.
            var resolution = ResolvePure(
                Mod("A", "1.0.0", Dependency("B", kind: "Incompatible")),
                Mod("B", "1.0.0", Dependency("C", order: "After")),
                Mod("C", "1.0.0", Dependency("B", order: "After")));
            AssertKinds(resolution, ModProblemKind.Cycle);
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "A"));
        }

        [Test]
        public void IncompatibleChain()
        {
            // Evaluated all at once: A is disabled for B even though B is disabled for C.
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible")), Mod("B", "1.0.0", Dependency("C", kind: "Incompatible")), Mod("C", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.Incompatible, ModProblemKind.Incompatible);
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "C"));
        }

        [Test]
        public void Discouraged()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Discouraged")), Mod("B", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.Discouraged);
            Assert.AreEqual(ModProblemSeverity.Warning, resolution.Problems[0].Severity);
            Assert.AreEqual(3, resolution.Mods.Count);
        }

        [Test]
        public void DiscouragedOutOfRange()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Discouraged", version: "1.3.x")), Mod("B", "1.4.0"));
            AssertKinds(resolution);
        }

        [TestCase("Recommended")]
        [TestCase("Supported")]
        public void OptionalMissing(string kind)
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: kind)));
            AssertKinds(resolution, ModProblemKind.DependencyOptional);
            Assert.AreEqual(ModProblemSeverity.Info, resolution.Problems[0].Severity);
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "A"));
        }

        [TestCase("Recommended")]
        [TestCase("Supported")]
        public void OptionalOutOfRange(string kind)
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: kind, version: "^2.0.0")), Mod("B", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.DependencyOptional);
        }

        [Test]
        public void OptionalDisabled()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Supported")), Mod("B", "1.0.0", Dependency("Missing")));
            AssertKinds(resolution, ModProblemKind.DependencyMissing, ModProblemKind.DependencyOptional);
        }

        [Test]
        public void OptionalPresent()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Recommended")), Mod("B", "1.0.0"));
            AssertKinds(resolution);
        }

        [Test]
        public void AccommodatedMissing()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Accommodated")));
            AssertKinds(resolution);
        }

        [Test]
        public void BaseVersionRequired()
        {
            var resolution = ResolvePure("1.0.0", Mod("A", "1.0.0", Dependency(BaseId, version: "^2.0.0")));
            AssertKinds(resolution, ModProblemKind.DependencyVersion);
        }

        [Test]
        public void BaseVersionSupported()
        {
            // An explicit base entry replaces the implicit Required one.
            var resolution = ResolvePure("1.0.0", Mod("A", "1.0.0", Dependency(BaseId, kind: "Supported", version: "^2.0.0")));
            AssertKinds(resolution, ModProblemKind.DependencyOptional);
            CollectionAssert.AreEqual(new[] { BaseId, "A" }, Ids(resolution));
        }

        [Test]
        public void BaseIncompatible()
        {
            var resolution = ResolvePure("1.5.0", Mod("A", "1.0.0", Dependency(BaseId, kind: "Incompatible", version: "<2.0.0")));
            AssertKinds(resolution, ModProblemKind.Incompatible);
        }

        [TestCase("1.5.0", false)]
        [TestCase("2.1.0", true)]
        public void SplitEntries(string version, bool enabled)
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", version: ">=2.0.0"), Dependency("B", kind: "Incompatible", version: "<2.0.0")), Mod("B", version));
            Assert.AreEqual(enabled ? ModStatus.Enabled : ModStatus.Disabled, Status(resolution, "A"));
        }

        [Test]
        public void DependentOfSchemaUnsupported()
        {
            var problems = new List<ModProblem>();
            var future = ModRead("<Mod><schemaVersion>2</schemaVersion><id>B</id></Mod>", problems);
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, string>(), Mod("A", "1.0.0", Dependency("B")), future);
            AssertKinds(resolution, ModProblemKind.SchemaUnsupported, ModProblemKind.DependencyDisabled);
        }

        [Test]
        public void DependentOfInvalidVersion()
        {
            var problems = new List<ModProblem>();
            var broken = ModRead("<Mod><id>B</id><version>banana</version></Mod>", problems);
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, string>(), Mod("A", "1.0.0", Dependency("B", version: "^1.0.0")), broken);
            AssertKinds(resolution, ModProblemKind.Manifest, ModProblemKind.DependencyDisabled);
        }

        [Test]
        public void DependentOfMalformed()
        {
            var problems = new List<ModProblem>();
            Mod broken = null;
            ExpectErrors(() => broken = ModRead("<Mod><id>B</id>", problems), error => error.Contains("Malformed"));
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, string>(), Mod("A", "1.0.0", Dependency("B")), broken);
            AssertKinds(resolution, ModProblemKind.Manifest, ModProblemKind.DependencyMissing);
            Assert.IsTrue(resolution.ModsInstalled.Contains(broken));
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(broken));
        }

        [Test]
        public void AssemblyReservedElsewhere()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(new ModAssembly() { path = "/mods/A/Assemblies/Newtonsoft.Json.dll", name = "Newtonsoft.Json" });
            var resolution = ResolvePure("1.0.0", new List<ModProblem>(), new Dictionary<string, string>() { { "Newtonsoft.Json", "/game/Newtonsoft.Json.dll" } }, mod);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
        }

        [Test]
        public void AssemblyReservedSamePath()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(new ModAssembly() { path = "/mods/A/Assemblies/A.dll", name = "A" });
            var resolution = ResolvePure("1.0.0", new List<ModProblem>(), new Dictionary<string, string>() { { "A", "/mods/A/Assemblies/A.dll" } }, mod);
            AssertKinds(resolution);
        }

        [Test]
        public void AssemblyClash()
        {
            var a = Mod("A", "1.0.0");
            a.assemblies.Add(new ModAssembly() { path = "/mods/A/Assemblies/Lib.dll", name = "Lib" });
            var b = Mod("B", "1.0.0");
            b.assemblies.Add(new ModAssembly() { path = "/mods/B/Assemblies/lib.dll", name = "lib" });
            var resolution = ResolvePure(a, b);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            Assert.AreEqual(2, resolution.Problems[0].Mods.Count);
            CollectionAssert.AreEqual(new[] { BaseId }, Ids(resolution));
        }

        [Test]
        public void AssemblyClashWithDisabled()
        {
            var a = Mod("A", "1.0.0");
            a.assemblies.Add(new ModAssembly() { path = "/mods/A/Assemblies/Lib.dll", name = "Lib" });
            var b = Mod("B", "1.0.0", Dependency("Missing"));
            b.assemblies.Add(new ModAssembly() { path = "/mods/B/Assemblies/Lib.dll", name = "Lib" });
            var resolution = ResolvePure(a, b);
            AssertKinds(resolution, ModProblemKind.DependencyMissing);
            Assert.AreEqual(ModStatus.Enabled, Status(resolution, "A"));
        }

        [Test]
        public void ProblemsStableAcrossInputOrder()
        {
            var mods = new[]
            {
                Mod("A", "1.0.0", Dependency("Missing")),
                Mod("B", "1.0.0", Dependency("A")),
                Mod("C", "1.0.0", Dependency("D", kind: "Discouraged")),
                Mod("D", "1.0.0", Dependency("E", kind: "Recommended")),
            };

            var expected = ResolvePure(mods).Problems.Select(problem => problem.Message).ToList();
            Assert.AreEqual(4, expected.Count);

            var reversed = ResolvePure(mods.Reverse().ToArray()).Problems.Select(problem => problem.Message).ToList();
            CollectionAssert.AreEqual(expected, reversed);
        }
    }
}
