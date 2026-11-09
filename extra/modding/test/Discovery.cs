using System.IO;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Discovery : Base
    {
        private static string ModFolder(string root, string folder, string manifest, string manifestName = "Mod.xml")
        {
            var directory = System.IO.Path.Combine(root, folder);
            Directory.CreateDirectory(System.IO.Path.Combine(directory, "Decs"));
            File.WriteAllText(System.IO.Path.Combine(directory, manifestName), manifest);
            return directory;
        }

        private static ModLoader Loader()
        {
            return new ModLoader(BaseId, "1.0.0");
        }

        // Each resolution of a scan stands alone: turning a mod off and back on returns to where it started, the scan's own problems appear once in each, and resolving adds nothing to the next resolution's problems.
        [Test]
        public void ScanResolvedRepeatedly()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0", Dependency("Missing")));
            ModFolder(root, "b", ModXml("B", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(root, "stray"));

            var scan = Loader().Scan(new[] { root });
            var on = scan.Resolve();
            var off = scan.Resolve(new[] { "A", "B" });
            var onAgain = scan.Resolve();

            CollectionAssert.AreEqual(new[] { BaseId, "B" }, Ids(on));
            CollectionAssert.AreEqual(new[] { BaseId }, Ids(off));
            CollectionAssert.AreEqual(new[] { BaseId, "B" }, Ids(onAgain));
            AssertKinds(on, ModProblemKind.ManifestMissing, ModProblemKind.DependencyMissing);
            AssertKinds(off, ModProblemKind.ManifestMissing);
            AssertKinds(onAgain, ModProblemKind.ManifestMissing, ModProblemKind.DependencyMissing);

            // A mod screen can key its state on the mods themselves.
            CollectionAssert.AreEqual(on.ModsInstalled, off.ModsInstalled);
        }

        // A manifest's diagnostics come from reading it, so they come once, from the scan.
        [Test]
        public void ResolveReportsNothing()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", "<Mod><id>A</id><version>1.0.0</version><fromTheFuture /></Mod>");

            ModScan scan = null;
            ExpectWarnings(() => scan = Loader().Scan(new[] { root }), warning => warning.Contains("fromTheFuture"));
            scan.Resolve();
            scan.Resolve(new[] { "A" });
        }

        [Test]
        public void ResolveBaseOff()
        {
            var scan = Loader().Scan(new string[0]);
            Assert.Throws<System.ArgumentException>(() => scan.Resolve(new[] { BaseId }));
        }

        [Test]
        public void FindsMods()
        {
            var root = DirectoryTemp();
            var b = ModFolder(root, "b", ModXml("B", "1.0.0"));
            var a = ModFolder(root, "a", ModXml("A", "1.0.0", Dependency("B")));

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertInvariant(resolution);
            AssertKinds(resolution);
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(resolution));

            var modA = resolution.Mods.Single(mod => mod.Id == "A");
            Assert.AreEqual(a, modA.Directory);
            Assert.AreEqual(System.IO.Path.Combine(a, "Decs"), modA.directoryDecs);
            Assert.IsNull(modA.FileIcon);
        }

        [Test]
        public void ContentNone()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.Delete(System.IO.Path.Combine(a, "Decs"));

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution);
        }

        [Test]
        public void NameCase()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.Move(System.IO.Path.Combine(a, "Decs"), System.IO.Path.Combine(a, "decs"));
            File.WriteAllText(System.IO.Path.Combine(a, "decs", "Thing.xml"), "<Decs />");
            File.WriteAllBytes(System.IO.Path.Combine(a, "Icon.png"), new byte[0]);

            var resolution = Loader().Scan(new[] { root }).Resolve();
            var cases = resolution.Problems.Where(problem => problem.Kind == ModProblemKind.ContentCase).ToList();
            Assert.AreEqual(2, cases.Count, string.Join("\n", resolution.Problems));
            Assert.IsTrue(cases.All(problem => problem.Severity == ModProblemSeverity.Warning));
            StringAssert.Contains("`decs`", cases[0].Message);
            StringAssert.Contains("`Icon.png`", cases[1].Message);
        }

        [Test]
        public void ManifestCase()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0"), "mod.xml");

            var resolution = Loader().Scan(new[] { root }).Resolve();
            if (File.Exists(System.IO.Path.Combine(root, "a", "Mod.xml")))
            {
                // Case-insensitive filesystem: the mod loads, with a warning.
                Assert.IsTrue(resolution.Problems.Any(problem => problem.Kind == ModProblemKind.ContentCase && problem.Message.Contains("`mod.xml`")));
                Assert.AreEqual(2, resolution.Mods.Count);
            }
            else
            {
                AssertKinds(resolution, ModProblemKind.ManifestMissing);
                StringAssert.Contains("`mod.xml`", resolution.Problems[0].Message);
            }
        }

        [Test]
        public void XmlMisplaced()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(a, "Defs"));
            File.WriteAllText(System.IO.Path.Combine(a, "Defs", "Stray.xml"), "<Decs />");

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.ContentMisplaced);
            Assert.AreEqual(ModProblemSeverity.Warning, resolution.Problems[0].Severity);
            StringAssert.Contains("Stray.xml", resolution.Problems[0].Message);
        }

        [Test]
        public void XmlExtraBesideDecs()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            File.WriteAllText(System.IO.Path.Combine(a, "Decs", "Fine.xml"), "<Decs />");
            File.WriteAllText(System.IO.Path.Combine(a, "Notes.xml"), "<Notes />");

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution);
        }

        [Test]
        public void DllMisplaced()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(a, "Assemblies", "net8.0"));
            File.WriteAllBytes(System.IO.Path.Combine(a, "Assemblies", "net8.0", "Nested.dll"), new byte[0]);

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.ContentMisplaced);
            Assert.AreEqual(ModProblemSeverity.Warning, resolution.Problems[0].Severity);
            StringAssert.Contains("Nested.dll", resolution.Problems[0].Message);
        }

        [Test]
        public void DllExtraBesideAssemblies()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(a, "Assemblies", "lib"));
            File.WriteAllBytes(System.IO.Path.Combine(a, "Assemblies", "native.dll"), new byte[] { 1, 2, 3, 4 });
            File.WriteAllBytes(System.IO.Path.Combine(a, "Assemblies", "lib", "Nested.dll"), new byte[0]);

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.AssemblyNative);
        }

        [Test]
        public void SkipsDotFolders()
        {
            var root = DirectoryTemp();
            ModFolder(root, ".hidden", ModXml("A", "1.0.0"));
            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution);
            Assert.AreEqual(1, resolution.ModsInstalled.Count);
        }

        [Test]
        public void ManifestMissing()
        {
            var root = DirectoryTemp();
            Directory.CreateDirectory(System.IO.Path.Combine(root, "NestedTooDeep"));
            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.ManifestMissing);
            Assert.AreEqual(ModProblemSeverity.Info, resolution.Problems[0].Severity);
        }

        [Test]
        public void DirectoryMissing()
        {
            var resolution = Loader().Scan(new[] { System.IO.Path.Combine(DirectoryTemp(), "nope") }).Resolve();
            AssertKinds(resolution, ModProblemKind.DirectoryMissing);
            Assert.AreEqual(ModProblemSeverity.Info, resolution.Problems[0].Severity);
        }

        [Test]
        public void DirectoryTwice()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0"));
            var resolution = Loader().Scan(new[] { root, System.IO.Path.Combine(root, ".", "") }).Resolve();
            AssertKinds(resolution);
            Assert.AreEqual(2, resolution.ModsInstalled.Count);
        }

        [Test]
        public void DirectoryTrailingSeparator()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0"));
            var resolution = Loader().Scan(new[] { root, root + System.IO.Path.DirectorySeparatorChar }).Resolve();
            AssertKinds(resolution);
            Assert.AreEqual(2, resolution.ModsInstalled.Count);
        }

        [Test]
        public void SeveralRoots()
        {
            var first = DirectoryTemp();
            var second = DirectoryTemp();
            ModFolder(first, "x", ModXml("A", "1.0.0"));
            var newer = ModFolder(second, "y", ModXml("A", "2.0.0"));
            var resolution = Loader().Scan(new[] { first, second }).Resolve();
            AssertKinds(resolution, ModProblemKind.Duplicate);
            Assert.AreEqual(newer, resolution.Mods[1].Directory);
        }

        [Test]
        public void Icon()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            File.WriteAllBytes(System.IO.Path.Combine(a, "icon.png"), new byte[0]);
            var resolution = Loader().Scan(new[] { root }).Resolve();
            Assert.AreEqual(System.IO.Path.Combine(a, "icon.png"), resolution.Mods[1].FileIcon);
        }

        [Test]
        public void ConventionsRenamed()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"), "About.xml");
            Directory.Move(System.IO.Path.Combine(a, "Decs"), System.IO.Path.Combine(a, "Content"));
            File.WriteAllBytes(System.IO.Path.Combine(a, "Preview.png"), new byte[0]);

            var loader = Loader();
            loader.FileManifest = "About.xml";
            loader.FileIcon = "Preview.png";
            loader.DirectoryDecs = "Content";

            var resolution = loader.Scan(new[] { root }).Resolve();
            AssertKinds(resolution);
            Assert.AreEqual(System.IO.Path.Combine(a, "Preview.png"), resolution.Mods[1].FileIcon);
            Assert.AreEqual(System.IO.Path.Combine(a, "Content"), resolution.Mods[1].directoryDecs);
        }

        [Test]
        public void ManifestProblemsCarryFile()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", "<Mod><id>A</id><version>1.0</version></Mod>");
            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.Manifest);
            StringAssert.Contains(System.IO.Path.Combine(a, "Mod.xml"), resolution.Problems[0].Message);
        }

        [Test]
        public void NativeAssembly()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(a, "Assemblies"));
            File.WriteAllBytes(System.IO.Path.Combine(a, "Assemblies", "steam_api64.dll"), new byte[] { 1, 2, 3, 4 });

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.AssemblyNative);
            Assert.AreEqual(ModProblemSeverity.Warning, resolution.Problems[0].Severity);
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(resolution.ModsInstalled[1]));
            Assert.IsEmpty(resolution.ModsInstalled[1].assemblies);
        }

        [Test]
        public void AssemblyNamedLikeDec()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(a, "Assemblies"));
            File.Copy(typeof(Dec.Dec).Assembly.Location, System.IO.Path.Combine(a, "Assemblies", "dec.dll"));

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
        }

        [Test]
        public void AssemblyNamedLikeDecUsed()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(a, "Assemblies"));
            File.Copy(typeof(Dec.Dec).Assembly.Location, System.IO.Path.Combine(a, "Assemblies", "dec.dll"));
            DecUtilLib.Compilation.CompileToFile("public class Holder { public Dec.Dec dec; }", new System.Reflection.Assembly[] { }, "AssemblyNamedLikeDecUsed", System.IO.Path.Combine(a, "Assemblies", "A.dll"));

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, ModProblemKind.AssemblyRedundant);
            Assert.AreEqual(ModStatus.Enabled, resolution.GetStatus(resolution.ModsInstalled[1]));
        }

        [TestCase("0.0.0.0", ModProblemKind.AssemblyRedundant)]
        [TestCase("2.0.0.0", ModProblemKind.AssemblyConflict)]
        public void AssemblyNamedLikeInMemory(string version, ModProblemKind kind)
        {
            Assert.AreEqual(System.Runtime.Loader.AssemblyLoadContext.Default, System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(Dec.Dec).Assembly));
            var name = DecUtilLib.Compilation.Compile("public class Nothing { }", new System.Reflection.Assembly[] { }).GetName().Name;

            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"));
            Directory.CreateDirectory(System.IO.Path.Combine(a, "Assemblies"));
            var copy = System.IO.Path.Combine(a, "Assemblies", "Lib.dll");
            DecUtilLib.Compilation.CompileToFile($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")] public class Nothing {{ }}", new System.Reflection.Assembly[] { }, name, copy);

            // The mod's own assembly uses its copy, so it's compiled against that, loaded off to the side.
            var scratch = new System.Runtime.Loader.AssemblyLoadContext(null, isCollectible: true);
            DecUtilLib.Compilation.CompileToFile("public class Holder { public Nothing nothing; }", new[] { scratch.LoadFromAssemblyPath(copy) }, "AssemblyNamedLikeInMemory", System.IO.Path.Combine(a, "Assemblies", "A.dll"));
            scratch.Unload();

            var resolution = Loader().Scan(new[] { root }).Resolve();
            AssertKinds(resolution, kind);
        }

        [TestCase("1bad", "1.0.0")]
        [TestCase("Game", "1.0")]
        [TestCase(null, "1.0.0")]
        public void BaseInvalid(string id, string version)
        {
            Assert.Throws<System.ArgumentException>(() => new ModLoader(id, version));
        }
    }
}
