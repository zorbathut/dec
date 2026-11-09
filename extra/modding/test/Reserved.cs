using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private List<AssemblyLoadContext> contexts;

        [SetUp]
        public void Prep()
        {
            run = Guid.NewGuid().ToString("N");
            directory = DirectoryTemp();
            contexts = new List<AssemblyLoadContext>();
            game = Context();
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var context in contexts)
            {
                context.Unload();
            }
        }

        private AssemblyLoadContext Context()
        {
            var context = new AssemblyLoadContext(null, isCollectible: true);
            contexts.Add(context);
            return context;
        }

        private string Name(string id)
        {
            return $"ModTest_{run}_{id}";
        }

        private ISet<string> Names(params string[] ids)
        {
            return new HashSet<string>(ids.Select(Name), StringComparer.OrdinalIgnoreCase);
        }

        // An assembly with a class holding a field of each given assembly's class, so it references each of them.
        private string CompileToFile(string id, string version, params Assembly[] references)
        {
            var fields = string.Join(" ", references.Select(reference => $"public {reference.GetName().Name}.Thing {reference.GetName().Name};"));
            var source = $"[assembly: System.Reflection.AssemblyVersion(\"{version}\")] namespace {Name(id)} {{ public class Thing {{ {fields} }} }}";
            var path = System.IO.Path.Combine(directory, $"{Name(id)}_{version}.dll");
            DecUtilLib.Compilation.CompileToFile(source, references, Name(id), path);
            return path;
        }

        // Loads the way Godot's editor does, from memory.
        private void LoadFromMemory(AssemblyLoadContext context, string path)
        {
            using (var stream = File.OpenRead(path))
            {
                context.LoadFromStream(stream);
            }
        }

        [Test]
        public void InMemoryAssemblyReserved()
        {
            LoadFromMemory(game, CompileToFile("Game", "1.2.3.4"));
            var reserved = ModLoader.AssembliesReserved(game, Names("Game"))[Name("Game")];
            Assert.IsNull(reserved.location);
            Assert.AreEqual(new Version(1, 2, 3, 4), reserved.name.Version);
        }

        // Stands for both a dependency the game context hasn't loaded yet and one it hands in from another context, like Godot's GodotSharp; neither is among its assemblies.
        [Test]
        public void ReferenceReserved()
        {
            var dependency = Context().LoadFromAssemblyPath(CompileToFile("Dependency", "1.0.0.0"));
            LoadFromMemory(game, CompileToFile("Game", "1.0.0.0", dependency));

            var reserved = ModLoader.AssembliesReserved(game, Names("Dependency"))[Name("Dependency")];
            Assert.IsNull(reserved.location);
            Assert.AreEqual(new Version(1, 0, 0, 0), reserved.name.Version);
        }

        [Test]
        public void ReferenceHighestVersion()
        {
            var older = Context().LoadFromAssemblyPath(CompileToFile("Dependency", "1.0.0.0"));
            var newer = Context().LoadFromAssemblyPath(CompileToFile("Dependency", "1.2.0.0"));
            LoadFromMemory(game, CompileToFile("GameA", "1.0.0.0", older));
            LoadFromMemory(game, CompileToFile("GameB", "1.0.0.0", newer));
            LoadFromMemory(game, CompileToFile("GameC", "1.0.0.0", older));

            Assert.AreEqual(new Version(1, 2, 0, 0), ModLoader.AssembliesReserved(game, Names("Dependency"))[Name("Dependency")].name.Version);
        }

        [Test]
        public void OnlyNamesAsked()
        {
            var dependency = Context().LoadFromAssemblyPath(CompileToFile("Dependency", "1.0.0.0"));
            LoadFromMemory(game, CompileToFile("Game", "1.0.0.0", dependency));
            CollectionAssert.IsEmpty(ModLoader.AssembliesReserved(game, Names("Unrelated")));
        }

        [Test]
        public void ModReferencesNotReserved()
        {
            var extra = Context().LoadFromAssemblyPath(CompileToFile("Extra", "1.0.0.0"));
            var path = System.IO.Path.GetFullPath(CompileToFile("Mod", "1.0.0.0", extra));
            game.LoadFromAssemblyPath(path);
            ModResolution.AssembliesLoaded.Add(path);

            var reserved = ModLoader.AssembliesReserved(game, Names("Mod", "Extra"));
            Assert.AreEqual(path, reserved[Name("Mod")].location);
            Assert.IsFalse(reserved.ContainsKey(Name("Extra")));
        }

        // TPA versions are read from the file, without loading it.
        [Test]
        public void TrustedVersionRead()
        {
            const string name = "System.Net.Mail";
            Assert.IsFalse(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == name), "this test needs an assembly nothing has loaded");

            var reserved = ModLoader.AssembliesReserved(game, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name })[name];
            Assert.IsTrue(File.Exists(reserved.location));
            Assert.AreEqual(AssemblyName.GetAssemblyName(reserved.location).Version, reserved.name.Version);
            Assert.IsFalse(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == name));
        }
    }
}
