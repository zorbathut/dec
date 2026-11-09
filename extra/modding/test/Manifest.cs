using System.Collections.Generic;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Manifest : Base
    {
        private static List<ModProblem> ReadProblems(string xml, out Mod mod)
        {
            var problems = new List<ModProblem>();
            mod = ModRead(xml, problems);
            return problems;
        }

        private static void AssertManifestError(string xml)
        {
            var problems = ReadProblems(xml, out var mod);
            Assert.IsTrue(problems.Any(problem => problem.Severity == ModProblemSeverity.Error), string.Join("\n", problems));
            Assert.IsTrue(mod.manifestError);
        }

        [Test]
        public void Minimal()
        {
            var mod = ModRead("<Mod><id>Author.Mod</id><version>1.2.3</version></Mod>");

            Assert.AreEqual("Author.Mod", mod.Id);
            Assert.AreEqual(new SemanticVersioning.Version("1.2.3"), mod.Version);
            Assert.IsNull(mod.Name);
            Assert.IsNull(mod.Description);
            Assert.IsEmpty(mod.Authors);
            Assert.IsEmpty(mod.Urls);
            Assert.IsEmpty(mod.Dependencies);
            Assert.IsFalse(mod.IsBase);
        }

        [Test]
        public void Full()
        {
            var mod = ModRead(@"
                <Mod>
                    <schemaVersion>1</schemaVersion>
                    <id>Author.Mod</id>
                    <name>Mod Name</name>
                    <description>Mod description</description>
                    <version>2.0.0-beta.1+build.5</version>
                    <authors>
                        <li><name>Alpha</name><email>alpha@example.com</email><urls><li><url>https://example.com/alpha</url><label>Site</label></li></urls></li>
                        <li><email>beta@example.com</email></li>
                    </authors>
                    <urls>
                        <li><url>https://example.com/mod</url><label>Home</label></li>
                        <li><url>https://example.com/source</url></li>
                    </urls>
                    <dependencies>
                        <li><id>Other.Lib</id><kind>Supported</kind><version>^1.2.0</version><order>Before</order><text>Integration text</text></li>
                        <li><id>Plain</id></li>
                    </dependencies>
                </Mod>");

            Assert.AreEqual("Mod Name", mod.Name);
            Assert.AreEqual("Mod description", mod.Description);
            Assert.AreEqual("beta.1", mod.Version.PreRelease);

            Assert.AreEqual(2, mod.Authors.Count);
            Assert.AreEqual("Alpha", mod.Authors[0].Name);
            Assert.AreEqual("alpha@example.com", mod.Authors[0].Email);
            Assert.AreEqual("https://example.com/alpha", mod.Authors[0].Urls.Single().Url);
            Assert.AreEqual("Site", mod.Authors[0].Urls.Single().Label);
            Assert.IsNull(mod.Authors[1].Name);
            Assert.IsEmpty(mod.Authors[1].Urls);

            Assert.AreEqual(2, mod.Urls.Count);
            Assert.AreEqual("Home", mod.Urls[0].Label);
            Assert.IsNull(mod.Urls[1].Label);

            var lib = mod.Dependencies[0];
            Assert.AreEqual("Other.Lib", lib.Id);
            Assert.AreEqual(ModDependencyKind.Supported, lib.Kind);
            Assert.AreEqual(new SemanticVersioning.Range("^1.2.0"), lib.Version);
            Assert.AreEqual(ModDependencyOrder.Before, lib.Order);
            Assert.AreEqual("Integration text", lib.Text);

            var plain = mod.Dependencies[1];
            Assert.AreEqual(ModDependencyKind.Required, plain.Kind);
            Assert.IsNull(plain.Version);
            Assert.AreEqual(ModDependencyOrder.After, plain.Order);
            Assert.IsNull(plain.Text);
        }

        [Test]
        public void MissingId()
        {
            AssertManifestError("<Mod><version>1.0.0</version></Mod>");
        }

        [Test]
        public void MissingVersion()
        {
            AssertManifestError("<Mod><id>A</id></Mod>");
        }

        [TestCase("1a")]
        [TestCase("_a")]
        [TestCase("a b")]
        [TestCase("ä")]
        [TestCase("aä")]
        [TestCase("a١")]
        [TestCase("KelvinK")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public void IdInvalid(string id)
        {
            var problems = ReadProblems($"<Mod><id>{id}</id><version>1.0.0</version></Mod>", out var mod);
            Assert.IsNotEmpty(problems);
            Assert.IsNull(mod.Id);
            Assert.IsTrue(mod.manifestError);
        }

        [TestCase("A")]
        [TestCase("Au.Mod-x_2")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public void IdValid(string id)
        {
            Assert.AreEqual(id, ModRead($"<Mod><id>{id}</id><version>1.0.0</version></Mod>").Id);
        }

        [TestCase("1.0")]
        [TestCase("1.0.0.0")]
        [TestCase("01.0.0")]
        [TestCase("v1.0.0")]
        [TestCase("=1.0.0")]
        [TestCase("1.0.0-01")]
        [TestCase("1. 0.0")]
        [TestCase("99999999999.0.0")]
        [TestCase("١.0.0")]
        public void VersionInvalid(string version)
        {
            var problems = ReadProblems($"<Mod><id>A</id><version>{version}</version></Mod>", out var mod);
            Assert.IsNotEmpty(problems);
            Assert.IsNull(mod.Version);
            Assert.IsTrue(mod.manifestError);
        }

        [TestCase("0.0.0")]
        [TestCase(" 1.0.0")]
        [TestCase("1.0.0 ")]
        [TestCase("\n    1.0.0\n")]
        [TestCase("1.0.0-beta.1+b")]
        [TestCase("1.0.0+build")]
        [TestCase("10.20.30-rc.1.alpha-x")]
        public void VersionValid(string version)
        {
            Assert.AreEqual(new SemanticVersioning.Version(version.Trim()), ModRead($"<Mod><id>A</id><version>{version}</version></Mod>").Version);
        }

        [Test]
        public void IdTrimmed()
        {
            var mod = ModRead("<Mod><id>\n    Author.Mod\n</id><version>1.0.0</version><dependencies><li><id> Other </id></li></dependencies></Mod>");
            Assert.AreEqual("Author.Mod", mod.Id);
            Assert.AreEqual("Other", mod.Dependencies.Single().Id);
        }

        [Test]
        public void SchemaNewer()
        {
            // Dec still points out the elements it doesn't recognize; none of them become problems.
            List<ModProblem> problems = null;
            Mod mod = null;
            ExpectWarnings(() => problems = ReadProblems("<Mod><schemaVersion>2</schemaVersion><id>A</id><name>Named</name><version>whatever</version><garbage><li>stuff</li></garbage></Mod>", out mod), warning => warning.Contains("garbage"));
            AssertKinds(problems, ModProblemKind.SchemaUnsupported);
            Assert.AreEqual(ModProblemSeverity.Error, problems[0].Severity);
            Assert.AreEqual("A", mod.Id);
            Assert.AreEqual("Named", mod.Name);
            Assert.IsTrue(mod.manifestError);
        }

        [Test]
        public void SchemaNewerInvalidId()
        {
            var problems = ReadProblems("<Mod><schemaVersion>2</schemaVersion><id>1bad</id></Mod>", out var mod);
            AssertKinds(problems, ModProblemKind.SchemaUnsupported);
            Assert.IsNull(mod.Id);
        }

        [Test]
        public void SchemaNotInteger()
        {
            var problems = ReadProblems("<Mod><schemaVersion>abc</schemaVersion><id>A</id><version>1.0.0</version></Mod>", out var mod);
            Assert.AreEqual(1, problems.Count, string.Join("\n", problems));
            Assert.AreEqual(ModProblemSeverity.Error, problems[0].Severity);
            Assert.IsTrue(mod.manifestError);
        }

        [Test]
        public void SchemaZero()
        {
            AssertManifestError("<Mod><schemaVersion>0</schemaVersion><id>A</id><version>1.0.0</version></Mod>");
        }

        [Test]
        public void UnknownElement()
        {
            // Reported by Dec, not as a problem with the mod, which still loads.
            List<ModProblem> problems = null;
            Mod mod = null;
            ExpectWarnings(() => problems = ReadProblems("<Mod><id>A</id><version>1.0.0</version><fromTheFuture>1</fromTheFuture></Mod>", out mod), warning => warning.Contains("fromTheFuture"));
            AssertKinds(problems);
            Assert.IsFalse(mod.manifestError);
        }

        [Test]
        public void WrongRoot()
        {
            List<ModProblem> problems = null;
            Mod mod = null;
            ExpectWarnings(() => problems = ReadProblems("<About><id>A</id><version>1.0.0</version></About>", out mod), warning => warning.Contains("About"));
            AssertKinds(problems);
            Assert.IsFalse(mod.manifestError);
        }

        [Test]
        public void Malformed()
        {
            List<ModProblem> problems = null;
            Mod mod = null;
            ExpectErrors(() => problems = ReadProblems("<Mod><id>A</id>", out mod), error => error.Contains("Malformed"));
            AssertKinds(problems, ModProblemKind.Manifest);
            Assert.AreEqual(ModProblemSeverity.Error, problems[0].Severity);
            Assert.IsNull(mod.Id);
            Assert.IsTrue(mod.manifestError);
        }

        [Test]
        public void RootNull()
        {
            var problems = new List<ModProblem>();
            var mod = ModRead(@"<Mod null=""true"" />", problems);
            Assert.IsTrue(problems.Any(problem => problem.Severity == ModProblemSeverity.Error));
            var resolution = ResolvePure("1.0.0", problems, new Dictionary<string, AssemblyReserved>(), mod);
            Assert.AreEqual(ModStatus.Disabled, resolution.GetStatus(mod));
        }

        [Test]
        public void AuthorEmpty()
        {
            AssertManifestError("<Mod><id>A</id><version>1.0.0</version><authors><li /></authors></Mod>");
        }

        [Test]
        public void AuthorUrlOnly()
        {
            var mod = ModRead("<Mod><id>A</id><version>1.0.0</version><authors><li><urls><li><url>https://example.com</url></li></urls></li></authors></Mod>");
            Assert.AreEqual("https://example.com", mod.Authors.Single().Urls.Single().Url);
        }

        [Test]
        public void UrlMissing()
        {
            AssertManifestError("<Mod><id>A</id><version>1.0.0</version><urls><li><label>Nothing</label></li></urls></Mod>");
        }

        [Test]
        public void ListsNull()
        {
            var mod = ModRead(@"<Mod><id>A</id><version>1.0.0</version><authors null=""true"" /><urls null=""true"" /><dependencies><li null=""true"" /></dependencies></Mod>");
            Assert.IsEmpty(mod.Authors);
            Assert.IsEmpty(mod.Urls);
            Assert.IsEmpty(mod.Dependencies);
        }

        [Test]
        public void DependencyMissingId()
        {
            AssertManifestError(ModXml("A", "1.0.0", "<li><kind>Supported</kind></li>"));
        }

        [TestCase("^^1")]
        [TestCase("latest")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("^99999999999")]
        public void DependencyRangeInvalid(string range)
        {
            AssertManifestError(ModXml("A", "1.0.0", $"<li><id>B</id><version>{range}</version></li>"));
        }

        [Test]
        public void DependencySelf()
        {
            AssertManifestError(ModXml("A", "1.0.0", Dependency("A", kind: "Supported")));
        }

        [Test]
        public void DependencyKindUnknown()
        {
            AssertManifestError(ModXml("A", "1.0.0", Dependency("B", kind: "Mandatory")));
        }

        [Test]
        public void IncompatibleOrder()
        {
            AssertManifestError(ModXml("A", "1.0.0", Dependency("B", kind: "Incompatible", order: "After")));
        }

        [Test]
        public void IncompatibleOrderDefault()
        {
            var mod = Mod("A", "1.0.0", Dependency("B", kind: "Incompatible"));
            Assert.AreEqual(ModDependencyOrder.Unordered, mod.Dependencies.Single().Order);
        }

        [TestCase("Before")]
        [TestCase("Unordered")]
        public void BaseOrder(string order)
        {
            AssertManifestError(ModXml("A", "1.0.0", Dependency(BaseId, version: "^1.0.0", order: order)));
        }

        [Test]
        public void BaseOrderAfter()
        {
            Mod("A", "1.0.0", Dependency(BaseId, version: "^1.0.0", order: "After"));
        }

        [Test]
        public void RequiredTwice()
        {
            AssertManifestError(ModXml("A", "1.0.0", Dependency("B", version: "^1.0.0"), Dependency("B", version: "^2.0.0")));
        }

        [TestCase("^1.0.0", "^1.2.0")]
        [TestCase(null, "^1.0.0")]
        [TestCase(null, null)]
        [TestCase("1.x || 3.x", "3.1.0")]
        [TestCase("<=1.0.0", ">=1.0.0")]
        [TestCase("<2.0.0", ">=2.0.0-0")]
        [TestCase("1.2.3 - 1.4.0", "~1.4.0")]
        public void Overlap(string a, string b)
        {
            // Each range is valid alone, so the single error is the overlap.
            Mod("A", "1.0.0", Dependency("B", kind: "Recommended", version: a));
            Mod("A", "1.0.0", Dependency("B", kind: "Discouraged", version: b));

            var problems = ReadProblems(ModXml("A", "1.0.0", Dependency("B", kind: "Recommended", version: a), Dependency("B", kind: "Discouraged", version: b)), out var mod);
            Assert.AreEqual(1, problems.Count, string.Join("\n", problems));
            Assert.AreEqual(ModProblemSeverity.Error, problems[0].Severity);
            Assert.IsTrue(mod.manifestError);
        }

        [TestCase("^1.0.0", "^2.0.0")]
        [TestCase("<2.0.0", ">=2.0.0")]
        [TestCase("1.x || 3.x", "2.x")]
        [TestCase("<1.0.0", ">1.0.0")]
        [TestCase("^1.0.0", "1.0.0-beta")]
        public void Disjoint(string a, string b)
        {
            var mod = Mod("A", "1.0.0", Dependency("B", kind: "Recommended", version: a), Dependency("B", kind: "Discouraged", version: b));
            Assert.AreEqual(2, mod.Dependencies.Count);
        }

        [Test]
        public void DisjointWithRequired()
        {
            Mod("A", "1.0.0", Dependency("B", version: ">=2.0.0"), Dependency("B", kind: "Incompatible", version: "<2.0.0"));
        }

        [Test]
        public void OverlapWithRequired()
        {
            AssertManifestError(ModXml("A", "1.0.0", Dependency("B", version: "^1.0.0"), Dependency("B", kind: "Discouraged", version: "1.3.x")));
        }
    }
}
