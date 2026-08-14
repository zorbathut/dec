using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ParserDependency : Base
    {
        private static List<string> setupOrder;

        [SetUp]
        public void SetUp()
        {
            setupOrder = new List<string>();
        }

        [Dec.Abstract]
        public abstract class TestDec : Dec.Dec
        {
            [Dec.Setup]
            internal void Record(Action<string> reporter)
            {
                setupOrder.Add(this.GetType().Name);
            }
        }

        // Classes for TestNoDependencies
        public class NoDep_ADec : TestDec { }
        public class NoDep_BDec : TestDec { }
        public class NoDep_CDec : TestDec { }

        [Test]
        public void TestNoDependencies()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NoDep_ADec), typeof(NoDep_BDec), typeof(NoDep_CDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NoDep_ADec decName=""A"" />
                    <NoDep_BDec decName=""B"" />
                    <NoDep_CDec decName=""C"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEquivalent(new[] { "NoDep_ADec", "NoDep_BDec", "NoDep_CDec" }, setupOrder);
        }

        [Dec.SetupAfter(typeof(SimpleAlpha_BDec))]
        public class SimpleAlpha_ADec : TestDec { }
        public class SimpleAlpha_BDec : TestDec { }

        [Test]
        public void TestSimpleDependency_Alphabetic()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleAlpha_ADec), typeof(SimpleAlpha_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <SimpleAlpha_ADec decName=""A"" />
                        <SimpleAlpha_BDec decName=""B"" />
                    </Decs>");

            parser.Finish();

            CollectionAssert.AreEqual(new[] { "SimpleAlpha_BDec", "SimpleAlpha_ADec" }, setupOrder);
        }

        public class SimpleNonAlpha_ADec : TestDec { }
        [Dec.SetupAfter(typeof(SimpleNonAlpha_ADec))]
        public class SimpleNonAlpha_BDec : TestDec { }

        [Test]
        public void TestSimpleDependency_NonAlphabetic()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleNonAlpha_ADec), typeof(SimpleNonAlpha_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <SimpleNonAlpha_ADec decName=""A"" />
                        <SimpleNonAlpha_BDec decName=""B"" />
                    </Decs>");

            parser.Finish();

            CollectionAssert.AreEqual(new[] { "SimpleNonAlpha_ADec", "SimpleNonAlpha_BDec" }, setupOrder);
        }

        // Classes for TestComplexDependencies
        [Dec.SetupAfter(typeof(Complex_BDec))]
        [Dec.SetupAfter(typeof(Complex_CDec))]
        public class Complex_ADec : TestDec { }
        public class Complex_BDec : TestDec { }
        [Dec.SetupAfter(typeof(Complex_BDec))]
        [Dec.SetupAfter(typeof(Complex_DDec))]
        public class Complex_CDec : TestDec { }
        public class Complex_DDec : TestDec { }

        [Test]
        public void TestComplexDependencies()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Complex_ADec), typeof(Complex_BDec), typeof(Complex_CDec), typeof(Complex_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Complex_ADec decName=""A"" />
                    <Complex_BDec decName=""B"" />
                    <Complex_CDec decName=""C"" />
                    <Complex_DDec decName=""D"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "Complex_BDec", "Complex_DDec", "Complex_CDec", "Complex_ADec" }, setupOrder);
        }

        // Classes for TestCyclicDependencies
        [Dec.SetupAfter(typeof(Cyclic_BDec))]
        public class Cyclic_ADec : TestDec { }
        [Dec.SetupAfter(typeof(Cyclic_CDec))]
        public class Cyclic_BDec : TestDec { }
        [Dec.SetupAfter(typeof(Cyclic_ADec))]
        public class Cyclic_CDec : TestDec { }

        [Test]
        public void TestCyclicDependencies()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Cyclic_ADec), typeof(Cyclic_BDec), typeof(Cyclic_CDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Cyclic_ADec decName=""A"" />
                    <Cyclic_BDec decName=""B"" />
                    <Cyclic_CDec decName=""C"" />
                </Decs>");

            ExpectErrors(() => parser.Finish(), err => err.Contains("Cycle detected"));

            // The exact recovery order after a cycle is arbitrary; this pins the current behavior, not a contract.
            CollectionAssert.AreEqual(new[] { "Cyclic_ADec", "Cyclic_CDec", "Cyclic_BDec" }, setupOrder);
        }

        // Classes for TestPartialDependencies and TestDependenciesWithMissingTypes
        [Dec.SetupAfter(typeof(Partial_BDec))]
        public class Partial_ADec : TestDec { }
        [Dec.SetupAfter(typeof(Partial_CDec))]
        public class Partial_BDec : TestDec { }
        [Dec.SetupAfter(typeof(Partial_DDec))]
        public class Partial_CDec : TestDec { }
        public class Partial_DDec : TestDec { }

        [Test]
        public void TestPartialDependencies()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Partial_ADec), typeof(Partial_BDec), typeof(Partial_CDec), typeof(Partial_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Partial_ADec decName=""A"" />
                    <Partial_BDec decName=""B"" />
                    <Partial_CDec decName=""C"" />
                    <Partial_DDec decName=""D"" />
                </Decs>");

            parser.Finish();

            CollectionAssert.AreEqual(new[] { "Partial_DDec", "Partial_CDec", "Partial_BDec", "Partial_ADec" }, setupOrder);
        }

        [Test]
        public void TestDependenciesWithMissingTypes()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Partial_ADec), typeof(Partial_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Partial_ADec decName=""A"" />
                    <Partial_BDec decName=""B"" />
                </Decs>");

            ExpectErrors(() => parser.Finish(), err => err.Contains("no instances"));

            CollectionAssert.AreEqual(new[] { "Partial_BDec", "Partial_ADec" }, setupOrder);
        }
    }
}
