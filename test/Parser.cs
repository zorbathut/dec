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
    }
}
