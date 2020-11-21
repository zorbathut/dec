namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Composer : Base
    {
        // A lot of the Writer functionality is tested via BehaviorMode.Rewritten in other tests, so these tests mostly handle the Create/Delete/Rename functions.

        public class SomeDefs : Def.Def
        {
            public SomeValues values;
            public SomeDefs defs;
        }

        public class SomeValues : Def.Def
        {
            public int number;
        }

        [Test]
        public void Creation([Values] BehaviorMode mode)
        {
            Def.Database.Create<SomeValues>("Hello").number = 10;
            Def.Database.Create<SomeValues>("Goodbye").number = 42;

            DoBehavior(mode);

            Assert.AreEqual(10, Def.Database<SomeValues>.Get("Hello").number);
            Assert.AreEqual(42, Def.Database<SomeValues>.Get("Goodbye").number);
        }

        [Test]
        public void CreationNonGeneric([Values] BehaviorMode mode)
        {
            (Def.Database.Create(typeof(SomeValues), "Hello") as SomeValues).number = 10;
            (Def.Database.Create(typeof(SomeValues), "Goodbye") as SomeValues).number = 42;

            DoBehavior(mode);

            Assert.AreEqual(10, Def.Database<SomeValues>.Get("Hello").number);
            Assert.AreEqual(42, Def.Database<SomeValues>.Get("Goodbye").number);
        }

        private class NotADef { }

        [Test]
        public void CreationNonGenericNonDef([Values] BehaviorMode mode)
        {
            ExpectErrors(() => Def.Database.Create(typeof(NotADef), "NotADef"));
        }

        [Test]
        public void MultiCreation([Values] BehaviorMode mode)
        {
            Def.Database.Create<SomeDefs>("Defs");
            Def.Database.Create<SomeValues>("Values");

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<SomeDefs>.Get("Defs"));
            Assert.IsNull(Def.Database<SomeValues>.Get("Defs"));

            Assert.IsNull(Def.Database<SomeDefs>.Get("Values"));
            Assert.IsNotNull(Def.Database<SomeValues>.Get("Values"));
        }

        [Test]
        public void FailedCreation()
        {
            Def.Database.Create<SomeDefs>("Def");
            ExpectErrors(() => Def.Database.Create<SomeDefs>("Def"));
            Def.Database.Delete(Def.Database<SomeDefs>.Get("Def"));
            Def.Database.Create<SomeDefs>("Def");
            ExpectErrors(() => Def.Database.Create<SomeDefs>("Def"));
            ExpectErrors(() => Def.Database.Create<SomeDefs>("Def"));
        }

        private class RootDef : Def.Def { }
        private class LeafADef : RootDef { }
        private class LeafBDef : RootDef { }

        [Test]
        public void FailedForkCreation()
        {
            Def.Database.Create<LeafADef>("Def");
            ExpectErrors(() => Def.Database.Create<LeafBDef>("Def"));
            Def.Database.Delete(Def.Database<RootDef>.Get("Def"));
            Def.Database.Create<RootDef>("Def");
            ExpectErrors(() => Def.Database.Create<LeafADef>("Def"));
            ExpectErrors(() => Def.Database.Create<LeafBDef>("Def"));
        }

        [Test]
        public void Databases()
        {
            var selfRef = Def.Database.Create<SomeDefs>("SelfRef");
            var otherRef = Def.Database.Create<SomeDefs>("OtherRef");
            var values = Def.Database.Create<SomeValues>("Values");

            Assert.AreSame(selfRef, Def.Database.Get(typeof(SomeDefs), "SelfRef"));
            Assert.AreSame(otherRef, Def.Database.Get(typeof(SomeDefs), "OtherRef"));
            Assert.AreSame(values, Def.Database.Get(typeof(SomeValues), "Values"));

            Assert.AreSame(selfRef, Def.Database<SomeDefs>.Get("SelfRef"));
            Assert.AreSame(otherRef, Def.Database<SomeDefs>.Get("OtherRef"));
            Assert.AreSame(values, Def.Database<SomeValues>.Get("Values"));
        }

        [Test]
        public void References([Values] BehaviorMode mode)
        {
            var selfRef = Def.Database.Create<SomeDefs>("SelfRef");
            var otherRef = Def.Database.Create<SomeDefs>("OtherRef");
            var values = Def.Database.Create<SomeValues>("Values");

            selfRef.defs = selfRef;
            selfRef.values = values;
            otherRef.defs = selfRef;
            otherRef.values = values;

            DoBehavior(mode);

            Assert.AreSame(Def.Database<SomeDefs>.Get("SelfRef"), Def.Database<SomeDefs>.Get("SelfRef").defs);
            Assert.AreSame(Def.Database<SomeValues>.Get("Values"), Def.Database<SomeDefs>.Get("SelfRef").values);
            Assert.AreSame(Def.Database<SomeDefs>.Get("SelfRef"), Def.Database<SomeDefs>.Get("OtherRef").defs);
            Assert.AreSame(Def.Database<SomeValues>.Get("Values"), Def.Database<SomeDefs>.Get("OtherRef").values);
        }

        public class IntDef : Def.Def
        {
            public int value = 4;
        }

        [Test]
        public void Delete([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            Def.Database.Delete(Def.Database<IntDef>.Get("Two"));

            DoBehavior(mode);

            Assert.AreEqual(1, Def.Database<IntDef>.Get("One").value);
            Assert.IsNull(Def.Database<IntDef>.Get("Two"));
            Assert.AreEqual(3, Def.Database<IntDef>.Get("Three").value);
        }

        [Test]
        public void DoubleDelete([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            var one = Def.Database<IntDef>.Get("One");
            Def.Database.Delete(one);
            ExpectErrors(() => Def.Database.Delete(one));
            Def.Database.Delete(Def.Database<IntDef>.Get("Three"));

            DoBehavior(mode);

            Assert.IsNull(Def.Database<IntDef>.Get("One"));
            Assert.AreEqual(2, Def.Database<IntDef>.Get("Two").value);
            Assert.IsNull(Def.Database<IntDef>.Get("Three"));
        }

        [Test]
        public void CreateDeleteHierarchy()
        {
            var a = Def.Database.Create<LeafADef>("A");

            Assert.AreSame(a, Def.Database<RootDef>.Get("A"));
            Assert.AreSame(a, Def.Database<LeafADef>.Get("A"));
            Assert.AreSame(a, Def.Database.Get(typeof(RootDef), "A"));
            Assert.AreSame(a, Def.Database.Get(typeof(LeafADef), "A"));

            Def.Database.Delete(a);

            Assert.IsNull(Def.Database<RootDef>.Get("A"));
            Assert.IsNull(Def.Database<LeafADef>.Get("A"));
            Assert.IsNull(Def.Database.Get(typeof(RootDef), "A"));
            Assert.IsNull(Def.Database.Get(typeof(LeafADef), "A"));
        }

        [Test]
        public void Rename([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            Def.Database.Rename(Def.Database<IntDef>.Get("One"), "OneBeta");
            Def.Database.Rename(Def.Database<IntDef>.Get("OneBeta"), "OneGamma");

            // yes okay this is confusing
            Def.Database.Rename(Def.Database<IntDef>.Get("Two"), "One");

            DoBehavior(mode);

            Assert.AreEqual(1, Def.Database<IntDef>.Get("OneGamma").value);
            Assert.AreEqual(2, Def.Database<IntDef>.Get("One").value);
            Assert.AreEqual(3, Def.Database<IntDef>.Get("Three").value);
        }

        [Test]
        public void RenameError([Values] BehaviorMode mode)
        {
            var a = Def.Database.Create<StubDef>("A");
            var b = Def.Database.Create<StubDef>("B");
            var c = Def.Database.Create<StubDef>("C");

            ExpectErrors(() => Def.Database.Rename(a, "B"));
            Def.Database.Rename(c, "C");

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("A"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("B"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("C"));
        }

        [Test]
        public void RenameDeleted([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""One""><value>1</value></IntDef>
                    <IntDef defName=""Two""><value>2</value></IntDef>
                    <IntDef defName=""Three""><value>3</value></IntDef>
                </Defs>");
            parser.Finish();

            var three = Def.Database<IntDef>.Get("Three");
            Def.Database.Delete(three);
            ExpectErrors(() => Def.Database.Rename(three, "ThreePhoenix"));

            DoBehavior(mode);

            Assert.AreEqual(1, Def.Database<IntDef>.Get("One").value);
            Assert.AreEqual(2, Def.Database<IntDef>.Get("Two").value);
            Assert.IsNull(Def.Database<IntDef>.Get("Three"));
            Assert.IsNull(Def.Database<IntDef>.Get("ThreePhoenix"));
        }

        [Test]
        public void ReferenceDeleted([Values] BehaviorMode mode)
        {
            var ephemeral = Def.Database.Create<SomeDefs>("Ephemeral");
            var stored = Def.Database.Create<SomeDefs>("Stored");

            stored.defs = ephemeral;

            Def.Database.Delete(ephemeral);

            DoBehavior(mode, expectWriteErrors: true, expectParseErrors: true);

            if (mode != BehaviorMode.Bare)
            {
                Assert.IsNull(Def.Database<SomeDefs>.Get("Stored").defs);
                Assert.IsNull(Def.Database<SomeDefs>.Get("Ephemeral"));
            }
        }

        [Test]
        public void ReferenceReplaced([Values] BehaviorMode mode)
        {
            var ephemeral = Def.Database.Create<SomeDefs>("Ephemeral");
            var stored = Def.Database.Create<SomeDefs>("Stored");

            stored.defs = ephemeral;

            Def.Database.Delete(ephemeral);

            Def.Database.Create<SomeDefs>("Ephemeral");

            DoBehavior(mode, expectWriteErrors: true, expectParseErrors: true);

            if (mode != BehaviorMode.Bare)
            {
                Assert.IsNull(Def.Database<SomeDefs>.Get("Stored").defs);
                Assert.IsNotNull(Def.Database<SomeDefs>.Get("Ephemeral"));
            }
        }

        public class NonSerializedDef : Def.Def
        {
            public int serializedValue = 30;

            [NonSerialized]
            public int nonSerializedValue = 40;
        }

        [Test]
        public void NonSerialized()
        {
            var ephemeral = Def.Database.Create<NonSerializedDef>("TestDef");
            ephemeral.serializedValue = 35;
            ephemeral.nonSerializedValue = 45;

            var writer = new Def.Composer();
            string data = writer.Compose();

            Assert.IsTrue(data.Contains("serializedValue"));
            Assert.IsFalse(data.Contains("nonSerializedValue"));
        }
    }
}
