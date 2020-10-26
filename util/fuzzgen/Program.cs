
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

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
                // 0-99, weighted heavily towards 0
                int parameterCount = (int)(Math.Pow(10, Math.Pow(Rand.Next(1f), 2) * 2) - 1);

                for (int i = 0; i < parameterCount; ++i)
                {
                    var m = new Member();
                    m.name = GenerateIdentifier();

                    m.type = MemberTypeDistribution.Distribution.Choose();

                    c.members.Add(m);
                }
            }

            // Output data
            foreach (var c in composites)
            {
                Dbg.Inf(c.WriteToOutput());
            }
        }
    }
}
