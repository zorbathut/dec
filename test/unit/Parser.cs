using NUnit.Framework;
using System;

namespace DecTest
{
    [TestFixture]
    public class Parser : Base
    {
        [Test]
        public void ConflictingParsers()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters());

            var parserA = new Dec.Parser();
            ExpectErrors(() => new Dec.Parser());
        }

        [Test]
        public void LateAddition()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters());

            var parser = new Dec.Parser();
            parser.Finish();

            ExpectErrors(() => parser.AddString(Dec.Parser.FileType.Xml, "<Decs />"));
        }

        [Test]
        public void MultiFinish()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters());

            var parser = new Dec.Parser();
            parser.Finish();

            ExpectErrors(() => parser.Finish());
        }

        [Test]
        public void PostFinish()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters());

            var parserA = new Dec.Parser();
            parserA.Finish();

            ExpectErrors(() => new Dec.Parser());
        }

        [Test]
        public void PostClear()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters());

            var parserA = new Dec.Parser();
            parserA.Finish();

            Dec.Database.Clear();

            var parserB = new Dec.Parser();
            parserB.Finish();
        }

        [Test]
        public void PartialClear()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters());

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
        public void NonSerializablePositive([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>55</value>
                    </IntDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(55, Dec.Database<IntDec>.Get("TestDec").value);
            Assert.AreEqual(70, Dec.Database<IntDec>.Get("TestDec").nonSerializedValue);
        }

        [Test]
        public void NonSerializableNegative([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>60</value>
                        <nonSerializedValue>65</nonSerializedValue>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

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
        public void AbstractRoot([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConcreteChildADec), typeof(ConcreteChildBDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            DoParserTests(mode);

            Assert.AreEqual(20, Dec.Database<ConcreteChildADec>.Get("TestDec").absInt);
            Assert.AreEqual(30, Dec.Database<ConcreteChildADec>.Get("TestDec").ccaInt);
            Assert.AreEqual(40, Dec.Database<ConcreteChildBDec>.Get("TestDec").absInt);
            Assert.AreEqual(50, Dec.Database<ConcreteChildBDec>.Get("TestDec").ccbInt);
        }

        [Test]
        public void LoadFile([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddFile(Dec.Parser.FileType.Xml, "data/Parser.LoadFile.xml");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(55, Dec.Database<IntDec>.Get("TestDec").value);
        }

        [Test]
        public void LoadFileError([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddFile(Dec.Parser.FileType.Xml, "data/Parser.LoadFileError.xml");
            ExpectErrors(() => parser.Finish(), errorValidator: str => str.Contains("Parser.LoadFileError"));

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<IntDec>.Get("TestDec"));
        }

        [Test]
        public void LoadDirectory([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectory");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }

        [Test]
        public void LoadDirectoryRecursive([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectoryRecursive");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }

        [Test]
        public void LoadDirectoryDotIgnore([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectoryDotIgnore");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }

        [Test]
        public void LoadDirectoryDotIgnore1Dot([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectoryDotIgnore/.");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }

        [Test]
        public void LoadDirectoryDotIgnore2Dot([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddDirectory("data/Parser.LoadDirectoryDotIgnore/../Parser.LoadDirectoryDotIgnore");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(40, Dec.Database<IntDec>.Get("TestDec1").value);
            Assert.AreEqual(80, Dec.Database<IntDec>.Get("TestDec2").value);
        }

        [Test]
        public void NullDirectory([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            ExpectErrors((() => parser.AddDirectory(null)));
            parser.Finish();

            DoParserTests(mode);
        }

        [Test]
        public void EmptyStringDirectory([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            ExpectErrors((() => parser.AddDirectory("")));
            parser.Finish();

            DoParserTests(mode);
        }

        [Test]
        public void NullFilename([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            ExpectErrors((() => parser.AddFile(Dec.Parser.FileType.Xml, null)));
            parser.Finish();

            DoParserTests(mode);
        }

        [Test]
        public void EmptyStringFilename([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            ExpectErrors((() => parser.AddFile(Dec.Parser.FileType.Xml, "")));
            parser.Finish();

            DoParserTests(mode);
        }

        [Test]
        public void NullString([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            ExpectErrors((() => parser.AddString(Dec.Parser.FileType.Xml, null)));
            parser.Finish();

            DoParserTests(mode);
        }

        [Test]
        public void EmptyStringString([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            ExpectErrors((() => parser.AddString(Dec.Parser.FileType.Xml, "")));
            parser.Finish();

            DoParserTests(mode);
        }

        [Test]
        public void NullStream([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            ExpectErrors((() => parser.AddStream(Dec.Parser.FileType.Xml, null)));
            parser.Finish();

            DoParserTests(mode);
        }
    }
}
