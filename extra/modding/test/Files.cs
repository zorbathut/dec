using System.IO;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Files : Base
    {
        private static string ModFolder(string root, string folder, string manifest, params string[] files)
        {
            var directory = System.IO.Path.Combine(root, folder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(System.IO.Path.Combine(directory, "mod.xml"), manifest);
            foreach (var file in files)
            {
                var path = System.IO.Path.Combine(directory, file);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                File.WriteAllText(path, folder);
            }
            return directory;
        }

        private static ModResolution Resolve(string root, params string[] idsOff)
        {
            return new ModLoader(BaseId, "1.0.0").Scan(new[] { root }).Resolve(idsOff);
        }

        [Test]
        public void LaterLoadingWins()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"), "dec/art/thing.png");
            ModFolder(root, "b", ModXml("B", "1.0.0", Dependency("A", kind: "Accommodated", order: "Before")), "dec/art/thing.png");

            var resolution = Resolve(root);
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, resolution.Mods.Select(mod => mod.Id));
            Assert.AreEqual(System.IO.Path.Combine(a, "dec", "art", "thing.png"), resolution.FileResolve("dec/art/thing.png"));
        }

        [Test]
        public void NotLoadingIgnored()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0"), "dec/art/thing.png");
            ModFolder(root, "b", ModXml("B", "1.0.0", Dependency("Missing")), "dec/art/thing.png");

            var resolution = Resolve(root, "A");
            Assert.IsNull(resolution.FileResolve("dec/art/thing.png"));
        }

        [Test]
        public void Absent()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0"), "dec/art/thing.png");

            var resolution = Resolve(root);
            Assert.IsNull(resolution.FileResolve("dec/art/other.png"));
            Assert.IsNull(resolution.FileResolve("dec/art"));
            Assert.IsNull(resolution.FileResolve(""));
            Assert.IsNull(resolution.FileResolve(null));
        }

        // So resolving a path that's already been resolved leaves it alone.
        [Test]
        public void RootedAndSchemeNotResolved()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0"), "dec/art/thing.png");

            var resolution = Resolve(root);
            var resolved = resolution.FileResolve("dec/art/thing.png");
            Assert.IsNotNull(resolved);
            Assert.IsNull(resolution.FileResolve(resolved));
            Assert.IsNull(resolution.FileResolve("res://dec/art/thing.png"));
            Assert.IsNull(resolution.FileResolve("/dec/art/thing.png"));
        }

        [Test]
        public void BackslashIsSeparator()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"), "dec/art/thing.png");

            Assert.AreEqual(System.IO.Path.Combine(a, "dec", "art", "thing.png"), Resolve(root).FileResolve("dec\\art\\thing.png"));
        }

        [Test]
        public void CaseSensitive()
        {
            var root = DirectoryTemp();
            var a = ModFolder(root, "a", ModXml("A", "1.0.0"), "dec/art/thing.png");

            var resolution = Resolve(root);
            Assert.AreEqual(System.IO.Path.Combine(a, "dec", "art", "thing.png"), resolution.FileResolve("dec/art/thing.png"));
            Assert.IsNull(resolution.FileResolve("dec/art/Thing.png"));
            Assert.IsNull(resolution.FileResolve("dec/Art/thing.png"));
        }

        [Test]
        public void DotNamesNotIndexed()
        {
            var root = DirectoryTemp();
            ModFolder(root, "a", ModXml("A", "1.0.0"), ".git/objects/thing", "dec/.hidden.png", "dec/.cache/thing.png");

            var resolution = Resolve(root);
            Assert.IsNull(resolution.FileResolve(".git/objects/thing"));
            Assert.IsNull(resolution.FileResolve("dec/.hidden.png"));
            Assert.IsNull(resolution.FileResolve("dec/.cache/thing.png"));
        }
    }
}
