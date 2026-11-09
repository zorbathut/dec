using System.Collections.Generic;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Duplicate : Base
    {
        private static Mod ModAt(string directory, string id, string version)
        {
            var problems = new List<ModProblem>();
            var mod = ModRead(ModXml(id, version), problems, directory);
            Assert.IsEmpty(problems);
            return mod;
        }

        [Test]
        public void NewestWins()
        {
            var older = ModAt("/a/old", "A", "1.0.0");
            var newer = ModAt("/b/new", "A", "1.2.0");
            var resolution = ResolvePure(older, newer);

            AssertKinds(resolution, ModProblemKind.Duplicate);
            CollectionAssert.AreEqual(new[] { newer, older }, resolution.Problems[0].Mods);
            Assert.AreEqual(ModStatus.Hidden, resolution.GetStatus(older));
            Assert.AreSame(newer, resolution.Mods[1]);
        }

        [Test]
        public void ReleaseBeatsPrerelease()
        {
            var beta = ModAt("/a", "A", "1.2.0-beta");
            var release = ModAt("/b", "A", "1.2.0");
            var resolution = ResolvePure(beta, release);
            Assert.AreSame(release, resolution.Mods[1]);
        }

        [Test]
        public void EqualVersionsPathOrder()
        {
            var second = ModAt("/z/copy", "A", "1.0.0");
            var first = ModAt("/a/copy", "A", "1.0.0");
            var resolution = ResolvePure(second, first);
            AssertKinds(resolution, ModProblemKind.Duplicate);
            Assert.AreSame(first, resolution.Mods[1]);
        }

        [Test]
        public void BuildMetadataEqual()
        {
            var second = ModAt("/z", "A", "1.0.0+aaa");
            var first = ModAt("/a", "A", "1.0.0+zzz");
            var resolution = ResolvePure(second, first);
            Assert.AreSame(first, resolution.Mods[1]);
        }

        [Test]
        public void BrokenNewerLoses()
        {
            var problems = new List<ModProblem>();
            var brokenNewer = ModRead("<Mod><id>A</id><version>2.0.0</version><authors><li /></authors></Mod>", problems, "/a");
            var working = ModAt("/b", "A", "1.0.0");
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, AssemblyReserved>(), brokenNewer, working);

            Assert.AreSame(working, resolution.Mods[1]);
            Assert.AreEqual(ModStatus.Hidden, resolution.GetStatus(brokenNewer));
        }

        [Test]
        public void AllBroken()
        {
            var problems = new List<ModProblem>();
            var older = ModRead("<Mod><id>A</id><version>1.0.0</version><authors><li /></authors></Mod>", problems, "/a");
            var newer = ModRead("<Mod><id>A</id><version>2.0.0</version><authors><li /></authors></Mod>", problems, "/b");
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, AssemblyReserved>(), older, newer);

            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(newer));
            Assert.AreEqual(ModStatus.Hidden, resolution.GetStatus(older));
        }

        [Test]
        public void CaseConflict()
        {
            var resolution = ResolvePure(Mod("Foo", "1.0.0"), Mod("foo", "1.0.0"), Mod("Bar", "1.0.0", Dependency("Foo")));
            AssertKinds(resolution, ModProblemKind.IdConflict, ModProblemKind.DependencyDisabled);
            CollectionAssert.AreEqual(new[] { BaseId }, Ids(resolution));
        }

        [TestCase(BaseId)]
        [TestCase("game")]
        public void BaseIdReused(string id)
        {
            var resolution = ResolvePure(Mod(id, "1.0.0"), Mod("Other", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.IdConflict);
            CollectionAssert.AreEqual(new[] { BaseId, "Other" }, Ids(resolution));
        }
    }
}
