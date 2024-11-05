
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DecTest
{
    [TestFixture]
    public class Path : Base
    {
        private class PathTester : Dec.IRecordable
        {
            public static int validations = 0;
            public string text;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref text);

                Assert.AreEqual(text, recorder.Context.PathString());
                ++validations;
            }
        }

        private class PathDec : Dec.Dec
        {
            public PathTester member;
            public PathTester[] array;
            public PathTester[,] arrayMulti;
            public List<PathTester> list;
            public Dictionary<PathTester, string> dictKey;
            public Dictionary<string, PathTester> dictValue;
            public HashSet<PathTester> set;
        }

        [SetUp]
        public void Setup()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PathDec) } });
            PathTester.validations = 0;
        }

        [Test]
        public void MemberPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <member>PathDec.TestDec.member</member>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 1);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 1 : 3));
        }

        [Test]
        public void ArrayPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <array>
                            <li>PathDec.TestDec.array[0]</li>
                            <li>PathDec.TestDec.array[1]</li>
                        </array>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 2);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 2 : 6));
        }

        [Test]
        public void MultiDimensionalArrayPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <arrayMulti>
                            <li>
                                <li>PathDec.TestDec.arrayMulti[0,0]</li>
                                <li>PathDec.TestDec.arrayMulti[0,1]</li>
                            </li>
                            <li>
                                <li>PathDec.TestDec.arrayMulti[1,0]</li>
                                <li>PathDec.TestDec.arrayMulti[1,1]</li>
                            </li>
                        </arrayMulti>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 4);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 4 : 12));
        }

        [Test]
        public void ListPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <list>
                            <li>PathDec.TestDec.list[0]</li>
                            <li>PathDec.TestDec.list[1]</li>
                        </list>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 2);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 2 : 6));
        }

        [Test]
        public void DictionaryKeyPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <dictKey>
                            <li>
                                <key>PathDec.TestDec.dictKey[KEY]</key>
                                <value>sure</value>
                            </li>
                        </dictKey>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 1);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 1 : 3));
        }

        [Test]
        public void DictionaryValuePath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <dictValue>
                            <li>
                                <key>first</key>
                                <value>PathDec.TestDec.dictValue[nyi]</value>
                            </li>
                        </dictValue>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 1);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 1 : 3));
        }

        [Test]
        public void HashSetPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <set>
                            <li>PathDec.TestDec.set[KEY]</li>
                        </set>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 1);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 1 : 3));
        }

        // nyi due to needing a write step
        /*
        [Test]
        public void Record()
        {
            var initialData = new PathTester { text = "RECORD" };

            string serialized = Dec.Recorder.Write(initialData);
            var deserialized = Dec.Recorder.Read<PathTester>(serialized);

            Assert.IsNotNull(deserialized);

            Assert.AreEqual(2, PathTester.validations); // Once for write, once for read
        }

        [Test]
        public void RecordRef()
        {
            var initialData = new PathTester { text = "RECORD" };

            string serialized = Dec.Recorder.Write((initialData, initialData));
            var deserialized = Dec.Recorder.Read<(PathTester, PathTester)>(serialized);

            Assert.IsNotNull(deserialized);

            Assert.AreEqual(2, PathTester.validations); // Once for write, once for read
        }

        [Test]
        public void Simple()
        {
            var initialData = new PathTester { text = "SimplePath" };

            string serialized = Dec.Recorder.WriteSimple(initialData, "SimplePath");
            var deserialized = Dec.Recorder.ReadSimple<PathTester>(serialized, "SimplePath");

            Assert.IsNotNull(deserialized);

            Assert.AreEqual(2, PathTester.validations); // Once for write, once for read
        }*/
    }
}