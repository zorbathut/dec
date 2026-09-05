
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
            public static bool ignore = false;
            public static int validations = 0;
            public string text;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref text);

                if (!PathTester.ignore)
                {
                    Assert.AreEqual(text, recorder.Context.PathString());
                    ++validations;
                }
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
            PathTester.ignore = false;
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
                                <value>PathDec.TestDec.dictValue[UNSERIALIZABLE]</value>
                            </li>
                            <second>PathDec.TestDec.dictValue[second]</second>
                        </dictValue>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 2);

            // the `second` currently does not serialize properly, so we replace the value there for now
            // fix this when we add support!
            Dec.Database<PathDec>.Get("TestDec").dictValue["second"] = new PathTester { text = "PathDec.TestDec.dictValue[UNSERIALIZABLE]" };

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 2 : 6));
        }

        [Test]
        public void HashSetPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <set>
                            <li>PathDec.TestDec.set[SETELEM]</li>
                        </set>
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 1);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 1 : 3));
        }

        [Test]
        public void InheritedPath([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""AbstractDec"" abstract=""true"">
                        <member>PathDec.ConcreteDec.member</member>
                    </PathDec>

                    <PathDec decName=""ConcreteDec"" parent=""AbstractDec"">
                    </PathDec>
                </Decs>");
            parser.Finish();

            Assert.IsTrue(PathTester.validations == 1);

            DoParserTests(mode);

            Assert.IsTrue(PathTester.validations == (mode == ParserMode.Bare ? 1 : 3));
        }

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
            // this actual string may change someday
            var initialData = new PathTester { text = "REF.ref00000" };

            // we can't know that it's a Ref until we've written it out
            PathTester.ignore = true;
            string serialized = Dec.Recorder.Write((initialData, initialData));

            PathTester.ignore = false;
            var deserialized = Dec.Recorder.Read<(PathTester, PathTester)>(serialized);

            Assert.IsNotNull(deserialized);

            Assert.AreEqual(1, PathTester.validations);
        }

        [Test]
        public void Simple()
        {
            var initialData = new PathTester { text = "SimplePath" };

            string serialized = Dec.Recorder.WriteSimple(initialData, "SimplePath");
            var deserialized = Dec.Recorder.ReadSimple<PathTester>(serialized, "SimplePath");

            Assert.IsNotNull(deserialized);

            Assert.AreEqual(2, PathTester.validations); // Once for write, once for read
        }

        [Test]
        public void PathEquality()
        {
            var rootA = new Dec.PathRoot("R");
            var rootA2 = new Dec.PathRoot("R");
            var rootB = new Dec.PathRoot("B");

            Assert.IsTrue(rootA.Equals(rootA2));
            Assert.AreEqual(rootA.GetHashCode(), rootA2.GetHashCode());
            Assert.IsFalse(rootA.Equals(rootB));

            var memberA = new Dec.PathMember(rootA, "field");
            var memberA2 = new Dec.PathMember(rootA2, "field");
            var memberB = new Dec.PathMember(rootA, "other");

            Assert.IsTrue(memberA.Equals(memberA2));
            Assert.AreEqual(memberA.GetHashCode(), memberA2.GetHashCode());
            Assert.IsFalse(memberA.Equals(memberB));

            var index1 = new Dec.PathIndex(memberA, 1);
            var index1b = new Dec.PathIndex(memberA2, 1);
            var index2 = new Dec.PathIndex(memberA, 2);

            Assert.IsTrue(index1.Equals(index1b));
            Assert.AreEqual(index1.GetHashCode(), index1b.GetHashCode());
            Assert.IsFalse(index1.Equals(index2));

            // cross-class
            Assert.IsFalse(memberA.Equals(rootA));
            Assert.IsFalse(index1.Equals(memberA));
        }
    }
}
