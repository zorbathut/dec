using System;
using System.Collections.Generic;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Report : Base
    {
        [Test]
        public void EachProblemOnce()
        {
            var problems = new List<ModProblem>();
            var broken = ModRead("<Mod><id>U</id><version>1.0</version></Mod>", problems);
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, string>(),
                broken,
                Mod("A", "1.0.0", Dependency("Missing")),
                Mod("B", "1.0.0", Dependency("C", kind: "Discouraged")),
                Mod("C", "1.0.0"),
                Mod("D", "1.0.0", Dependency("Z", kind: "Recommended")));

            var reported = new List<(ModProblemSeverity severity, string message)>();

            ExpectWarningsAndErrors(() =>
            {
                var info = Dec.Config.InfoHandler;
                var warning = Dec.Config.WarningHandler;
                var error = Dec.Config.ErrorHandler;
                try
                {
                    Dec.Config.InfoHandler = message => { reported.Add((ModProblemSeverity.Info, message)); info(message); };
                    Dec.Config.WarningHandler = message => { reported.Add((ModProblemSeverity.Warning, message)); warning(message); };
                    Dec.Config.ErrorHandler = message => { reported.Add((ModProblemSeverity.Error, message)); error(message); };

                    resolution.Report();
                }
                finally
                {
                    Dec.Config.InfoHandler = info;
                    Dec.Config.WarningHandler = warning;
                    Dec.Config.ErrorHandler = error;
                }
            }, warning => true, error => true);

            Assert.AreEqual(resolution.Problems.Count, reported.Count);
            foreach (var severity in new[] { ModProblemSeverity.Info, ModProblemSeverity.Warning, ModProblemSeverity.Error })
            {
                Assert.AreEqual(resolution.Problems.Count(problem => problem.Severity == severity), reported.Count(entry => entry.severity == severity), severity.ToString());
                Assert.AreNotEqual(0, reported.Count(entry => entry.severity == severity), severity.ToString());
            }

            for (int i = 0; i < reported.Count; ++i)
            {
                Assert.AreEqual(resolution.Problems[i].Severity, reported[i].severity);
                StringAssert.Contains(resolution.Problems[i].Message, reported[i].message);
            }
        }

        [Test]
        public void ResolveReportsOnlyDec()
        {
            var root = DirectoryTemp();
            var folder = System.IO.Path.Combine(root, "a");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(folder, "Decs"));
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "Mod.xml"), "<Mod><id>A</id><version>1.0.0</version><fromTheFuture /><dependencies><li><id>Missing</id></li></dependencies></Mod>");

            // Dec's own diagnostic is reported as it's found; the harness fails on any other warning or error, so the mod's problems aren't reported yet.
            ModResolution resolution = null;
            ExpectWarnings(() => resolution = new ModLoader(BaseId, "1.0.0", DirectoryTemp()).Resolve(new[] { root }), warning => warning.Contains("fromTheFuture"));
            AssertKinds(resolution, ModProblemKind.DependencyMissing);
        }

        [Test]
        public void MessagesDistinct()
        {
            var missing = ResolvePure(Mod("A", "1.0.0", Dependency("B"))).Problems.Single().Message;
            var version = ResolvePure(Mod("A", "1.0.0", Dependency("B", version: "^2.0.0")), Mod("B", "1.0.0")).Problems.Single().Message;
            var incompatible = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Incompatible")), Mod("B", "1.0.0")).Problems.Single().Message;
            CollectionAssert.AllItemsAreUnique(new[] { missing, version, incompatible });
        }

        [Test]
        public void DependencyTextIncluded()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", "<li><id>B</id><kind>Incompatible</kind><text>Quoted reason</text></li>"), Mod("B", "1.0.0"));
            StringAssert.Contains("Quoted reason", resolution.Problems.Single().Message);
        }
    }
}
