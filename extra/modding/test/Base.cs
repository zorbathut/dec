using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Base : DecTest.Base
    {
        protected const string BaseId = "Game";

        [SetUp]
        public void ClearModding()
        {
            ModResolution.ClearAssembliesLoadedForTest();
        }

        protected static string DirectoryTemp()
        {
            var directory = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "modding-temp", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        protected static string ModXml(string id, string version, params string[] dependencies)
        {
            string dependencyBlock = dependencies.Length == 0 ? "" : $"<dependencies>{string.Join("", dependencies)}</dependencies>";
            return $"<Mod><id>{id}</id><version>{version}</version>{dependencyBlock}</Mod>";
        }

        protected static string Dependency(string id, string kind = null, string version = null, string order = null)
        {
            string result = $"<li><id>{id}</id>";
            if (kind != null)
            {
                result += $"<kind>{kind}</kind>";
            }
            if (version != null)
            {
                result += $"<version>{System.Security.SecurityElement.Escape(version)}</version>";
            }
            if (order != null)
            {
                result += $"<order>{order}</order>";
            }
            return result + "</li>";
        }

        protected static Mod ModRead(string xml, List<ModProblem> problems, string directory = null)
        {
            return ModManifest.Read(xml, "Mod.xml", directory ?? $"/mods/{Guid.NewGuid():N}", BaseId, problems);
        }

        protected static Mod ModRead(string xml)
        {
            var problems = new List<ModProblem>();
            var mod = ModRead(xml, problems);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            return mod;
        }

        protected static Mod Mod(string id, string version, params string[] dependencies)
        {
            return ModRead(ModXml(id, version, dependencies));
        }

        // Names are full assembly names, such as `Lib, Version=1.0.0.0`.
        internal static ModAssembly AssemblyMod(string path, string name, params string[] references)
        {
            var assembly = new ModAssembly() { path = path, name = new System.Reflection.AssemblyName(name) };
            assembly.references.AddRange(references.Select(reference => new System.Reflection.AssemblyName(reference)));
            return assembly;
        }

        internal static AssemblyReserved AssemblyGame(string location, string name)
        {
            return new AssemblyReserved() { location = location, name = name == null ? null : new System.Reflection.AssemblyName(name) };
        }

        protected static ModResolution ResolvePure(params Mod[] mods)
        {
            return ResolvePure("1.0.0", mods);
        }

        protected static ModResolution ResolvePure(string baseVersion, params Mod[] mods)
        {
            return ResolvePure(baseVersion, new List<ModProblem>(), new Dictionary<string, AssemblyReserved>(), mods);
        }

        internal static ModResolution ResolvePure(string baseVersion, List<ModProblem> problems, Dictionary<string, AssemblyReserved> assembliesReserved, params Mod[] mods)
        {
            var resolution = ModResolver.Resolve(Dec.Modding.Mod.CreateBase(BaseId, new SemanticVersioning.Version(baseVersion)), mods, problems, assembliesReserved, decBuiltInto: null);
            AssertInvariant(resolution);
            return resolution;
        }

        protected static void AssertInvariant(ModResolution resolution)
        {
            foreach (var problem in resolution.Problems.Where(problem => problem.Severity == ModProblemSeverity.Error))
            {
                foreach (var mod in problem.Mods)
                {
                    Assert.AreNotEqual(ModStatus.Enabled, resolution.GetStatus(mod), $"{mod} is enabled despite: {problem}");
                }
            }

            foreach (var mod in resolution.ModsInstalled.Where(mod => resolution.GetStatus(mod) == ModStatus.Disabled))
            {
                Assert.IsTrue(resolution.Problems.Any(problem => problem.Severity == ModProblemSeverity.Error && problem.Mods.Contains(mod)), $"{mod} is disabled with no error naming it");
            }

            foreach (var mod in resolution.ModsInstalled.Where(mod => resolution.GetStatus(mod) == ModStatus.Hidden))
            {
                Assert.IsTrue(resolution.Problems.Any(problem => problem.Kind == ModProblemKind.Duplicate && problem.Mods.Skip(1).Contains(mod)), $"{mod} is hidden but didn't lose a duplicate");
            }

            CollectionAssert.AreEqual(resolution.ModsInstalled.Where(mod => resolution.GetStatus(mod) == ModStatus.Enabled).OrderBy(mod => mod.Id, StringComparer.Ordinal), resolution.Mods.OrderBy(mod => mod.Id, StringComparer.Ordinal));
            Assert.IsTrue(resolution.Mods[0].IsBase);
        }

        protected static void AssertKinds(ModResolution resolution, params ModProblemKind[] kinds)
        {
            CollectionAssert.AreEquivalent(kinds, resolution.Problems.Select(problem => problem.Kind), string.Join("\n", resolution.Problems));
        }

        protected static void AssertKinds(List<ModProblem> problems, params ModProblemKind[] kinds)
        {
            CollectionAssert.AreEquivalent(kinds, problems.Select(problem => problem.Kind), string.Join("\n", problems));
        }

        protected static string[] Ids(ModResolution resolution)
        {
            return resolution.Mods.Select(mod => mod.Id).ToArray();
        }
    }
}
