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
            ModContent.CheckCollisions(mod, listing, problems);
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

        // That both exist is a collision, not a misspelling.
        [Test]
        public void BothSpellings()
        {
            AssertKinds(Check(Listing(new[] { "dec" }, "mod.xml", "Mod.xml", "dec/a.xml")).problems, ModProblemKind.ContentCaseCollision);
        }

        [Test]
        public void CollisionFiles()
        {
            AssertProblem(Check(Listing(new[] { "dec", "dec/art" }, "mod.xml", "dec/a.xml", "dec/art/thing.png", "dec/art/Thing.png", "dec/art/other.png")), ModProblemKind.ContentCaseCollision, new[] { "dec/art/thing.png", "dec/art/Thing.png" }, new[] { "dec/art/other.png" });
        }

        // Reported once for the folders, not again for the files inside them.
        [Test]
        public void CollisionFolders()
        {
            AssertProblem(Check(Listing(new[] { "dec", "art", "Art" }, "mod.xml", "dec/a.xml", "art/a.png", "Art/a.png", "Art/b.png")), ModProblemKind.ContentCaseCollision, new[] { "art", "Art" }, new[] { "art/a.png", "Art/a.png" });
        }

        [Test]
        public void CollisionFileAndFolder()
        {
            AssertProblem(Check(Listing(new[] { "dec", "Data" }, "mod.xml", "dec/a.xml", "data", "Data/a.png")), ModProblemKind.ContentCaseCollision, new[] { "data", "Data" });
        }

        [Test]
        public void CollisionSeveral()
        {
            AssertProblem(Check(Listing(new[] { "dec", "a", "A" }, "mod.xml", "dec/a.xml", "x.png", "X.png")), ModProblemKind.ContentCaseCollision, new[] { "a", "A", "x.png", "X.png" });
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
            Assert.Throws<System.ArgumentException>(() => loader.Scan(new[] { DirectoryTemp() }, null));
        }

        private static (List<ModProblem> problems, Mod mod) CheckOverrides(string[] game, params string[] files)
        {
            var problems = new List<ModProblem>();
            var mod = Mod("A", "1.0.0");
            ModContent.CheckOverrides(mod, Listing(new string[0], files), ModContent.FilesOverridable(game), problems);
            return (problems, mod);
        }

        [Test]
        public void OverrideFile()
        {
            AssertProblem(CheckOverrides(new[] { "dec/art/thing.png" }, "dec/art/Thing.png"), ModProblemKind.ContentCaseOverride, new[] { "dec/art/Thing.png", "dec/art/thing.png" });
        }

        // One entry for the miscased folder rather than one for each file in it.
        [Test]
        public void OverrideFolder()
        {
            AssertProblem(CheckOverrides(new[] { "textures/a.png", "textures/b.png" }, "Textures/a.png", "Textures/b.png"), ModProblemKind.ContentCaseOverride, new[] { "Textures", "textures" }, new[] { "Textures/a.png", "Textures/b.png" });
        }

        // The mod's own new files are found by the mod's own spelling, so a folder spelled differently from the game's is fine unless something in it was meant to replace a game file.
        [Test]
        public void OverrideNewFilesInOtherCase()
        {
            AssertKinds(CheckOverrides(new[] { "textures/a.png", "data" }, "Textures/new.png", "Data/b.png").problems);
        }

        [Test]
        public void OverrideExact()
        {
            AssertKinds(CheckOverrides(new[] { "dec/art/thing.png" }, "dec/art/thing.png", "dec/art/new.png", "other/Thing.png").problems);
        }

        [Test]
        public void OverrideGameDuplicates()
        {
            var game = new[] { "a/x.png", "a/x.png", "a/X.png", "a\\y.png" };
            AssertKinds(CheckOverrides(game, "a/x.png", "a/X.png", "a/y.png").problems);
            AssertProblem(CheckOverrides(game, "A/x.png"), ModProblemKind.ContentCaseOverride, new[] { "A" });
        }

        [TestCase("/abs/x.png")]
        [TestCase("res://x.png")]
        [TestCase("C:x.png")]
        [TestCase("./x.png")]
        [TestCase("a/../x.png")]
        [TestCase("a//x.png")]
        [TestCase("a/")]
        [TestCase("")]
        [TestCase(null)]
        public void OverrideGamePathInvalid(string path)
        {
            var exception = Assert.Throws<System.ArgumentException>(() => ModContent.FilesOverridable(new[] { "fine/x.png", path }));
            Assert.AreEqual("filesOverridable", exception.ParamName);
        }
    }
}
