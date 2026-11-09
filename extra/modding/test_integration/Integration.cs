using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ModdingTestIntegration
{
    public class IntegrationThingDec : Dec.Dec
    {
        public int value;
    }

    // Runs with Dec's real type discovery, which unit tests replace with explicit type lists.
    [TestFixture]
    public class Integration
    {
        private static string Folder(string root, string id, string manifest)
        {
            var folder = Path.Combine(root, id);
            Directory.CreateDirectory(Path.Combine(folder, "Decs"));
            File.WriteAllText(Path.Combine(folder, "Mod.xml"), manifest);
            return folder;
        }

        private static string Patcher(string id, string dependencies)
        {
            return $"<Mod><id>{id}</id><version>1.0.0</version><dependencies>{dependencies}</dependencies></Mod>";
        }

        [Test]
        public void ResolveLoadFinish()
        {
            Dec.Config.UsingNamespaces = new string[] { "ModdingTestIntegration" };

            string run = "ModIntegration_" + Guid.NewGuid().ToString("N");
            var temp = Path.Combine(TestContext.CurrentContext.WorkDirectory, "modding-integration", run);
            var root = Path.Combine(temp, "Mods");
            var baseDirectory = Path.Combine(temp, "Base");
            Directory.CreateDirectory(baseDirectory);
            File.WriteAllText(Path.Combine(baseDirectory, "Thing.xml"), @"<Decs><IntegrationThingDec decName=""Thing""><value>1</value></IntegrationThingDec></Decs>");

            // A code mod with its own dec type, converter, static references, setup function, and entry point.
            var code = Folder(root, "Code", "<Mod><id>Code</id><version>1.0.0</version></Mod>");
            Directory.CreateDirectory(Path.Combine(code, "Assemblies"));
            DecUtilLib.Compilation.CompileToFile($@"
                namespace {run}
                {{
                    public class Gadget
                    {{
                        public int value;
                    }}

                    public class GadgetConverter : Dec.ConverterString<Gadget>
                    {{
                        public override string Write(Gadget input)
                        {{
                            return input.value.ToString();
                        }}

                        public override Gadget Read(string input, Dec.Context context)
                        {{
                            return new Gadget() {{ value = int.Parse(input) }};
                        }}
                    }}

                    public class GadgetDec : Dec.Dec
                    {{
                        public Gadget gadget;
                    }}

                    [Dec.StaticReferences]
                    public static class GadgetDecs
                    {{
                        static GadgetDecs()
                        {{
                            Dec.StaticReferencesAttribute.Initialized();
                        }}

                        public static GadgetDec Widget;
                    }}

                    public static class Setups
                    {{
                        public static bool ran;

                        [Dec.Setup]
                        public static void Run(System.Action<string> reporter)
                        {{
                            ran = true;
                        }}
                    }}

                    public class Entry : Dec.Modding.IModEntrypoint
                    {{
                        public static int read;

                        public void Initialize(Dec.Modding.ModEntrypointInfo info)
                        {{
                            read = Dec.Recorder.ReadSimple<Gadget>(""<Root>5</Root>"", ""Root"").value;
                        }}
                    }}
                }}", new[] { typeof(Dec.Modding.IModEntrypoint).Assembly }, run, Path.Combine(code, "Assemblies", run + ".dll"));
            File.WriteAllText(Path.Combine(code, "Decs", "Gadget.xml"), $@"<Decs><{run}.GadgetDec decName=""Widget""><gadget>7</gadget></{run}.GadgetDec></Decs>");

            // Two data mods patching the base game's dec; whichever loads last wins.
            Folder(root, "Patcher.A", Patcher("Patcher.A", ""));
            File.WriteAllText(Path.Combine(root, "Patcher.A", "Decs", "Patch.xml"), @"<Decs><IntegrationThingDec decName=""Thing"" mode=""patch""><value>2</value></IntegrationThingDec></Decs>");
            Folder(root, "Patcher.B", Patcher("Patcher.B", "<li><id>Patcher.A</id><kind>Accommodated</kind></li>"));
            File.WriteAllText(Path.Combine(root, "Patcher.B", "Decs", "Patch.xml"), @"<Decs><IntegrationThingDec decName=""Thing"" mode=""patch""><value>3</value></IntegrationThingDec></Decs>");

            var loader = new Dec.Modding.ModLoader("Game", "1.0.0");

            var resolution = loader.Scan(new[] { root }).Resolve();
            Assert.IsEmpty(resolution.Problems, string.Join("\n", resolution.Problems));
            CollectionAssert.AreEqual(new[] { "Game", "Code", "Patcher.A", "Patcher.B" }, resolution.Mods.Select(mod => mod.Id));
            resolution.Load(module => module.AddDirectory(baseDirectory)).Finish();

            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(candidate => candidate.GetName().Name == run);
            var widget = assembly.GetType($"{run}.GadgetDecs").GetField("Widget").GetValue(null);
            Assert.IsNotNull(widget);
            var gadget = widget.GetType().GetField("gadget").GetValue(widget);
            Assert.AreEqual(7, gadget.GetType().GetField("value").GetValue(gadget));
            Assert.IsTrue((bool)assembly.GetType($"{run}.Setups").GetField("ran").GetValue(null));
            Assert.AreEqual(5, assembly.GetType($"{run}.Entry").GetField("read").GetValue(null));
            Assert.AreEqual(3, Dec.Database<IntegrationThingDec>.Get("Thing").value);

            // Flip the patchers' order and load again in the same process.
            Dec.Database.Clear();
            File.WriteAllText(Path.Combine(root, "Patcher.A", "Mod.xml"), Patcher("Patcher.A", "<li><id>Patcher.B</id><kind>Accommodated</kind></li>"));
            File.WriteAllText(Path.Combine(root, "Patcher.B", "Mod.xml"), Patcher("Patcher.B", ""));

            resolution = loader.Scan(new[] { root }).Resolve();
            Assert.IsEmpty(resolution.Problems, string.Join("\n", resolution.Problems));
            CollectionAssert.AreEqual(new[] { "Game", "Code", "Patcher.B", "Patcher.A" }, resolution.Mods.Select(mod => mod.Id));
            resolution.Load(module => module.AddDirectory(baseDirectory)).Finish();

            Assert.AreEqual(2, Dec.Database<IntegrationThingDec>.Get("Thing").value);
            Assert.IsNotNull(assembly.GetType($"{run}.GadgetDecs").GetField("Widget").GetValue(null));
        }
    }
}
