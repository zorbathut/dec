namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Composer : Base
    {
        // A lot of the Writer functionality is tested via BehaviorMode.Rewritten in other tests, so these tests mostly handle the Create/Delete/Rename functions.

        public class SomeDefsDef : Def.Def
        {
            public SomeValuesDef values;
            public SomeDefsDef defs;
        }

        public class SomeValuesDef : Def.Def
        {
            public int number;
        }

        [Test]
        public void Creation([Values] BehaviorMode mode)
        {
            Def.Database.Create<SomeValuesDef>("Hello").number = 10;
            Def.Database.Create<SomeValuesDef>("Goodbye").number = 42;

            DoBehavior(mode);

            Assert.AreEqual(10, Def.Database<SomeValuesDef>.Get("Hello").number);
            Assert.AreEqual(42, Def.Database<SomeValuesDef>.Get("Goodbye").number);
        }

        [Test]
        public void CreationNonGeneric([Values] BehaviorMode mode)
        {
            (Def.Database.Create(typeof(SomeValuesDef), "Hello") as SomeValuesDef).number = 10;
            (Def.Database.Create(typeof(SomeValuesDef), "Goodbye") as SomeValuesDef).number = 42;

            DoBehavior(mode);

            Assert.AreEqual(10, Def.Database<SomeValuesDef>.Get("Hello").number);
            Assert.AreEqual(42, Def.Database<SomeValuesDef>.Get("Goodbye").number);
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
            Def.Database.Create<SomeDefsDef>("Defs");
            Def.Database.Create<SomeValuesDef>("Values");

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<SomeDefsDef>.Get("Defs"));
            Assert.IsNull(Def.Database<SomeValuesDef>.Get("Defs"));

            Assert.IsNull(Def.Database<SomeDefsDef>.Get("Values"));
            Assert.IsNotNull(Def.Database<SomeValuesDef>.Get("Values"));
        }

        [Test]
        public void FailedCreation()
        {
            Def.Database.Create<SomeDefsDef>("Def");
            ExpectErrors(() => Def.Database.Create<SomeDefsDef>("Def"));
            Def.Database.Delete(Def.Database<SomeDefsDef>.Get("Def"));
            Def.Database.Create<SomeDefsDef>("Def");
            ExpectErrors(() => Def.Database.Create<SomeDefsDef>("Def"));
            ExpectErrors(() => Def.Database.Create<SomeDefsDef>("Def"));
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
            var selfRef = Def.Database.Create<SomeDefsDef>("SelfRef");
            var otherRef = Def.Database.Create<SomeDefsDef>("OtherRef");
            var values = Def.Database.Create<SomeValuesDef>("Values");

            Assert.AreSame(selfRef, Def.Database.Get(typeof(SomeDefsDef), "SelfRef"));
            Assert.AreSame(otherRef, Def.Database.Get(typeof(SomeDefsDef), "OtherRef"));
            Assert.AreSame(values, Def.Database.Get(typeof(SomeValuesDef), "Values"));

            Assert.AreSame(selfRef, Def.Database<SomeDefsDef>.Get("SelfRef"));
            Assert.AreSame(otherRef, Def.Database<SomeDefsDef>.Get("OtherRef"));
            Assert.AreSame(values, Def.Database<SomeValuesDef>.Get("Values"));
        }

        [Test]
        public void References([Values] BehaviorMode mode)
        {
            var selfRef = Def.Database.Create<SomeDefsDef>("SelfRef");
            var otherRef = Def.Database.Create<SomeDefsDef>("OtherRef");
            var values = Def.Database.Create<SomeValuesDef>("Values");

            selfRef.defs = selfRef;
            selfRef.values = values;
            otherRef.defs = selfRef;
            otherRef.values = values;

            DoBehavior(mode);

            Assert.AreSame(Def.Database<SomeDefsDef>.Get("SelfRef"), Def.Database<SomeDefsDef>.Get("SelfRef").defs);
            Assert.AreSame(Def.Database<SomeValuesDef>.Get("Values"), Def.Database<SomeDefsDef>.Get("SelfRef").values);
            Assert.AreSame(Def.Database<SomeDefsDef>.Get("SelfRef"), Def.Database<SomeDefsDef>.Get("OtherRef").defs);
            Assert.AreSame(Def.Database<SomeValuesDef>.Get("Values"), Def.Database<SomeDefsDef>.Get("OtherRef").values);
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
        public void ReferenceDeleted([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            var ephemeral = Def.Database.Create<SomeDefsDef>("Ephemeral");
            var stored = Def.Database.Create<SomeDefsDef>("Stored");

            stored.defs = ephemeral;

            Def.Database.Delete(ephemeral);

            DoBehavior(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true, validation_expectWriteErrors: true);

            if (mode != BehaviorMode.Bare)
            {
                Assert.IsNull(Def.Database<SomeDefsDef>.Get("Stored").defs);
                Assert.IsNull(Def.Database<SomeDefsDef>.Get("Ephemeral"));
            }
        }

        [Test]
        public void ReferenceReplaced([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            var ephemeral = Def.Database.Create<SomeDefsDef>("Ephemeral");
            var stored = Def.Database.Create<SomeDefsDef>("Stored");

            stored.defs = ephemeral;

            Def.Database.Delete(ephemeral);

            Def.Database.Create<SomeDefsDef>("Ephemeral");

            DoBehavior(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true, validation_expectWriteErrors: true);

            if (mode != BehaviorMode.Bare)
            {
                Assert.IsNull(Def.Database<SomeDefsDef>.Get("Stored").defs);
                Assert.IsNotNull(Def.Database<SomeDefsDef>.Get("Ephemeral"));
            }
        }

        public class NonSerializedDef : Def.Def
        {
            public int serializedValue = 30;

            [NonSerialized]
            public int nonSerializedValue = 40;
        }

        [Test]
        public void NonSerialized([Values(BehaviorMode.RewrittenBare, BehaviorMode.RewrittenPretty)] BehaviorMode mode)
        {
            var ephemeral = Def.Database.Create<NonSerializedDef>("TestDef");
            ephemeral.serializedValue = 35;
            ephemeral.nonSerializedValue = 45;

            var writer = new Def.Composer();
            string data = writer.ComposeXml(mode == BehaviorMode.RewrittenPretty);

            Assert.IsTrue(data.Contains("serializedValue"));
            Assert.IsFalse(data.Contains("nonSerializedValue"));
        }

        public class EnumContainerDef : Def.Def
        {
            public enum Enum
            {
                Alpha,
                Beta,
                Gamma,
            }

            public Enum alph;
            public Enum bet;
            public Enum gam;
        }

        [Test]
        public void Enum([Values(BehaviorMode.RewrittenBare, BehaviorMode.RewrittenPretty)] BehaviorMode mode)
        {
            var enums = Def.Database.Create<EnumContainerDef>("TestDef");
            enums.alph = EnumContainerDef.Enum.Alpha;
            enums.bet = EnumContainerDef.Enum.Beta;
            enums.gam = EnumContainerDef.Enum.Gamma;

            var writer = new Def.Composer();
            string data = writer.ComposeXml(mode == BehaviorMode.RewrittenPretty);

            Assert.IsTrue(data.Contains("Alpha"));
            Assert.IsTrue(data.Contains("Beta"));
            Assert.IsTrue(data.Contains("Gamma"));

            Assert.IsFalse(data.Contains("__value"));
        }

        [Test]
        public void Pretty([Values(BehaviorMode.RewrittenBare, BehaviorMode.RewrittenPretty)] BehaviorMode mode)
        {
            Def.Database.Create<StubDef>("Hello");

            var output = new Def.Composer().ComposeXml(mode == BehaviorMode.RewrittenPretty);

            Assert.AreEqual(mode == BehaviorMode.RewrittenPretty, output.Contains("\n"));
        }
    }
}
