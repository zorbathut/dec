namespace DefTest
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    [TestFixture]
    public class Validation : Base
    {
        private static Dictionary<string, Assembly> AssemblyLookup = new Dictionary<string, Assembly>();

        [Test, TestCaseSource(nameof(GenerateValidationParser))]
        public void Parser(string id, [Values] BehaviorMode mode)
        {
            string directory = Path.Combine("data", "validation", "parser", id);

            Assembly assembly;
            if (!AssemblyLookup.TryGetValue(id, out assembly))
            {
                // gotta load
                assembly = DefUtilLib.Compilation.Compile(File.ReadAllText(Path.Combine(directory, "Harness.cs")), new Assembly[] { this.GetType().Assembly });
                AssemblyLookup[id] = assembly;
            }

            var type = assembly.GetType("DefTest.Harness");
            type.GetMethod("Setup").Invoke(null, null);

            var parser = new Def.Parser();
            parser.AddDirectory(directory);
            parser.Finish();

            DoBehavior(mode, validation_assemblies: new Assembly[] { assembly });

            type.GetMethod("Validate").Invoke(null, null);
        }

        public static IEnumerable<object[]> GenerateValidationParser()
        {
            PrepCwd();

            var targetDir = Path.Combine("data", "validation", "parser");

            if (!Directory.Exists(targetDir))
            {
                yield break;
            }

            foreach (var path in Directory.GetDirectories(targetDir))
            {
                var id = Path.GetFileName(path);
                yield return new object[] { id, BehaviorMode.Bare };
                yield return new object[] { id, BehaviorMode.RewrittenBare };
                yield return new object[] { id, BehaviorMode.RewrittenPretty };
                yield return new object[] { id, BehaviorMode.Validation };
            }
        }
    }
}
