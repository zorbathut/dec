using System.IO;
using System.Linq;
using System.Text;

namespace Fuzzgen
{
    internal static class Program
    {
        private static void Init()
        {
            // Configure Dec's error reporting
            Dec.Config.InfoHandler = str => Dbg.Inf(str);
            Dec.Config.WarningHandler = str => Dbg.Wrn(str);
            Dec.Config.ErrorHandler = str => Dbg.Err(str);
            Dec.Config.ExceptionHandler = e => Dbg.Ex(e);

            // Configure Dec's namespaces
            Dec.Config.UsingNamespaces = new string[] { "Fuzzgen" };

            // In most cases, you'll just want to read all the XML files in your data directory, which is easy
            var parser = new Dec.Parser();
            parser.AddDirectory("data/dec");
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
                if (c.type == Composite.Type.Dec)
                {
                    c.name += "Dec";
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
                if (c.type != Composite.Type.Dec)
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
                sb.AppendLine("<Decs>");

                foreach (var i in env.instances)
                {
                    sb.AppendLine(Util.Indent(i.WriteXmlDec()));
                }

                sb.AppendLine("</Decs>");

                xmlCode = sb.ToString();
            }

            // This is a bit janky; we want to use Dec features when doing generation, but now we need to blow it away to generate the .cs code
            // So I guess maybe it would be nice to have non-global state right now :V

            Dec.Database.Clear();

            var bootstrapAssembly = DecUtilLib.Compilation.Compile(testCode, new System.Reflection.Assembly[] { });
            bootstrapAssembly.GetType("DecTest.Harness").GetMethod("Setup").Invoke(null, null);

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, xmlCode);
            parser.Finish();

            var composer = new Dec.Composer();
            var tests = composer.ComposeValidation();

            string finalCode = GenerateTestHarness(tests);

            string path = $"../../test/data/golden/parser/{System.DateTimeOffset.Now:yyyyMMddhhmmss}";
            System.IO.Directory.CreateDirectory(path);

            DecUtilLib.Compress.WriteToFile(System.IO.Path.Combine(path, "Harness.cs"), finalCode);
            DecUtilLib.Compress.WriteToFile(System.IO.Path.Combine(path, "data.xml"), xmlCode);
        }
    }
}
