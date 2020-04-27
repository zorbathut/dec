namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    public class Database : Base
    {
        [Test]
        public void DatabaseList([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                    <StubDef defName=""TestDefC"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefA"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefB"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefC"));

            Assert.AreEqual(3, Def.Database<StubDef>.List.Length);

            Assert.IsTrue(Def.Database<StubDef>.List.Contains(Def.Database<StubDef>.Get("TestDefA")));
            Assert.IsTrue(Def.Database<StubDef>.List.Contains(Def.Database<StubDef>.Get("TestDefB")));
            Assert.IsTrue(Def.Database<StubDef>.List.Contains(Def.Database<StubDef>.Get("TestDefC")));
        }

        private class RootDef : Def.Def
        {

        }

        private class ParentDef : RootDef
        {

        }

        private class ChildDef : ParentDef
        {

        }

        [Test]
        public void DatabaseHierarchy([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RootDef), typeof(ParentDef), typeof(ChildDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <RootDef defName=""RootDef"" />
                    <ParentDef defName=""ParentDef"" />
                    <ChildDef defName=""ChildDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var root = Def.Database<RootDef>.Get("RootDef");
            var parent = Def.Database<ParentDef>.Get("ParentDef");
            var child = Def.Database<ChildDef>.Get("ChildDef");

            Assert.IsTrue(Def.Database<RootDef>.List.Contains(root));
            Assert.IsTrue(Def.Database<RootDef>.List.Contains(parent));
            Assert.IsTrue(Def.Database<RootDef>.List.Contains(child));
            Assert.IsTrue(Def.Database<ParentDef>.List.Contains(parent));
            Assert.IsTrue(Def.Database<ParentDef>.List.Contains(child));
            Assert.IsTrue(Def.Database<ChildDef>.List.Contains(child));

            Assert.AreEqual(3, Def.Database<RootDef>.Count);
            Assert.AreEqual(2, Def.Database<ParentDef>.Count);
            Assert.AreEqual(1, Def.Database<ChildDef>.Count);

            Assert.AreEqual(3, Def.Database.Count);
            Assert.AreEqual(3, Def.Database.List.Count());

            Assert.Contains(root, Def.Database.List.ToArray());
            Assert.Contains(parent, Def.Database.List.ToArray());
            Assert.Contains(child, Def.Database.List.ToArray());
        }

        private class NotActuallyADef
        {

        }

        [Test]
        public void DatabaseErrorQuery()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            ExpectErrors(() => Assert.IsNull(Def.Database.Get(typeof(NotActuallyADef), "Fake")));
        }

        private Func<Type, Type> getDefRootType;

        [OneTimeSetUp]
        public void CreateCallbacks()
        {
            var reflectionClass = Assembly.GetAssembly(typeof(Def.Def)).GetType("Def.UtilType");

            var serialize = reflectionClass.GetMethod("GetDefRootType", BindingFlags.NonPublic | BindingFlags.Static);
            getDefRootType = type => (Type)serialize.Invoke(null, new object[] { type });
        }

        abstract class CppAbstractTypeDef : Def.Def
        {
        }

        class CppAbstractTypeDerivedDef : CppAbstractTypeDef
        {
        }

        class CppAbstractTypeDerived2Def : CppAbstractTypeDerivedDef
        {
        }

        [Def.Abstract]
        class DefAbstractTypeDef : Def.Def
        {
        }

        class DefAbstractTypeDerivedDef : DefAbstractTypeDef
        {
        }

        class DefAbstractTypeDerived2Def : DefAbstractTypeDerivedDef
        {
        }

        [Def.Abstract]
        abstract class FullAbstractTypeDef : Def.Def
        {
        }

        class FullAbstractTypeDerivedDef : FullAbstractTypeDef
        {
        }

        class FullAbstractTypeDerived2Def : FullAbstractTypeDerivedDef
        {
        }

        [Def.Abstract]
        abstract class FullAbstractTypeDerived3ADef : FullAbstractTypeDerived2Def
        {
        }

        class FullAbstractTypeDerived4ADef : FullAbstractTypeDerived3ADef
        {
        }

        [Def.Abstract]
        abstract class FullAbstractTypeDerived3BDef : FullAbstractTypeDerived2Def
        {
        }

        class FullAbstractTypeDerived4BDef : FullAbstractTypeDerived3BDef
        {
        }


        [Test]
        public void DefRootTypeTests()
        {
            Assert.AreEqual(typeof(CppAbstractTypeDef), getDefRootType(typeof(CppAbstractTypeDerivedDef)));
            Assert.AreEqual(typeof(CppAbstractTypeDef), getDefRootType(typeof(CppAbstractTypeDef)));

            Assert.AreEqual(typeof(FullAbstractTypeDerivedDef), getDefRootType(typeof(FullAbstractTypeDerivedDef)));

            // now for some errors
            ExpectErrors(() => Assert.IsNull(getDefRootType(typeof(FullAbstractTypeDef))));
            ExpectErrors(() => Assert.IsNull(getDefRootType(typeof(DefAbstractTypeDef))));

            // We've already errored once on DefAbstract, so it's currently not required to happen again (but it's allowed to.)
            Assert.AreEqual(typeof(DefAbstractTypeDerivedDef), getDefRootType(typeof(DefAbstractTypeDerivedDef)));

            Assert.AreEqual(typeof(CppAbstractTypeDef), getDefRootType(typeof(CppAbstractTypeDerived2Def)));
            Assert.AreEqual(typeof(DefAbstractTypeDerivedDef), getDefRootType(typeof(DefAbstractTypeDerived2Def)));
            Assert.AreEqual(typeof(FullAbstractTypeDerivedDef), getDefRootType(typeof(FullAbstractTypeDerived2Def)));

            ExpectErrors(() => Assert.IsNull(getDefRootType(typeof(FullAbstractTypeDerived3ADef))));
            ExpectErrors(() => Assert.AreEqual(typeof(FullAbstractTypeDerived4BDef), getDefRootType(typeof(FullAbstractTypeDerived4BDef))));
        }
    }
}
