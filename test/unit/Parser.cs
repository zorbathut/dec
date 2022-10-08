namespace DecTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Parser : Base
    {
        [Test]
        public void ConflictingParsers()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters();

            var parserA = new Dec.Parser();
            ExpectErrors(() => new Dec.Parser());
        }

        [Test]
        public void LateAddition()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters();

            var parser = new Dec.Parser();
            parser.Finish();

            ExpectErrors(() => parser.AddString("<Decs />"));
        }

        [Test]
        public void MultiFinish()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters();

            var parser = new Dec.Parser();
            parser.Finish();

            ExpectErrors(() => parser.Finish());
        }

        [Test]
        public void PostFinish()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters();

            var parserA = new Dec.Parser();
            parserA.Finish();

            ExpectErrors(() => new Dec.Parser());
        }

        [Test]
        public void PostClear()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters();

            var parserA = new Dec.Parser();
            parserA.Finish();

            Dec.Database.Clear();

            var parserB = new Dec.Parser();
            parserB.Finish();
        }

        [Test]
        public void PartialClear()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters();

            var parserA = new Dec.Parser();

            ExpectErrors(() => Dec.Database.Clear());
        }

        public class IntDec : Dec.Dec
        {
            public int value = 42;

            [NonSerialized]
            public int nonSerializedValue = 70;
        }

        [Test]
        public void NonSerializablePositive([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>55</value>
                    </IntDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(55, Dec.Database<IntDec>.Get("TestDec").value);
            Assert.AreEqual(70, Dec.Database<IntDec>.Get("TestDec").nonSerializedValue);
        }

        [Test]
        public void NonSerializableNegative([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>60</value>
                        <nonSerializedValue>65</nonSerializedValue>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            Assert.AreEqual(60, Dec.Database<IntDec>.Get("TestDec").value);
            Assert.AreEqual(70, Dec.Database<IntDec>.Get("TestDec").nonSerializedValue);
        }

        [Dec.Abstract]
        public abstract class AbstractRootDec : Dec.Dec
        {
            public int absInt = 0;
        }

        public class ConcreteChildADec : AbstractRootDec
        {
            public int ccaInt = 0;
        }

        public class ConcreteChildBDec : AbstractRootDec
        {
            public int ccbInt = 0;
        }

        [Test]
        public void AbstractRoot([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConcreteChildADec), typeof(ConcreteChildBDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ConcreteChildADec decName=""TestDec"">
                        <absInt>20</absInt>
                        <ccaInt>30</ccaInt>
                    </ConcreteChildADec>
                    <ConcreteChildBDec decName=""TestDec"">
                        <absInt>40</absInt>
                        <ccbInt>50</ccbInt>
                    </ConcreteChildBDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(20, Dec.Database<ConcreteChildADec>.Get("TestDec").absInt);
            Assert.AreEqual(30, Dec.Database<ConcreteChildADec>.Get("TestDec").ccaInt);
            Assert.AreEqual(40, Dec.Database<ConcreteChildBDec>.Get("TestDec").absInt);
            Assert.AreEqual(50, Dec.Database<ConcreteChildBDec>.Get("TestDec").ccbInt);
        }

        [Test]
        public void LoadFile([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddFile("data/Parser.LoadFile.xml");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(55, Dec.Database<IntDec>.Get("TestDec").value);
        }

        [Test]
        public void LoadFileError([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddFile("data/Parser.LoadFileError.xml");
            ExpectErrors(() => parser.Finish(), str => str.Contains("Parser.LoadFileError"));

            DoBehavior(mode);

            Assert.IsNotNull(Dec.Database<IntDec>.Get("TestDec"));
        }

        [Test]
        public void LoadDirectory([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectory");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }

        [Test]
        public void LoadDirectoryRecursive([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectoryRecursive");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }

        [Test]
        public void LoadDirectoryDotIgnore([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectoryRecursive");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }
    }
}
