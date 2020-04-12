namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        class RootDef : Def.Def
        {

        }

        class ParentDef : RootDef
        {

        }

        class ChildDef : ParentDef
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

        class NotActuallyADef
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
    }
}
