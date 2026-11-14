using System.Collections.Generic;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    // The content checks run over a listing rather than the disk, so they behave the same whatever the filesystem does with case.
    [TestFixture]
    public class Layout : Base
    {
        private static ModListing Listing(string[] directories, params string[] files)
        {
            return new ModListing("/mods/a", files, directories);
        }

        private static (List<ModProblem> problems, Mod mod) Check(ModListing listing, ModLoader loader = null)
        {
            var problems = new List<ModProblem>();
            var mod = Mod("A", "1.0.0");
            ModContent.CheckContent(mod, listing, (loader ?? new ModLoader(BaseId, "1.0.0")).Layout(), problems);
            return (problems, mod);
        }

        // An Error disabling the mod, naming each of the given paths and none of the absent ones.
        private static void AssertProblem((List<ModProblem> problems, Mod mod) result, ModProblemKind kind, string[] names, string[] absent = null)
        {
            AssertKinds(result.problems, kind);
            Assert.AreEqual(ModProblemSeverity.Error, result.problems[0].Severity);
            Assert.IsTrue(result.mod.manifestError);
            foreach (var name in names)
            {
                StringAssert.Contains($"`{name}`", result.problems[0].Message);
            }
            foreach (var name in absent ?? new string[0])
            {
                StringAssert.DoesNotContain($"`{name}`", result.problems[0].Message);
            }
        }

        [Test]
        public void Exact()
        {
            var result = Check(Listing(new[] { "dec", "assembly" }, "mod.xml", "icon.png", "dec/a.xml"));
            AssertKinds(result.problems);
            Assert.IsFalse(result.mod.manifestError);
        }

        [Test]
        public void NamesMiscased()
        {
            AssertProblem(Check(Listing(new[] { "Dec", "Assembly" }, "Mod.xml", "Dec/a.xml", "Assembly/b.dll")), ModProblemKind.ContentCase, new[] { "Mod.xml", "Dec", "Assembly" });
        }

        // A miscased icon costs only the icon, so it's a warning that leaves the mod loading.
        [Test]
        public void IconMiscased()
        {
            var result = Check(Listing(new[] { "dec" }, "mod.xml", "Icon.png", "dec/a.xml"));
            AssertKinds(result.problems, ModProblemKind.ContentCase);
            Assert.AreEqual(ModProblemSeverity.Warning, result.problems[0].Severity);
            StringAssert.Contains("`Icon.png`", result.problems[0].Message);
            Assert.IsFalse(result.mod.manifestError);
        }

        [Test]
        public void NestedNameMiscased()
        {
            var loader = new ModLoader(BaseId, "1.0.0") { DirectoryDecs = "data/dec" };
            AssertProblem(Check(Listing(new[] { "Data", "Data/dec" }, "mod.xml", "Data/dec/a.xml"), loader), ModProblemKind.ContentCase, new[] { "Data/dec" });
        }

        [Test]
        public void NestedNameBackslash()
        {
            var loader = new ModLoader(BaseId, "1.0.0") { DirectoryDecs = "data\\dec\\" };
            AssertKinds(Check(Listing(new[] { "data", "data/dec" }, "mod.xml", "data/dec/a.xml"), loader).problems);
        }

        [Test]
        public void ExtensionMiscased()
        {
            AssertProblem(Check(Listing(new[] { "dec", "dec/sub", "assembly" }, "mod.xml", "dec/sub/a.XML", "assembly/b.Dll")), ModProblemKind.ContentCase, new[] { "dec/sub/a.XML", "assembly/b.Dll" });
        }

        // Case problems aren't also reported as content in the wrong place.
        [Test]
        public void MiscasedNotMisplaced()
        {
            AssertProblem(Check(Listing(new[] { "Dec", "Assembly" }, "mod.xml", "Dec/a.XML", "Assembly/b.dll")), ModProblemKind.ContentCase, new[] { "Dec", "Assembly" });
        }

        // That both exist isn't a misspelling.
        [Test]
        public void BothSpellings()
        {
            AssertKinds(Check(Listing(new[] { "dec" }, "mod.xml", "Mod.xml", "dec/a.xml")).problems);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("./assembly")]
        [TestCase("a//b")]
        [TestCase("a/../b")]
        [TestCase("/assembly")]
        [TestCase(".assembly")]
        [TestCase("res://assembly")]
        public void LayoutNameInvalid(string name)
        {
            var loader = new ModLoader(BaseId, "1.0.0") { DirectoryAssemblies = name };
            Assert.Throws<System.ArgumentException>(() => loader.Layout());
            Assert.Throws<System.ArgumentException>(() => loader.Scan(new[] { DirectoryTemp() }));
        }
    }
}
