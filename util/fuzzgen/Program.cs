
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fuzzgen
{
    internal static class Program
    {
        private static void Init()
        {
            // Configure Def's error reporting
            Def.Config.InfoHandler = str => Dbg.Inf(str);
            Def.Config.WarningHandler = str => Dbg.Wrn(str);
            Def.Config.ErrorHandler = str => Dbg.Err(str);
            Def.Config.ExceptionHandler = e => Dbg.Ex(e);

            // Configure Def's namespaces
            Def.Config.UsingNamespaces = new string[] { "Fuzzgen" };

            // In most cases, you'll just want to read all the XML files in your data directory, which is easy
            var parser = new Def.Parser();
            parser.AddDirectory("data/def");
            parser.Finish();
        }

        private static void Main()
        {
            Init();

            var env = new Env();

            // Generate initial composites
            for (int i = 0; i < 100; ++i)
            {
                var c = new Composite();
                c.name = Rand.NextString();

                c.type = CompositeTypeDistribution.Distribution.Choose();
                if (c.type == Composite.Type.Def)
                {
                    c.name += "Def";
                }

                env.types.Add(c);
            }

            // Generate parameters
            foreach (var c in env.types)
            {
                int parameterCount = Rand.WeightedDistribution();

                for (int i = 0; i < parameterCount; ++i)
                {
                    c.members.Add(new Member(env, c, Rand.NextString(), MemberTypeDistribution.Distribution.Choose()));
                }
            }

            // Generate instances
            foreach (var c in env.types)
            {
                if (c.type != Composite.Type.Def)
                {
                    continue;
                }

                int creations = Rand.WeightedDistribution();
                for (int i = 0; i < creations; ++i)
                {
                    env.instances.Add(new Instance(env, c));
                }
            }

            string GenerateTestHarness(string testData)
            {
                string testHarness = File.ReadAllText("data/TestHarness.cs.template");

                var csComposites = new StringBuilder();
                foreach (var c in env.types)
                {
                    csComposites.Append(Util.Indent(c.WriteCsharpDefinition(), 2));
                }

                var types = string.Join(", ", env.types.Select(c => $"typeof({c.name})"));
                var filename = "data/Fuzzgen.FuzzgenTest.xml";

                return testHarness
                    .Replace("<<COMPOSITES>>", csComposites.ToString())
                    .Replace("<<TYPES>>", types)
                    .Replace("<<FILENAME>>", $"\"{filename}\"")
                    .Replace("<<TESTS>>", testData);
            }

            string testCode = GenerateTestHarness("");

            string xmlCode;

            // Output xml
            {
                var sb = new StringBuilder();
                sb.AppendLine("<Defs>");

                foreach (var i in env.instances)
                {
                    sb.AppendLine(Util.Indent(i.WriteXmlDef()));
                }

                sb.AppendLine("</Defs>");

                xmlCode = sb.ToString();
            }

            // This is a bit janky; we want to use Def features when doing generation, but now we need to blow it away to generate the .cs code
            // So I guess maybe it would be nice to have non-global state right now :V

            Def.Database.Clear();

            var bootstrapAssembly = DefUtilLib.Compilation.Compile(testCode, new System.Reflection.Assembly[] { });
            bootstrapAssembly.GetType("DefTest.Harness").GetMethod("Setup").Invoke(null, null);

            var parser = new Def.Parser();
            parser.AddString(xmlCode);
            parser.Finish();

            var composer = new Def.Composer();
            var tests = composer.ComposeValidation();

            string finalCode = GenerateTestHarness(tests);

            string path = $"../../test/data/validation/parser/{System.DateTimeOffset.Now:yyyyMMddhhmmss}";
            System.IO.Directory.CreateDirectory(path);

            DefUtilLib.Compress.WriteToFile(System.IO.Path.Combine(path, "Harness.cs"), finalCode);
            DefUtilLib.Compress.WriteToFile(System.IO.Path.Combine(path, "data.xml"), xmlCode);
        }
    }
}
