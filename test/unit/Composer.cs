using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Composer : Base
    {
        // A lot of the Writer functionality is tested via ParserMode.Rewritten in other tests, so these tests mostly handle the Create/Delete/Rename functions.

        public class SomeDecsDec : Dec.Dec
        {
            public SomeValuesDec values;
            public SomeDecsDec decs;
        }

        public class SomeValuesDec : Dec.Dec
        {
            public int number;
        }

        [Test]
        public void Creation([Values] ParserMode mode)
        {
            Dec.Database.Create<SomeValuesDec>("Hello").number = 10;
            Dec.Database.Create<SomeValuesDec>("Goodbye").number = 42;

            DoParserTests(mode);

            Assert.AreEqual(10, Dec.Database<SomeValuesDec>.Get("Hello").number);
            Assert.AreEqual(42, Dec.Database<SomeValuesDec>.Get("Goodbye").number);
        }

        [Test]
        public void CreationNonGeneric([Values] ParserMode mode)
        {
            (Dec.Database.Create(typeof(SomeValuesDec), "Hello") as SomeValuesDec).number = 10;
            (Dec.Database.Create(typeof(SomeValuesDec), "Goodbye") as SomeValuesDec).number = 42;

            DoParserTests(mode);

            Assert.AreEqual(10, Dec.Database<SomeValuesDec>.Get("Hello").number);
            Assert.AreEqual(42, Dec.Database<SomeValuesDec>.Get("Goodbye").number);
        }

        private class NotADec { }

        [Test]
        public void CreationNonGenericNonDec([Values] ParserMode mode)
        {
            ExpectErrors(() => Dec.Database.Create(typeof(NotADec), "NotADec"));
        }

        [Test]
        public void MultiCreation([Values] ParserMode mode)
        {
            Dec.Database.Create<SomeDecsDec>("Decs");
            Dec.Database.Create<SomeValuesDec>("Values");

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<SomeDecsDec>.Get("Decs"));
            Assert.IsNull(Dec.Database<SomeValuesDec>.Get("Decs"));

            Assert.IsNull(Dec.Database<SomeDecsDec>.Get("Values"));
            Assert.IsNotNull(Dec.Database<SomeValuesDec>.Get("Values"));
        }

        [Test]
        public void FailedCreation()
        {
            Dec.Database.Create<SomeDecsDec>("Dec");
            ExpectErrors(() => Dec.Database.Create<SomeDecsDec>("Dec"));
            Dec.Database.Delete(Dec.Database<SomeDecsDec>.Get("Dec"));
            Dec.Database.Create<SomeDecsDec>("Dec");
            ExpectErrors(() => Dec.Database.Create<SomeDecsDec>("Dec"));
            ExpectErrors(() => Dec.Database.Create<SomeDecsDec>("Dec"));
        }

        private class RootDec : Dec.Dec { }
        private class LeafADec : RootDec { }
        private class LeafBDec : RootDec { }

        [Test]
        public void FailedForkCreation()
        {
            Dec.Database.Create<LeafADec>("Dec");
            ExpectErrors(() => Dec.Database.Create<LeafBDec>("Dec"));
            Dec.Database.Delete(Dec.Database<RootDec>.Get("Dec"));
            Dec.Database.Create<RootDec>("Dec");
            ExpectErrors(() => Dec.Database.Create<LeafADec>("Dec"));
            ExpectErrors(() => Dec.Database.Create<LeafBDec>("Dec"));
        }

        [Test]
        public void Databases()
        {
            var selfRef = Dec.Database.Create<SomeDecsDec>("SelfRef");
            var otherRef = Dec.Database.Create<SomeDecsDec>("OtherRef");
            var values = Dec.Database.Create<SomeValuesDec>("Values");

            Assert.AreSame(selfRef, Dec.Database.Get(typeof(SomeDecsDec), "SelfRef"));
            Assert.AreSame(otherRef, Dec.Database.Get(typeof(SomeDecsDec), "OtherRef"));
            Assert.AreSame(values, Dec.Database.Get(typeof(SomeValuesDec), "Values"));

            Assert.AreSame(selfRef, Dec.Database<SomeDecsDec>.Get("SelfRef"));
            Assert.AreSame(otherRef, Dec.Database<SomeDecsDec>.Get("OtherRef"));
            Assert.AreSame(values, Dec.Database<SomeValuesDec>.Get("Values"));
        }

        [Test]
        public void References([Values] ParserMode mode)
        {
            var selfRef = Dec.Database.Create<SomeDecsDec>("SelfRef");
            var otherRef = Dec.Database.Create<SomeDecsDec>("OtherRef");
            var values = Dec.Database.Create<SomeValuesDec>("Values");

            selfRef.decs = selfRef;
            selfRef.values = values;
            otherRef.decs = selfRef;
            otherRef.values = values;

            DoParserTests(mode);

            Assert.AreSame(Dec.Database<SomeDecsDec>.Get("SelfRef"), Dec.Database<SomeDecsDec>.Get("SelfRef").decs);
            Assert.AreSame(Dec.Database<SomeValuesDec>.Get("Values"), Dec.Database<SomeDecsDec>.Get("SelfRef").values);
            Assert.AreSame(Dec.Database<SomeDecsDec>.Get("SelfRef"), Dec.Database<SomeDecsDec>.Get("OtherRef").decs);
            Assert.AreSame(Dec.Database<SomeValuesDec>.Get("Values"), Dec.Database<SomeDecsDec>.Get("OtherRef").values);
        }

        public class IntDec : Dec.Dec
        {
            public int value = 4;
        }

        [Test]
        public void Delete([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""One""><value>1</value></IntDec>
                    <IntDec decName=""Two""><value>2</value></IntDec>
                    <IntDec decName=""Three""><value>3</value></IntDec>
                </Decs>");
            parser.Finish();

            Dec.Database.Delete(Dec.Database<IntDec>.Get("Two"));

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Database<IntDec>.Get("One").value);
            Assert.IsNull(Dec.Database<IntDec>.Get("Two"));
            Assert.AreEqual(3, Dec.Database<IntDec>.Get("Three").value);
        }

        [Test]
        public void DoubleDelete([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""One""><value>1</value></IntDec>
                    <IntDec decName=""Two""><value>2</value></IntDec>
                    <IntDec decName=""Three""><value>3</value></IntDec>
                </Decs>");
            parser.Finish();

            var one = Dec.Database<IntDec>.Get("One");
            Dec.Database.Delete(one);
            ExpectErrors(() => Dec.Database.Delete(one));
            Dec.Database.Delete(Dec.Database<IntDec>.Get("Three"));

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<IntDec>.Get("One"));
            Assert.AreEqual(2, Dec.Database<IntDec>.Get("Two").value);
            Assert.IsNull(Dec.Database<IntDec>.Get("Three"));
        }

        [Test]
        public void CreateDeleteHierarchy()
        {
            var a = Dec.Database.Create<LeafADec>("A");

            Assert.AreSame(a, Dec.Database<RootDec>.Get("A"));
            Assert.AreSame(a, Dec.Database<LeafADec>.Get("A"));
            Assert.AreSame(a, Dec.Database.Get(typeof(RootDec), "A"));
            Assert.AreSame(a, Dec.Database.Get(typeof(LeafADec), "A"));

            Dec.Database.Delete(a);

            Assert.IsNull(Dec.Database<RootDec>.Get("A"));
            Assert.IsNull(Dec.Database<LeafADec>.Get("A"));
            Assert.IsNull(Dec.Database.Get(typeof(RootDec), "A"));
            Assert.IsNull(Dec.Database.Get(typeof(LeafADec), "A"));
        }

        [Test]
        public void Rename([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""One""><value>1</value></IntDec>
                    <IntDec decName=""Two""><value>2</value></IntDec>
                    <IntDec decName=""Three""><value>3</value></IntDec>
                </Decs>");
            parser.Finish();

            Dec.Database.Rename(Dec.Database<IntDec>.Get("One"), "OneBeta");
            Dec.Database.Rename(Dec.Database<IntDec>.Get("OneBeta"), "OneGamma");

            // yes okay this is confusing
            Dec.Database.Rename(Dec.Database<IntDec>.Get("Two"), "One");

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Database<IntDec>.Get("OneGamma").value);
            Assert.AreEqual(2, Dec.Database<IntDec>.Get("One").value);
            Assert.AreEqual(3, Dec.Database<IntDec>.Get("Three").value);
        }

        [Test]
        public void RenameError([Values] ParserMode mode)
        {
            var a = Dec.Database.Create<StubDec>("A");
            var b = Dec.Database.Create<StubDec>("B");
            var c = Dec.Database.Create<StubDec>("C");

            ExpectErrors(() => Dec.Database.Rename(a, "B"));
            Dec.Database.Rename(c, "C");

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("A"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("B"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("C"));
        }

        [Test]
        public void RenameDeleted([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""One""><value>1</value></IntDec>
                    <IntDec decName=""Two""><value>2</value></IntDec>
                    <IntDec decName=""Three""><value>3</value></IntDec>
                </Decs>");
            parser.Finish();

            var three = Dec.Database<IntDec>.Get("Three");
            Dec.Database.Delete(three);
            ExpectErrors(() => Dec.Database.Rename(three, "ThreePhoenix"));

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Database<IntDec>.Get("One").value);
            Assert.AreEqual(2, Dec.Database<IntDec>.Get("Two").value);
            Assert.IsNull(Dec.Database<IntDec>.Get("Three"));
            Assert.IsNull(Dec.Database<IntDec>.Get("ThreePhoenix"));
        }

        [Test]
        public void ReferenceDeleted([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var ephemeral = Dec.Database.Create<SomeDecsDec>("Ephemeral");
            var stored = Dec.Database.Create<SomeDecsDec>("Stored");

            stored.decs = ephemeral;

            Dec.Database.Delete(ephemeral);

            DoParserTests(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true, validation_expectWriteErrors: true);

            if (mode != ParserMode.Bare)
            {
                Assert.IsNull(Dec.Database<SomeDecsDec>.Get("Stored").decs);
                Assert.IsNull(Dec.Database<SomeDecsDec>.Get("Ephemeral"));
            }
        }

        [Test]
        public void ReferenceReplaced([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var ephemeral = Dec.Database.Create<SomeDecsDec>("Ephemeral");
            var stored = Dec.Database.Create<SomeDecsDec>("Stored");

            stored.decs = ephemeral;

            Dec.Database.Delete(ephemeral);

            Dec.Database.Create<SomeDecsDec>("Ephemeral");

            DoParserTests(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true, validation_expectWriteErrors: true);

            if (mode != ParserMode.Bare)
            {
                Assert.IsNull(Dec.Database<SomeDecsDec>.Get("Stored").decs);
                Assert.IsNotNull(Dec.Database<SomeDecsDec>.Get("Ephemeral"));
            }
        }

        public class NonSerializedDec : Dec.Dec
        {
            public int serializedValue = 30;

            [NonSerialized]
            public int nonSerializedValue = 40;
        }

        [Test]
        public void NonSerialized([Values(ParserMode.RewrittenBare, ParserMode.RewrittenPretty)] ParserMode mode)
        {
            var ephemeral = Dec.Database.Create<NonSerializedDec>("TestDec");
            ephemeral.serializedValue = 35;
            ephemeral.nonSerializedValue = 45;

            var writer = new Dec.Composer();
            string data = writer.ComposeXml(mode == ParserMode.RewrittenPretty);

            Assert.IsTrue(data.Contains("serializedValue"));
            Assert.IsFalse(data.Contains("nonSerializedValue"));
        }

        public class EnumContainerDec : Dec.Dec
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
        public void Enum([Values(ParserMode.RewrittenBare, ParserMode.RewrittenPretty)] ParserMode mode)
        {
            var enums = Dec.Database.Create<EnumContainerDec>("TestDec");
            enums.alph = EnumContainerDec.Enum.Alpha;
            enums.bet = EnumContainerDec.Enum.Beta;
            enums.gam = EnumContainerDec.Enum.Gamma;

            var writer = new Dec.Composer();
            string data = writer.ComposeXml(mode == ParserMode.RewrittenPretty);

            Assert.IsTrue(data.Contains("Alpha"));
            Assert.IsTrue(data.Contains("Beta"));
            Assert.IsTrue(data.Contains("Gamma"));

            Assert.IsFalse(data.Contains("__value"));
        }

        [Test]
        public void Pretty([Values(ParserMode.RewrittenBare, ParserMode.RewrittenPretty)] ParserMode mode)
        {
            Dec.Database.Create<StubDec>("Hello");

            var output = new Dec.Composer().ComposeXml(mode == ParserMode.RewrittenPretty);

            Assert.AreEqual(mode == ParserMode.RewrittenPretty, output.Contains("\n"));
        }
    }
}
