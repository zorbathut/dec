
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fuzzgen
{
    internal static class Program
    {
        private static Random Rand = new Random();

        private static string[] AnimalWords;
        private static string GenerateClassName()
        {
            string name = "";

            for (int i = 0; i < 3; ++i)
            {
                name += AnimalWords[Rand.Next(0, AnimalWords.Length)];
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

            // Generate data
            var composites = new List<Composite>();
            for (int i = 0; i < 100; ++i)
            {
                var c = new Composite();
                c.name = GenerateClassName();

                c.type = CompositeTypeDistribution.Distribution.Choose();
                if (c.type == Composite.Type.Def)
                {
                    c.name += "Def";
                }

                composites.Add(c);
            }

            // Output data
            foreach (var c in composites)
            {
                Dbg.Inf(c.WriteToOutput());
            }
        }
    }
}
