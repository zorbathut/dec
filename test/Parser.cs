namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Parser : Base
    {
        [Test]
        public void ConflictingParsers()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();
            ExpectErrors(() => new Def.Parser());
        }

        [Test]
        public void LateAddition()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parser = new Def.Parser();
            parser.Finish();

            ExpectErrors(() => parser.AddString("<Defs />"));
        }

        [Test]
        public void MultiFinish()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parser = new Def.Parser();
            parser.Finish();

            ExpectErrors(() => parser.Finish());
        }

        [Test]
        public void PostFinish()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();
            parserA.Finish();

            ExpectErrors(() => new Def.Parser());
        }

        [Test]
        public void PostClear()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();
            parserA.Finish();

            Def.Database.Clear();

            var parserB = new Def.Parser();
            parserB.Finish();
        }

        [Test]
        public void PartialClear()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();

            ExpectErrors(() => Def.Database.Clear());
        }

        public class IntDef : Def.Def
        {
            public int value = 42;

            [NonSerialized]
            public int nonSerializedValue = 70;
        }

        [Test]
        public void NonSerializablePositive([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>55</value>
                    </IntDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(55, Def.Database<IntDef>.Get("TestDef").value);
            Assert.AreEqual(70, Def.Database<IntDef>.Get("TestDef").nonSerializedValue);
        }

        [Test]
        public void NonSerializableNegative([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>60</value>
                        <nonSerializedValue>65</nonSerializedValue>
                    </IntDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            Assert.AreEqual(60, Def.Database<IntDef>.Get("TestDef").value);
            Assert.AreEqual(70, Def.Database<IntDef>.Get("TestDef").nonSerializedValue);
        }

        [Def.AbstractAttribute]
        public abstract class AbstractRootDef : Def.Def
        {
            public int absInt = 0;
        }

        public class ConcreteChildADef : AbstractRootDef
        {
            public int ccaInt = 0;
        }

        public class ConcreteChildBDef : AbstractRootDef
        {
            public int ccbInt = 0;
        }

        [Test]
        public void AbstractRoot([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConcreteChildADef), typeof(ConcreteChildBDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <ConcreteChildADef defName=""TestDef"">
                        <absInt>20</absInt>
                        <ccaInt>30</ccaInt>
                    </ConcreteChildADef>
                    <ConcreteChildBDef defName=""TestDef"">
                        <absInt>40</absInt>
                        <ccbInt>50</ccbInt>
                    </ConcreteChildBDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(20, Def.Database<ConcreteChildADef>.Get("TestDef").absInt);
            Assert.AreEqual(30, Def.Database<ConcreteChildADef>.Get("TestDef").ccaInt);
            Assert.AreEqual(40, Def.Database<ConcreteChildBDef>.Get("TestDef").absInt);
            Assert.AreEqual(50, Def.Database<ConcreteChildBDef>.Get("TestDef").ccbInt);
        }

        [Test]
        public void LoadFile([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddFile("data/Parser.LoadFile.xml");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(55, Def.Database<IntDef>.Get("TestDef").value);
        }

        [Test]
        public void LoadFileError([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddFile("data/Parser.LoadFileError.xml");
            ExpectErrors(() => parser.Finish(), str => str.Contains("Parser.LoadFileError"));

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<IntDef>.Get("TestDef"));
        }
    }
}
