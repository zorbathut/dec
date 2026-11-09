using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class AssemblyLoad : Base
    {
        public static List<string> EntryLog = new List<string>();

        private string run;
        private string root;
        private string baseDirectory;

        [SetUp]
        public void Prep()
        {
            EntryLog.Clear();
            run = Guid.NewGuid().ToString("N");
            root = DirectoryTemp();
            baseDirectory = DirectoryTemp();
        }

        private string AssemblyName(string id)
        {
            return $"ModTest_{run}_{id.Replace(".", "_")}";
        }

        // Each entry logs its mod's id and how many of this test's mod assemblies are loaded at that point.
        private string EntrySource(string id, string body = null)
        {
            body ??= $@"
                int loaded = 0;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {{
                    if (assembly.GetName().Name.StartsWith(""ModTest_{run}_""))
                    {{
                        ++loaded;
                    }}
                }}
                ModdingTest.AssemblyLoad.EntryLog.Add(info.Mod.Id + "":"" + loaded + "":"" + info.ModsEnabled.Count);";

            return $@"
                namespace {AssemblyName(id)}
                {{
                    public class Entry : Dec.Modding.IModEntrypoint
                    {{
                        public void Initialize(Dec.Modding.ModEntrypointInfo info)
                        {{
                            {body}
                        }}
                    }}
                }}";
        }

        private string CodeMod(string id, string source, params string[] dependencies)
        {
            var folder = System.IO.Path.Combine(root, id);
            Directory.CreateDirectory(System.IO.Path.Combine(folder, "Assemblies"));
            File.WriteAllText(System.IO.Path.Combine(folder, "Mod.xml"), ModXml(id, "1.0.0", dependencies));
            DecUtilLib.Compilation.CompileToFile(source, new[] { typeof(IModEntrypoint).Assembly, GetType().Assembly }, AssemblyName(id), System.IO.Path.Combine(folder, "Assemblies", AssemblyName(id) + ".dll"));
            return folder;
        }

        private ModResolution Resolve()
        {
            var resolution = new ModLoader(BaseId, "1.0.0", baseDirectory).Resolve(new[] { root });
            AssertInvariant(resolution);
            return resolution;
        }

        [Test]
        public void EntriesRunInOrder()
        {
            CodeMod("A", EntrySource("A"), Dependency("B"));
            CodeMod("B", EntrySource("B"));
            CodeMod("C", EntrySource("C"));

            var resolution = Resolve();
            AssertKinds(resolution);

            var parser = resolution.Load();
            parser.Finish();

            // Every assembly is loaded before the first entry runs.
            CollectionAssert.AreEqual(new[] { "B:3:4", "A:3:4", "C:3:4" }, EntryLog);
        }

        [Test]
        public void DisabledModNotLoaded()
        {
            CodeMod("A", EntrySource("A"), Dependency("Missing"));
            CodeMod("B", EntrySource("B"));

            var resolution = Resolve();
            ExpectErrors(() => resolution.Load().Finish(), error => error.Contains("Missing"));

            CollectionAssert.AreEqual(new[] { "B:1:2" }, EntryLog);
            Assert.IsFalse(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == AssemblyName("A")));
        }

        [Test]
        public void EntryTwice()
        {
            CodeMod("A", EntrySource("A") + $@"
                namespace {AssemblyName("A")}.Second
                {{
                    public class Entry : Dec.Modding.IModEntrypoint
                    {{
                        public void Initialize(Dec.Modding.ModEntrypointInfo info)
                        {{
                            ModdingTest.AssemblyLoad.EntryLog.Add(""second"");
                        }}
                    }}
                }}");

            var resolution = Resolve();
            ExpectErrors(() => resolution.Load().Finish(), error => error.Contains("more than one IModEntrypoint"));
            Assert.IsEmpty(EntryLog);
        }

        [Test]
        public void EntryThrows()
        {
            CodeMod("A", EntrySource("A", @"throw new System.InvalidOperationException(""entry failure"");"));
            CodeMod("B", EntrySource("B"));

            var resolution = Resolve();
            ExpectErrors(() => resolution.Load().Finish(), error => error.Contains("entry failure"));
            CollectionAssert.AreEqual(new[] { "B:2:3" }, EntryLog);
        }

        [Test]
        public void EntryWithoutConstructor()
        {
            CodeMod("A", $@"
                namespace {AssemblyName("A")}
                {{
                    public class Entry : Dec.Modding.IModEntrypoint
                    {{
                        public Entry(int unusable) {{ }}
                        public void Initialize(Dec.Modding.ModEntrypointInfo info) {{ }}
                    }}
                }}");

            var resolution = Resolve();
            ExpectErrors(() => resolution.Load().Finish(), error => error.Contains("IModEntrypoint"));
        }

        [Test]
        public void EntryAbsent()
        {
            CodeMod("A", $"namespace {AssemblyName("A")} {{ public class Nothing {{ }} }}");

            var resolution = Resolve();
            resolution.Load().Finish();
            Assert.IsTrue(AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == AssemblyName("A")));
        }

        [Test]
        public void BaseDirectoryMissing()
        {
            var resolution = new ModLoader(BaseId, "1.0.0", System.IO.Path.Combine(baseDirectory, "nope")).Resolve(new[] { root });
            ExpectErrors(() => resolution.Load().Finish(), error => error.Contains("nope"));
        }

        [Test]
        public void DirectoryAssembliesRenamed()
        {
            var folder = CodeMod("A", EntrySource("A"));
            Directory.Move(System.IO.Path.Combine(folder, "Assemblies"), System.IO.Path.Combine(folder, "Code"));

            var loader = new ModLoader(BaseId, "1.0.0", baseDirectory);
            loader.DirectoryAssemblies = "Code";
            loader.Resolve(new[] { root }).Load().Finish();

            CollectionAssert.AreEqual(new[] { "A:1:2" }, EntryLog);
        }

        [Test]
        public void LoadTwice()
        {
            var resolution = Resolve();
            resolution.Load().Finish();
            Assert.Throws<InvalidOperationException>(() => resolution.Load());
        }

        [Test]
        public void ReloadSameSet()
        {
            CodeMod("A", EntrySource("A"));

            Resolve().Load().Finish();
            Dec.Database.Clear();
            Resolve().Load().Finish();

            // The second load reuses the assembly without running its entry point again.
            CollectionAssert.AreEqual(new[] { "A:1:2" }, EntryLog);
        }

        [Test]
        public void ReloadWithoutCodeMod()
        {
            CodeMod("A", EntrySource("A"));

            Resolve().Load().Finish();
            Dec.Database.Clear();

            // Resolving a different directory, rather than deleting the mod, since a loaded assembly can't be deleted on Windows.
            root = DirectoryTemp();
            ExpectErrors(() => Resolve().Load().Finish(), error => error.Contains("A.dll"));
        }

        [Test]
        public void ReloadAddsLibraryOfLoadedMod()
        {
            var library = $"namespace {AssemblyName("Library")} {{ public class Thing {{ }} }}";
            var scratch = new System.Runtime.Loader.AssemblyLoadContext("scratch", isCollectible: true);
            var libraryPath = System.IO.Path.Combine(DirectoryTemp(), AssemblyName("Library") + ".dll");
            DecUtilLib.Compilation.CompileToFile(library, new System.Reflection.Assembly[] { }, AssemblyName("Library"), libraryPath);
            var libraryAssembly = scratch.LoadFromAssemblyPath(libraryPath);

            // A references the library without depending on a mod that ships it.
            var folder = System.IO.Path.Combine(root, "A");
            Directory.CreateDirectory(System.IO.Path.Combine(folder, "Assemblies"));
            File.WriteAllText(System.IO.Path.Combine(folder, "Mod.xml"), ModXml("A", "1.0.0"));
            DecUtilLib.Compilation.CompileToFile($"namespace {AssemblyName("A")} {{ public class Holder {{ public {AssemblyName("Library")}.Thing thing; }} }}", new[] { libraryAssembly }, AssemblyName("A"), System.IO.Path.Combine(folder, "Assemblies", AssemblyName("A") + ".dll"));
            scratch.Unload();

            Resolve().Load().Finish();
            Dec.Database.Clear();

            var libraryMod = System.IO.Path.Combine(root, "Library");
            Directory.CreateDirectory(System.IO.Path.Combine(libraryMod, "Assemblies"));
            File.WriteAllText(System.IO.Path.Combine(libraryMod, "Mod.xml"), ModXml("Library", "1.0.0"));
            DecUtilLib.Compilation.CompileToFile(library, new System.Reflection.Assembly[] { }, AssemblyName("Library"), System.IO.Path.Combine(libraryMod, "Assemblies", AssemblyName("Library") + ".dll"));

            AssertKinds(Resolve());
        }

        [Test]
        public void AssemblyClashNotLoaded()
        {
            var source = $"namespace {AssemblyName("Shared")} {{ public class Thing {{ }} }}";
            foreach (var id in new[] { "A", "B" })
            {
                var folder = System.IO.Path.Combine(root, id);
                Directory.CreateDirectory(System.IO.Path.Combine(folder, "Assemblies"));
                File.WriteAllText(System.IO.Path.Combine(folder, "Mod.xml"), ModXml(id, "1.0.0"));
                DecUtilLib.Compilation.CompileToFile(source, new System.Reflection.Assembly[] { }, AssemblyName("Shared"), System.IO.Path.Combine(folder, "Assemblies", "Shared.dll"));
            }

            var resolution = Resolve();
            AssertKinds(resolution, ModProblemKind.AssemblyConflict);
            CollectionAssert.AreEqual(new[] { BaseId }, Ids(resolution));
        }

        [Test]
        public void ModulesInOrder()
        {
            foreach (var (id, dependency) in new[] { ("A", Dependency("B")), ("B", null) })
            {
                var folder = System.IO.Path.Combine(root, id);
                Directory.CreateDirectory(System.IO.Path.Combine(folder, "Decs"));
                File.WriteAllText(System.IO.Path.Combine(folder, "Mod.xml"), dependency == null ? ModXml(id, "1.0.0") : ModXml(id, "1.0.0", dependency));
                File.WriteAllText(System.IO.Path.Combine(folder, "Decs", "Stub.xml"), $@"<Decs><StubDec decName=""Stub"" mode=""{(id == "B" ? "create" : "replace")}"" /></Decs>");
            }
            File.WriteAllText(System.IO.Path.Combine(baseDirectory, "Stub.xml"), "<Decs />");

            UpdateTestParametersExplicitTypes(new[] { typeof(StubDec) });

            var resolution = Resolve();
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(resolution));

            // A replaces what B created; in the other order, A's replace would have nothing to replace.
            resolution.Load().Finish();
            Assert.IsNotNull(Dec.Database<StubDec>.Get("Stub"));
        }
    }
}
