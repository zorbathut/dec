using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    // Models a host like Godot's editor, which loads the game into its own context from memory and finds the game's dependencies itself instead of through TPA.
    [TestFixture]
    public class Reserved : Base
    {
        private string run;
        private string directory;
        private AssemblyLoadContext game;
        private AssemblyLoadContext other;

        [SetUp]
        public void Prep()
        {
            run = Guid.NewGuid().ToString("N");
            directory = DirectoryTemp();
            game = new AssemblyLoadContext("game", isCollectible: true);
            other = new AssemblyLoadContext("other", isCollectible: true);
        }

        [TearDown]
        public void Cleanup()
        {
            game.Unload();
            other.Unload();
        }

        private string Name(string id)
        {
            return $"ModTest_{run}_{id}";
        }

        private string CompileToFile(string id, string source, params Assembly[] references)
        {
            var path = System.IO.Path.Combine(directory, Name(id) + ".dll");
            DecUtilLib.Compilation.CompileToFile(source, references, Name(id), path);
            return path;
        }

        // A class with a field of each given type, so the assembly references each of theirs.
        private string Holder(string id, params string[] ids)
        {
            var fields = string.Join(" ", Array.ConvertAll(ids, field => $"public {Name(field)}.Thing {field};"));
            return $"namespace {Name(id)} {{ public class Thing {{ {fields} }} }}";
        }

        // Builds a game assembly loaded from memory into the game context, referencing an assembly that isn't loaded there.
        private void GameSetUp()
        {
            var dependency = other.LoadFromAssemblyPath(CompileToFile("Dependency", Holder("Dependency")));
            var path = CompileToFile("Game", Holder("Game", "Dependency"), dependency);
            using (var stream = File.OpenRead(path))
            {
                game.LoadFromStream(stream);
            }
        }

        [Test]
        public void InMemoryAssemblyReserved()
        {
            GameSetUp();
            var reserved = ModLoader.AssembliesReserved(game);
            Assert.IsTrue(reserved.TryGetValue(Name("Game"), out var location));
            Assert.IsNull(location);
        }

        // Stands for both a dependency the game context hasn't loaded yet and one it hands in from another context, like Godot's GodotSharp; neither is among its assemblies.
        [Test]
        public void ReferenceReserved()
        {
            GameSetUp();
            var reserved = ModLoader.AssembliesReserved(game);
            Assert.IsTrue(reserved.TryGetValue(Name("Dependency"), out var location));
            Assert.IsNull(location);
        }

        [Test]
        public void ModReferencesNotReserved()
        {
            var extra = other.LoadFromAssemblyPath(CompileToFile("Extra", Holder("Extra")));
            var path = System.IO.Path.GetFullPath(CompileToFile("Mod", Holder("Mod", "Extra"), extra));
            game.LoadFromAssemblyPath(path);
            ModResolution.AssembliesLoaded.Add(path);

            var reserved = ModLoader.AssembliesReserved(game);
            Assert.AreEqual(path, reserved[Name("Mod")]);
            Assert.IsFalse(reserved.ContainsKey(Name("Extra")));
        }
    }
}
