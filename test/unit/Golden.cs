using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Golden : Base
    {
        private static Dictionary<string, Assembly> AssemblyLookup = new Dictionary<string, Assembly>();

        [Test, TestCaseSource(nameof(GenerateValidationParser))]
        public void Validation(string id, [Values] ParserMode mode)
        {
            string directory = System.IO.Path.Combine("data", "golden", "parser", id);

            Assembly assembly;
            if (!AssemblyLookup.TryGetValue(id, out assembly))
            {
                // gotta load
                assembly = DecUtilLib.Compilation.Compile(DecUtilLib.Compress.ReadFromFile(System.IO.Path.Combine(directory, "Harness.cs")), new Assembly[] { this.GetType().Assembly });
                AssemblyLookup[id] = assembly;
            }

            var type = assembly.GetType("DecTest.Harness");
            type.GetMethod("Setup").Invoke(null, null);

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, DecUtilLib.Compress.ReadFromFile(System.IO.Path.Combine(directory, "data.xml")));
            parser.Finish();

            DoParserTests(mode, validation_assemblies: new Assembly[] { assembly });

            type.GetMethod("Validate").Invoke(null, null);
        }

        public static IEnumerable<object[]> GenerateValidationParser()
        {
            PrepCwd();

            var targetDir = System.IO.Path.Combine("data", "golden", "parser");

            if (!Directory.Exists(targetDir))
            {
                yield break;
            }

            foreach (var path in Directory.GetDirectories(targetDir))
            {
                var id = System.IO.Path.GetFileName(path);
                foreach (ParserMode mode in System.Enum.GetValues(typeof(ParserMode)))
                {
                    yield return new object[] { id, mode };
                }
            }
        }
    }
}
