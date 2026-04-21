using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DecTest
{

    [TestFixture]
    public class Database : Base
    {
        [Test]
        public void DatabaseList([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                    <StubDec decName=""TestDecB"" />
                    <StubDec decName=""TestDecC"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDecA"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDecB"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDecC"));

            Assert.AreEqual(3, Dec.Database<StubDec>.List.Length);

            Assert.IsTrue(Dec.Database<StubDec>.List.Contains(Dec.Database<StubDec>.Get("TestDecA")));
            Assert.IsTrue(Dec.Database<StubDec>.List.Contains(Dec.Database<StubDec>.Get("TestDecB")));
            Assert.IsTrue(Dec.Database<StubDec>.List.Contains(Dec.Database<StubDec>.Get("TestDecC")));
        }

        private class RootDec : Dec.Dec
        {

        }

        private class ParentDec : RootDec
        {

        }

        private class ChildDec : ParentDec
        {

        }

        [Test]
        public void DatabaseHierarchy([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RootDec), typeof(ParentDec), typeof(ChildDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RootDec decName=""RootDec"" />
                    <ParentDec decName=""ParentDec"" />
                    <ChildDec decName=""ChildDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var root = Dec.Database<RootDec>.Get("RootDec");
            var parent = Dec.Database<ParentDec>.Get("ParentDec");
            var child = Dec.Database<ChildDec>.Get("ChildDec");

            Assert.IsTrue(Dec.Database<RootDec>.List.Contains(root));
            Assert.IsTrue(Dec.Database<RootDec>.List.Contains(parent));
            Assert.IsTrue(Dec.Database<RootDec>.List.Contains(child));
            Assert.IsTrue(Dec.Database<ParentDec>.List.Contains(parent));
            Assert.IsTrue(Dec.Database<ParentDec>.List.Contains(child));
            Assert.IsTrue(Dec.Database<ChildDec>.List.Contains(child));

            Assert.AreEqual(3, Dec.Database<RootDec>.Count);
            Assert.AreEqual(2, Dec.Database<ParentDec>.Count);
            Assert.AreEqual(1, Dec.Database<ChildDec>.Count);

            Assert.AreEqual(3, Dec.Database.Count);
            Assert.AreEqual(3, Dec.Database.List.Count());

            Assert.Contains(root, Dec.Database.List.ToArray());
            Assert.Contains(parent, Dec.Database.List.ToArray());
            Assert.Contains(child, Dec.Database.List.ToArray());
        }

        private class NotActuallyADec
        {

        }

        [Test]
        public void DatabaseErrorQuery()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            ExpectErrors(() => Assert.IsNull(Dec.Database.Get(typeof(NotActuallyADec), "Fake")), err => err.Contains("NotActuallyADec") && (err.Contains("not a valid Dec") || err.Contains("doesn't even inherit from Dec") || err.Contains("does not exist within a database hierarchy")));
        }

        private Func<Type, Type> getDecRootType;

        [OneTimeSetUp]
        public void CreateCallbacks()
        {
            var reflectionClass = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilType");

            var serialize = reflectionClass.GetMethod("GetDecRootType", BindingFlags.NonPublic | BindingFlags.Static);
            getDecRootType = type => (Type)serialize.Invoke(null, new object[] { type });
        }

        abstract class CppAbstractTypeDec : Dec.Dec
        {
        }

        class CppAbstractTypeDerivedDec : CppAbstractTypeDec
        {
        }

        class CppAbstractTypeDerived2Dec : CppAbstractTypeDerivedDec
        {
        }

        [Dec.Abstract]
        class DecAbstractTypeDec : Dec.Dec
        {
        }

        class DecAbstractTypeDerivedDec : DecAbstractTypeDec
        {
        }

        class DecAbstractTypeDerived2Dec : DecAbstractTypeDerivedDec
        {
        }

        [Dec.Abstract]
        abstract class FullAbstractTypeDec : Dec.Dec
        {
        }

        class FullAbstractTypeDerivedDec : FullAbstractTypeDec
        {
        }

        class FullAbstractTypeDerived2Dec : FullAbstractTypeDerivedDec
        {
        }

        [Dec.Abstract]
        abstract class FullAbstractTypeDerived3ADec : FullAbstractTypeDerived2Dec
        {
        }

        class FullAbstractTypeDerived4ADec : FullAbstractTypeDerived3ADec
        {
        }

        [Dec.Abstract]
        abstract class FullAbstractTypeDerived3BDec : FullAbstractTypeDerived2Dec
        {
        }

        class FullAbstractTypeDerived4BDec : FullAbstractTypeDerived3BDec
        {
        }

        [Test]
        public void DecRootTypeTests()
        {
            Assert.AreEqual(typeof(CppAbstractTypeDec), getDecRootType(typeof(CppAbstractTypeDerivedDec)));
            Assert.AreEqual(typeof(CppAbstractTypeDec), getDecRootType(typeof(CppAbstractTypeDec)));

            Assert.AreEqual(typeof(FullAbstractTypeDerivedDec), getDecRootType(typeof(FullAbstractTypeDerivedDec)));

            // now for some errors
            ExpectErrors(() => Assert.IsNull(getDecRootType(typeof(FullAbstractTypeDec))), err => err.Contains("FullAbstractTypeDec") && (err.Contains("tagged Dec.Abstract") || err.Contains("does not exist within a database hierarchy")));
            ExpectErrors(() => Assert.IsNull(getDecRootType(typeof(DecAbstractTypeDec))), err => err.Contains("DecAbstractTypeDec") && (err.Contains("tagged Dec.Abstract") || err.Contains("does not exist within a database hierarchy")));

            // We've already errored once on DecAbstract, so it's currently not required to happen again (but it's allowed to.)
            Assert.AreEqual(typeof(DecAbstractTypeDerivedDec), getDecRootType(typeof(DecAbstractTypeDerivedDec)));

            Assert.AreEqual(typeof(CppAbstractTypeDec), getDecRootType(typeof(CppAbstractTypeDerived2Dec)));
            Assert.AreEqual(typeof(DecAbstractTypeDerivedDec), getDecRootType(typeof(DecAbstractTypeDerived2Dec)));
            Assert.AreEqual(typeof(FullAbstractTypeDerivedDec), getDecRootType(typeof(FullAbstractTypeDerived2Dec)));

            ExpectErrors(() => Assert.IsNull(getDecRootType(typeof(FullAbstractTypeDerived3ADec))), err => err.Contains("FullAbstractTypeDerived3ADec") && (err.Contains("tagged Dec.Abstract") || err.Contains("does not exist within a database hierarchy")));
            ExpectErrors(() => Assert.AreEqual(typeof(FullAbstractTypeDerived4BDec), getDecRootType(typeof(FullAbstractTypeDerived4BDec))), err => err.Contains("FullAbstractTypeDerived3BDec") && err.Contains("tagged Dec.Abstract"));
        }


        [Test]
        public void EmptyDatabaseWarning()
        {
            int x = 0;

            Dec.Database.Clear();
            ExpectWarnings(() => Dec.Database.Get(typeof(CppAbstractTypeDec), "MissingDec"), wrn => wrn.Contains("no Decs loaded"));

            Dec.Database.Clear();
            ExpectWarnings(() => x = Dec.Database.List.Count(), wrn => wrn.Contains("no Decs loaded"));

            Dec.Database.Clear();
            ExpectWarnings(() => x = Dec.Database.Count, wrn => wrn.Contains("no Decs loaded"));
        }
    }
}
