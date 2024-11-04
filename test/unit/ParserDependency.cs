using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ParserDependency : Base
    {
        private static List<string> postLoadOrder;

        [SetUp]
        public void SetUp()
        {
            postLoadOrder = new List<string>();
        }

        [Dec.Abstract]
        public abstract class TestDec : Dec.Dec
        {
            public override void PostLoad(Action<string> reporter)
            {
                postLoadOrder.Add(this.GetType().Name);
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

            CollectionAssert.AreEquivalent(new[] { "NoDep_ADec", "NoDep_BDec", "NoDep_CDec" }, postLoadOrder);
        }

        [Dec.SetupDependsOn(typeof(SimpleAlpha_BDec))]
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

            CollectionAssert.AreEqual(new[] { "SimpleAlpha_BDec", "SimpleAlpha_ADec" }, postLoadOrder);
        }

        public class SimpleNonAlpha_ADec : TestDec { }
        [Dec.SetupDependsOn(typeof(SimpleNonAlpha_ADec))]
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

            CollectionAssert.AreEqual(new[] { "SimpleNonAlpha_ADec", "SimpleNonAlpha_BDec" }, postLoadOrder);
        }

        // Classes for TestComplexDependencies
        [Dec.SetupDependsOn(typeof(Complex_BDec))]
        [Dec.SetupDependsOn(typeof(Complex_CDec))]
        public class Complex_ADec : TestDec { }
        public class Complex_BDec : TestDec { }
        [Dec.SetupDependsOn(typeof(Complex_BDec))]
        [Dec.SetupDependsOn(typeof(Complex_DDec))]
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

            CollectionAssert.AreEqual(new[] { "Complex_BDec", "Complex_DDec", "Complex_CDec", "Complex_ADec" }, postLoadOrder);
        }

        // Classes for TestCyclicDependencies
        [Dec.SetupDependsOn(typeof(Cyclic_BDec))]
        public class Cyclic_ADec : TestDec { }
        [Dec.SetupDependsOn(typeof(Cyclic_CDec))]
        public class Cyclic_BDec : TestDec { }
        [Dec.SetupDependsOn(typeof(Cyclic_ADec))]
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

            ExpectErrors(() => parser.Finish());

            CollectionAssert.AreEqual(new[] { "Cyclic_CDec", "Cyclic_BDec", "Cyclic_ADec" }, postLoadOrder);
        }

        // Classes for TestPartialDependencies and TestDependenciesWithMissingTypes
        [Dec.SetupDependsOn(typeof(Partial_BDec))]
        public class Partial_ADec : TestDec { }
        [Dec.SetupDependsOn(typeof(Partial_CDec))]
        public class Partial_BDec : TestDec { }
        [Dec.SetupDependsOn(typeof(Partial_DDec))]
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

            CollectionAssert.AreEqual(new[] { "Partial_DDec", "Partial_CDec", "Partial_BDec", "Partial_ADec" }, postLoadOrder);
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

            ExpectErrors(() => parser.Finish());

            CollectionAssert.AreEqual(new[] { "Partial_BDec", "Partial_ADec" }, postLoadOrder);
        }
    }
}