using System.Collections.Generic;
using System.Linq;
using Dec.Modding;
using NUnit.Framework;

namespace ModdingTest
{
    [TestFixture]
    public class Order : Base
    {
        [Test]
        public void Ordinal()
        {
            var resolution = ResolvePure(Mod("Zeta", "1.0.0"), Mod("alpha", "1.0.0"), Mod("Beta", "1.0.0"));
            CollectionAssert.AreEqual(new[] { BaseId, "Beta", "Zeta", "alpha" }, Ids(resolution));
        }

        [Test]
        public void BaseFirstDespiteSorting()
        {
            var resolution = ResolvePure(Mod("AAA", "1.0.0", Dependency(BaseId, kind: "Accommodated")), Mod("A", "1.0.0", Dependency(BaseId, kind: "Supported", version: "^5.0.0")));
            Assert.AreEqual(BaseId, Ids(resolution)[0]);
        }

        [Test]
        public void After()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Accommodated")), Mod("B", "1.0.0"));
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(resolution));
        }

        [Test]
        public void Before()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0"), Mod("B", "1.0.0", Dependency("A", kind: "Accommodated", order: "Before")));
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(resolution));
        }

        [Test]
        public void Unordered()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", order: "Unordered")), Mod("B", "1.0.0"));
            CollectionAssert.AreEqual(new[] { BaseId, "A", "B" }, Ids(resolution));
        }

        [Test]
        public void RangeMismatchNoEdge()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Accommodated", version: "^2.0.0")), Mod("B", "1.0.0"));
            CollectionAssert.AreEqual(new[] { BaseId, "A", "B" }, Ids(resolution));
        }

        [Test]
        public void DiscouragedOrders()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0"), Mod("B", "1.0.0", Dependency("A", kind: "Discouraged", order: "Before")));
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(resolution));
        }

        [Test]
        public void Diamond()
        {
            var resolution = ResolvePure(
                Mod("D", "1.0.0", Dependency("B"), Dependency("C")),
                Mod("C", "1.0.0", Dependency("A")),
                Mod("B", "1.0.0", Dependency("A")),
                Mod("A", "1.0.0"));
            CollectionAssert.AreEqual(new[] { BaseId, "A", "B", "C", "D" }, Ids(resolution));
        }

        [Test]
        public void Permutations()
        {
            var mods = new[]
            {
                Mod("E", "1.0.0", Dependency("A", kind: "Accommodated", order: "Before")),
                Mod("D", "1.0.0", Dependency("B"), Dependency("C")),
                Mod("C", "1.0.0", Dependency("A")),
                Mod("B", "1.0.0", Dependency("A")),
                Mod("A", "1.0.0", Dependency("Absent", kind: "Recommended"), Dependency("D", kind: "Discouraged", order: "Unordered")),
                Mod("F", "1.0.0", Dependency("Missing")),
                Mod("G", "1.0.0", Dependency("F")),
            };

            var expected = ResolvePure(mods);
            var expectedProblems = expected.Problems.Select(problem => problem.Message).ToList();
            Assert.AreEqual(4, expectedProblems.Count);

            foreach (var permutation in Permute(mods.ToList()))
            {
                var resolution = ResolvePure(permutation.ToArray());
                CollectionAssert.AreEqual(Ids(expected), Ids(resolution));
                CollectionAssert.AreEqual(expectedProblems, resolution.Problems.Select(problem => problem.Message));
            }
        }

        private static IEnumerable<List<Mod>> Permute(List<Mod> mods)
        {
            if (mods.Count <= 1)
            {
                yield return mods;
                yield break;
            }

            for (int i = 0; i < mods.Count; ++i)
            {
                var rest = mods.Where((mod, index) => index != i).ToList();
                foreach (var tail in Permute(rest))
                {
                    tail.Insert(0, mods[i]);
                    yield return tail;
                }
            }
        }

        [Test]
        public void CycleTwo()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", order: "After")), Mod("B", "1.0.0", Dependency("A", order: "After")), Mod("C", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.Cycle);
            var problem = resolution.Problems.Single();
            CollectionAssert.AreEquivalent(new[] { "A", "B" }, problem.Mods.Select(mod => mod.Id));
            StringAssert.Contains("`A`", problem.Message);
            StringAssert.Contains("`B`", problem.Message);
            CollectionAssert.AreEqual(new[] { BaseId, "C" }, Ids(resolution));
        }

        [Test]
        public void CycleBefore()
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Supported", order: "Before")), Mod("B", "1.0.0", Dependency("A", kind: "Accommodated", order: "Before")), Mod("C", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.Cycle);

            var before = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Supported", order: "After")), Mod("B", "1.0.0", Dependency("A", kind: "Accommodated", order: "Before")));
            AssertKinds(before);
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(before));
        }

        [TestCase("Required")]
        [TestCase("Supported")]
        [TestCase("Accommodated")]
        public void PreferencesMutual(string kind)
        {
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: kind)), Mod("B", "1.0.0", Dependency("A", kind: kind)), Mod("C", "1.0.0"));
            AssertKinds(resolution, ModProblemKind.CycleBroken);
            Assert.AreEqual(ModProblemSeverity.Warning, resolution.Problems[0].Severity);
            CollectionAssert.AreEquivalent(new[] { "A", "B" }, resolution.Problems[0].Mods.Select(mod => mod.Id));
            CollectionAssert.AreEqual(new[] { BaseId, "A", "B", "C" }, Ids(resolution));
        }

        [Test]
        public void PreferenceYieldsToExplicit()
        {
            // B's explicit order stands; only A's default one is dropped.
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Supported")), Mod("B", "1.0.0", Dependency("A", kind: "Supported", order: "After")));
            AssertKinds(resolution, ModProblemKind.CycleBroken);
            CollectionAssert.AreEqual(new[] { BaseId, "A", "B" }, Ids(resolution));
        }

        [Test]
        public void PreferenceSamePairAsExplicit()
        {
            // A's default After on B and B's explicit Before on A are the same edge; the explicit one keeps it.
            var resolution = ResolvePure(Mod("A", "1.0.0", Dependency("B", kind: "Supported")), Mod("B", "1.0.0", Dependency("A", kind: "Supported", order: "Before")));
            AssertKinds(resolution);
            CollectionAssert.AreEqual(new[] { BaseId, "B", "A" }, Ids(resolution));
        }

        [Test]
        public void PreferencesOutsideCycleKept()
        {
            // Only preferences inside the cycle are dropped; C's still puts it after M, though C sorts first.
            var resolution = ResolvePure(Mod("M", "1.0.0", Dependency("N")), Mod("N", "1.0.0", Dependency("M")), Mod("C", "1.0.0", Dependency("M")));
            AssertKinds(resolution, ModProblemKind.CycleBroken);
            CollectionAssert.AreEqual(new[] { BaseId, "M", "C", "N" }, Ids(resolution));
        }

        [Test]
        public void ExplicitCycleSurvivesPreferenceDrop()
        {
            // The preference is dropped, but the explicit edges still form a cycle.
            var resolution = ResolvePure(
                Mod("A", "1.0.0", Dependency("B", order: "After"), Dependency("C", kind: "Supported")),
                Mod("B", "1.0.0", Dependency("A", order: "After")),
                Mod("C", "1.0.0", Dependency("A", kind: "Supported")));
            AssertKinds(resolution, ModProblemKind.CycleBroken, ModProblemKind.Cycle, ModProblemKind.DependencyOptional);
            CollectionAssert.AreEqual(new[] { BaseId, "C" }, Ids(resolution));
        }

        [Test]
        public void CycleGreedyTrap()
        {
            // Load order edges A->B, B->C, C->B, C->D, D->A, where X->Y means Y loads after X. Following each node's smallest successor loops on B and C forever.
            var resolution = ResolvePure(
                Mod("A", "1.0.0", Dependency("D", kind: "Accommodated", order: "After")),
                Mod("B", "1.0.0", Dependency("A", kind: "Accommodated", order: "After"), Dependency("C", kind: "Supported", order: "After")),
                Mod("C", "1.0.0", Dependency("B", kind: "Accommodated", order: "After")),
                Mod("D", "1.0.0", Dependency("C", kind: "Accommodated", order: "After")));

            AssertKinds(resolution, ModProblemKind.Cycle);
            Assert.AreEqual(4, resolution.Problems.Single().Mods.Count);

            // The shortest cycle through A passes through every member, so the description names them all.
            var message = resolution.Problems.Single().Message;
            foreach (var id in new[] { "A", "B", "C", "D" })
            {
                StringAssert.Contains($"`{id}`", message);
            }
        }

        [Test]
        public void CycleDownstream()
        {
            var resolution = ResolvePure(
                Mod("A", "1.0.0", Dependency("B", order: "After")),
                Mod("B", "1.0.0", Dependency("A", order: "After")),
                Mod("Follower", "1.0.0", Dependency("A", kind: "Accommodated")),
                Mod("Needer", "1.0.0", Dependency("A")));

            AssertKinds(resolution, ModProblemKind.Cycle, ModProblemKind.DependencyDisabled);
            CollectionAssert.AreEqual(new[] { BaseId, "Follower" }, Ids(resolution));
        }
    }
}
