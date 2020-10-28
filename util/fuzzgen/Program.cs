
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace Fuzzgen
{
    internal static class Program
    {
        private static string[] AnimalWords;
        private static string GenerateIdentifier()
        {
            string name = "";

            for (int i = 0; i < 3; ++i)
            {
                name += AnimalWords[Rand.Next(AnimalWords.Length)];
            }

            return name;
        }

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

            // Init animal words list
            AnimalWords = File.ReadAllText("data/animals.txt").Split(new char[] { ' ', '\r', '\n' }).Distinct().ToArray();
        }

        private static void Main()
        {
            Init();

            // Generate initial composites
            var composites = new List<Composite>();
            for (int i = 0; i < 100; ++i)
            {
                var c = new Composite();
                c.name = GenerateIdentifier();

                c.type = CompositeTypeDistribution.Distribution.Choose();
                if (c.type == Composite.Type.Def)
                {
                    c.name += "Def";
                }

                composites.Add(c);
            }

            // Generate parameters
            foreach (var c in composites)
            {
                int parameterCount = Rand.WeightedDistribution();

                for (int i = 0; i < parameterCount; ++i)
                {
                    var m = new Member();
                    m.name = GenerateIdentifier();

                    m.type = MemberTypeDistribution.Distribution.Choose();

                    c.members.Add(m);
                }
            }

            // Generate instances
            var instances = new List<Instance>();
            foreach (var c in composites)
            {
                if (c.type != Composite.Type.Def)
                {
                    continue;
                }

                int creations = Rand.WeightedDistribution();
                for (int i = 0; i < creations; ++i)
                {
                    var instance = new Instance();
                    instance.defName = GenerateIdentifier();
                    instance.composite = c;

                    float chance = Rand.Next(1f);
                    foreach (var member in c.members)
                    {
                        if (Rand.Next(1f) < chance)
                        {
                            // generate a value for this member
                            instance.values[member] = Rand.NextInt();
                        }
                    }

                    instances.Add(instance);
                }
            }

            // Output cs file
            {
                string testHarness = File.ReadAllText("data/TestHarness.cs.template");

                var csComposites = new StringBuilder();
                foreach (var c in composites)
                {
                    csComposites.Append(Util.Indent(c.WriteCsharp(), 2));
                }

                var tests = new StringBuilder();
                foreach (var i in instances)
                {
                    tests.Append(Util.Indent(i.WriteCsharp(), 3));
                }

                var types = string.Join(", ", composites.Select(c => $"typeof({c.name})"));
                var filename = "data/Fuzzgen.FuzzgenTest.xml";

                testHarness = testHarness
                    .Replace("<<COMPOSITES>>", csComposites.ToString())
                    .Replace("<<TYPES>>", types)
                    .Replace("<<FILENAME>>", $"\"{filename}\"")
                    .Replace("<<TESTS>>", tests.ToString());

                Dbg.Inf(testHarness);

                File.WriteAllText("../../test/Fuzzgen.cs", testHarness);
            }

            // Output xml
            {
                var sb = new StringBuilder();
                sb.AppendLine("<Defs>");

                foreach (var i in instances)
                {
                    sb.AppendLine(Util.Indent(i.WriteXml()));
                }

                sb.AppendLine("</Defs>");

                Dbg.Inf(sb.ToString());
                File.WriteAllText("../../test/data/Fuzzgen.FuzzgenTest.xml", sb.ToString());
            }
        }
    }
}
