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
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, AssemblyReserved>(), Mod("A", "1.0.0", Dependency("B")), future);
            AssertKinds(resolution, ModProblemKind.SchemaUnsupported, ModProblemKind.DependencyDisabled);
        }

        [Test]
        public void DependentOfInvalidVersion()
        {
            var problems = new List<ModProblem>();
            var broken = ModRead("<Mod><id>B</id><version>banana</version></Mod>", problems);
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, AssemblyReserved>(), Mod("A", "1.0.0", Dependency("B", version: "^1.0.0")), broken);
            AssertKinds(resolution, ModProblemKind.Manifest, ModProblemKind.DependencyDisabled);
        }

        [Test]
        public void DependentOfMalformed()
        {
            var problems = new List<ModProblem>();
            Mod broken = null;
            ExpectErrors(() => broken = ModRead("<Mod><id>B</id>", problems), error => error.Contains("Malformed"));
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, AssemblyReserved>(), Mod("A", "1.0.0", Dependency("B")), broken);
            AssertKinds(resolution, ModProblemKind.Manifest, ModProblemKind.DependencyMissing);
            Assert.IsTrue(resolution.ModsInstalled.Contains(broken));
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(broken));
        }

        private static ModResolution ResolveWithGame(Dictionary<string, AssemblyReserved> game, params Mod[] mods)
        {
            return ResolvePure("1.0.0", new List<ModProblem>(), game, mods);
        }

        private static ModResolution ResolveOff(string[] off, params Mod[] mods)
        {
            return ResolveWith("1.0.0", new List<ModProblem>(), new Dictionary<string, AssemblyReserved>(), null, off, mods);
        }

        [Test]
        public void Off()
        {
            var a = Mod("A", "1.0.0");
            var b = Mod("B", "1.0.0");
            var resolution = ResolveOff(new[] { "A" }, a, b);
            AssertKinds(resolution);
            Assert.AreEqual(ModStatus.Off, resolution.GetStatus(a));
            CollectionAssert.AreEqual(new[] { BaseId, "B" }, Ids(resolution));
        }

        // Ids are case-sensitive, so turning off `a` leaves `A` alone.
        [Test]
        public void OffCase()
        {
            var a = Mod("A", "1.0.0");
            var resolution = ResolveOff(new[] { "a" }, a);
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(a));
        }

        [Test]
        public void OffCascades()
        {
            var a = Mod("A", "1.0.0", Dependency("B"));
            var b = Mod("B", "1.0.0");
            var resolution = ResolveOff(new[] { "B" }, a, b);
            AssertKinds(resolution, ModProblemKind.DependencyDisabled);
            Assert.AreEqual(ModStatus.Off, resolution.GetStatus(b));
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(a));
        }

        // A mod's problems don't matter while it's off, and it doesn't count against other mods.
        [Test]
        public void OffUnchecked()
        {
            var missing = Mod("Missing.Dependency", "1.0.0", Dependency("Nowhere"));
            var incompatible = Mod("Incompatible", "1.0.0", Dependency("B", kind: "Incompatible"));
            var b = Mod("B", "1.0.0");
            var excluded = Mod("Excluded", "1.0.0");
            var excluder = Mod("Excluder", "1.0.0", Dependency("Excluded", kind: "Incompatible"));
            var clash = Mod("Clash", "1.0.0");
            clash.assemblies.Add(AssemblyMod("/mods/Clash/Assemblies/Lib.dll", "Lib, Version=1.0.0.0"));
            var owner = Mod("Owner", "1.0.0");
            owner.assemblies.Add(AssemblyMod("/mods/Owner/Assemblies/Lib.dll", "Lib, Version=1.0.0.0"));

            var resolution = ResolveOff(new[] { "Missing.Dependency", "Incompatible", "Excluded", "Clash" }, missing, incompatible, b, excluded, excluder, clash, owner);
            AssertKinds(resolution);
            CollectionAssert.AreEquivalent(new[] { BaseId, "B", "Excluder", "Owner" }, Ids(resolution));
        }

        // A player who turned a dependency off needs to hear something different from one whose dependency is broken.
        [Test]
        public void OffCascadeMessage()
        {
            var off = ResolveOff(new[] { "B" }, Mod("A", "1.0.0", Dependency("B")), Mod("B", "1.0.0"));
            var broken = ResolveOff(new string[0], Mod("A", "1.0.0", Dependency("B")), Mod("B", "1.0.0", Dependency("Nowhere")));
            var messageOff = off.Problems.Single(problem => problem.Kind == ModProblemKind.DependencyDisabled).Message;
            var messageBroken = broken.Problems.Single(problem => problem.Kind == ModProblemKind.DependencyDisabled).Message;
            Assert.AreNotEqual(messageBroken, messageOff);
        }

        [Test]
        public void OffDuplicate()
        {
            var older = Mod("A", "1.0.0");
            var newer = Mod("A", "2.0.0");
            var resolution = ResolveOff(new[] { "A" }, older, newer);
            AssertKinds(resolution, ModProblemKind.Duplicate);
            Assert.AreEqual(ModStatus.Off, resolution.GetStatus(newer));
            Assert.AreEqual(ModStatus.Hidden, resolution.GetStatus(older));
        }

        [Test]
        public void OffOptional()
        {
            var a = Mod("A", "1.0.0", Dependency("B", kind: "Recommended"));
            var b = Mod("B", "1.0.0");
            var resolution = ResolveOff(new[] { "B" }, a, b);
            AssertKinds(resolution, ModProblemKind.DependencyOptional);
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(a));
        }

        // Turning off a broken mod is how a player stops hearing about it, though the mod screen still lists what's wrong.
        [Test]
        public void OffManifestError()
        {
            var problems = new List<ModProblem>();
            var broken = ModRead("<Mod><id>A</id></Mod>", problems);
            var resolution = ResolveWith("1.0.0", problems, new Dictionary<string, AssemblyReserved>(), null, new[] { "A" }, broken);
            Assert.AreEqual(ModStatus.Off, resolution.GetStatus(broken));
            AssertKinds(resolution, ModProblemKind.Manifest);
        }

        // Turning off either of two mods whose ids differ only in case clears the conflict.
        [Test]
        public void OffIdConflict()
        {
            var lower = Mod("a", "1.0.0");
            var upper = Mod("A", "1.0.0");
            var resolution = ResolveOff(new[] { "a" }, lower, upper);
            AssertKinds(resolution);
            Assert.AreEqual(ModStatus.Off, resolution.GetStatus(lower));
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(upper));
        }

        // Explicit load orders in a cycle can't be satisfied, unless one side of it is off.
        [Test]
        public void OffCycle()
        {
            var a = Mod("A", "1.0.0", Dependency("B", order: "After"));
            var b = Mod("B", "1.0.0", Dependency("A", order: "After"));
            var resolution = ResolveOff(new[] { "B" }, a, b);
            AssertKinds(resolution, ModProblemKind.DependencyDisabled);

            var c = Mod("C", "1.0.0", Dependency("D", kind: "Accommodated", order: "After"));
            var d = Mod("D", "1.0.0", Dependency("C", kind: "Accommodated", order: "After"));
            resolution = ResolveOff(new[] { "D" }, c, d);
            AssertKinds(resolution);
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(c));
        }

        [Test]
        public void OffDiscouraged()
        {
            var a = Mod("A", "1.0.0", Dependency("B", kind: "Discouraged"));
            var b = Mod("B", "1.0.0");
            var resolution = ResolveOff(new[] { "B" }, a, b);
            AssertKinds(resolution);
        }

        // A game that builds Dec's source into its own assembly, Game.
        private static ModResolution ResolveDecBuiltIn(Dictionary<string, AssemblyReserved> game, params Mod[] mods)
        {
            return ResolveWith("1.0.0", new List<ModProblem>(), game, "Game", new string[0], mods);
        }

        // Dec's standalone assembly is `dec`, but a reference may spell it either way.
        [TestCase("dec")]
        [TestCase("Dec")]
        public void DecSeparateShipped(string name)
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod($"/mods/A/Assemblies/{name}.dll", $"{name}, Version=1.0.0.0"));
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0", $"{name}, Version=1.0.0.0"));

            var resolution = ResolveDecBuiltIn(new Dictionary<string, AssemblyReserved>(), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyDecSeparate);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void DecSeparateReferenced()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0", "dec, Version=1.0.0.0"));

            var resolution = ResolveDecBuiltIn(new Dictionary<string, AssemblyReserved>(), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyDecSeparate);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        // A game that also has a real `dec`, one the mod's copy can't stand in for, reports only the Dec error, not a conflict whose advice to reference `dec` without copying it contradicts it.
        [Test]
        public void DecSeparateGameHasDec()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/dec.dll", "dec, Version=1.0.0.0"));
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0", "dec, Version=1.0.0.0"));
            var game = new Dictionary<string, AssemblyReserved>(System.StringComparer.OrdinalIgnoreCase) { { "dec", AssemblyGame("/game/dec.dll", "dec, Version=0.5.0.0, PublicKeyToken=null") } };

            var resolution = ResolveDecBuiltIn(game, mod);
            AssertKinds(resolution, ModProblemKind.AssemblyDecSeparate);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void DecSeparateUnrelated()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0", "Decimal, Version=1.0.0.0"));

            var resolution = ResolveDecBuiltIn(new Dictionary<string, AssemblyReserved>(), mod);
            AssertKinds(resolution);
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(mod));
        }

        // A mod shipping Lib alongside its own assembly, which uses Lib.
        private static Mod ModWithLib(string id, string version, string referenced = null, string token = "null")
        {
            var mod = Mod(id, "1.0.0");
            mod.assemblies.Add(AssemblyMod($"/mods/{id}/Assemblies/Lib.dll", $"Lib, Version={version}, PublicKeyToken={token}"));
            mod.assemblies.Add(AssemblyMod($"/mods/{id}/Assemblies/{id}.dll", $"{id}, Version=1.0.0.0", $"Lib, Version={referenced ?? version}, PublicKeyToken={token}"));
            return mod;
        }

        private static Dictionary<string, AssemblyReserved> GameWithLib(string location, string version, string token = "null")
        {
            return new Dictionary<string, AssemblyReserved>(System.StringComparer.OrdinalIgnoreCase) { { "Lib", AssemblyGame(location, version == null ? null : $"Lib, Version={version}, PublicKeyToken={token}") } };
        }

        private static void AssertSkipped(ModResolution resolution, Mod mod, params string[] names)
        {
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(mod));
            CollectionAssert.AreEquivalent(mod.assemblies.Select(assembly => assembly.name.Name).Except(names), resolution.AssembliesLoading(mod).Select(assembly => assembly.name.Name));
        }

        [TestCase("/game/Lib.dll", "1.0.0.0", "2.0.0.0")]
        [TestCase(null, "1.0.0.0", "2.0.0.0")]
        [TestCase("/game/Lib.dll", "2.0.0.0", "2.0.0.0")]
        [TestCase(null, "2.0.0.0", "2.0.0.0")]
        public void AssemblyReservedSkipped(string location, string version, string game)
        {
            var mod = ModWithLib("A", version);
            var resolution = ResolveWithGame(GameWithLib(location, game), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyRedundant);
            Assert.AreEqual(ModProblemSeverity.Warning, resolution.Problems[0].Severity);
            AssertSkipped(resolution, mod, "Lib");
        }

        [TestCase("/game/Lib.dll")]
        [TestCase(null)]
        public void AssemblyReservedNewer(string location)
        {
            var mod = ModWithLib("A", "3.0.0.0");
            var resolution = ResolveWithGame(GameWithLib(location, "2.0.0.0"), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void AssemblyReservedReferenceNewer()
        {
            var mod = ModWithLib("A", "1.0.0.0", referenced: "3.0.0.0");
            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", "2.0.0.0"), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void AssemblyReservedUnreferenced()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/Lib.dll", "Lib, Version=1.0.0.0"));
            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", "2.0.0.0"), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void AssemblyReservedLibraryMod()
        {
            var library = Mod("Library", "1.0.0");
            library.assemblies.Add(AssemblyMod("/mods/Library/Assemblies/Lib.dll", "Lib, Version=1.0.0.0"));
            var user = Mod("User", "1.0.0", Dependency("Library"));
            user.assemblies.Add(AssemblyMod("/mods/User/Assemblies/User.dll", "User, Version=1.0.0.0", "Lib, Version=1.0.0.0"));

            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", "2.0.0.0"), library, user);
            AssertKinds(resolution, ModProblemKind.AssemblyRedundant);
            AssertSkipped(resolution, library, "Lib");
            AssertSkipped(resolution, user);
        }

        [TestCase("0123456789abcdef", "null", ModProblemKind.AssemblyConflict)]
        [TestCase("null", "0123456789abcdef", ModProblemKind.AssemblyConflict)]
        [TestCase("0123456789abcdef", "fedcba9876543210", ModProblemKind.AssemblyConflict)]
        [TestCase("0123456789abcdef", "0123456789abcdef", ModProblemKind.AssemblyRedundant)]
        public void AssemblyReservedToken(string token, string tokenGame, ModProblemKind kind)
        {
            var mod = ModWithLib("A", "1.0.0.0", token: token);
            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", "2.0.0.0", tokenGame), mod);
            AssertKinds(resolution, kind);
        }

        [Test]
        public void AssemblyReservedVersionUnknown()
        {
            var mod = ModWithLib("A", "1.0.0.0");
            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", null), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void AssemblyReservedSkippedNoClash()
        {
            var a = ModWithLib("A", "1.0.0.0");
            var b = ModWithLib("B", "1.5.0.0");
            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", "2.0.0.0"), a, b);
            AssertKinds(resolution, ModProblemKind.AssemblyRedundant, ModProblemKind.AssemblyRedundant);
            AssertSkipped(resolution, a, "Lib");
            AssertSkipped(resolution, b, "Lib");
        }

        [Test]
        public void AssemblyReservedSkippedSeveral()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/Lib.dll", "Lib, Version=1.0.0.0"));
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/Other.dll", "Other, Version=1.0.0.0"));
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0", "Lib, Version=1.0.0.0", "Other, Version=1.0.0.0"));
            var game = new Dictionary<string, AssemblyReserved>()
            {
                { "Lib", AssemblyGame("/game/Lib.dll", "Lib, Version=1.0.0.0") },
                { "Other", AssemblyGame(null, "Other, Version=1.0.0.0") },
            };

            var resolution = ResolveWithGame(game, mod);
            AssertKinds(resolution, ModProblemKind.AssemblyRedundant);
            AssertSkipped(resolution, mod, "Lib", "Other");
        }

        [Test]
        public void AssemblyReservedCaseInsensitive()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/lib.dll", "lib, Version=1.0.0.0"));
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0", "LIB, Version=1.0.0.0"));
            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", "2.0.0.0"), mod);
            AssertKinds(resolution, ModProblemKind.AssemblyRedundant);
            AssertSkipped(resolution, mod, "lib");
        }

        // Whichever copy binds serves every mod, so another mod needing a newer version than the game's rules the game's copy out.
        [Test]
        public void AssemblyReservedOtherModNeedsNewer()
        {
            var library = Mod("Library", "1.0.0");
            library.assemblies.Add(AssemblyMod("/mods/Library/Assemblies/Lib.dll", "Lib, Version=1.0.0.0"));
            var user = Mod("User", "1.0.0", Dependency("Library"));
            user.assemblies.Add(AssemblyMod("/mods/User/Assemblies/User.dll", "User, Version=1.0.0.0", "Lib, Version=2.5.0.0"));

            var resolution = ResolveWithGame(GameWithLib("/game/Lib.dll", "2.0.0.0"), library, user);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict, ModProblemKind.DependencyDisabled);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(library));
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(user));
        }

        // A mod disabled for one assembly doesn't also claim to use the game's copy of another.
        [Test]
        public void AssemblyReservedMixed()
        {
            var mod = ModWithLib("A", "1.0.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/Other.dll", "Other, Version=1.0.0.0"));
            var game = GameWithLib("/game/Lib.dll", "2.0.0.0");
            game["Other"] = AssemblyGame("/game/Other.dll", "Other, Version=1.0.0.0");

            var resolution = ResolveWithGame(game, mod);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void AssemblyReservedSamePath()
        {
            var mod = Mod("A", "1.0.0");
            mod.assemblies.Add(AssemblyMod("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0"));
            var resolution = ResolveWithGame(new Dictionary<string, AssemblyReserved>() { { "A", AssemblyGame("/mods/A/Assemblies/A.dll", "A, Version=1.0.0.0") } }, mod);
            AssertKinds(resolution);
            AssertSkipped(resolution, mod);
        }

        [Test]
        public void AssemblyClash()
        {
            var a = Mod("A", "1.0.0");
            a.assemblies.Add(AssemblyMod("/mods/A/Assemblies/Lib.dll", "Lib"));
            var b = Mod("B", "1.0.0");
            b.assemblies.Add(AssemblyMod("/mods/B/Assemblies/lib.dll", "lib"));
            var resolution = ResolvePure(a, b);
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            Assert.AreEqual(2, resolution.Problems[0].Mods.Count);
            CollectionAssert.AreEqual(new[] { BaseId }, Ids(resolution));
        }

        [Test]
        public void AssemblyClashWithDisabled()
        {
            var a = Mod("A", "1.0.0");
            a.assemblies.Add(AssemblyMod("/mods/A/Assemblies/Lib.dll", "Lib"));
            var b = Mod("B", "1.0.0", Dependency("Missing"));
            b.assemblies.Add(AssemblyMod("/mods/B/Assemblies/Lib.dll", "Lib"));
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
