using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class CollectionGeneralized : Base
    {
        // SortedDictionary tests

        public class SortedDictionaryDec : Dec.Dec
        {
            public SortedDictionary<string, int> data;
        }

        [Test]
        public void SortedDictionaryBasic([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedDictionaryDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedDictionaryDec decName=""TestDec"">
                        <data>
                            <hello>1</hello>
                            <goodbye>2</goodbye>
                        </data>
                    </SortedDictionaryDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedDictionaryDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.data["hello"]);
            Assert.AreEqual(2, result.data["goodbye"]);
            Assert.AreEqual(2, result.data.Count);
            Assert.IsInstanceOf<SortedDictionary<string, int>>(result.data);
        }

        [Test]
        public void SortedDictionaryLi([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedDictionaryDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedDictionaryDec decName=""TestDec"">
                        <data>
                            <li>
                                <key>alpha</key>
                                <value>10</value>
                            </li>
                            <li>
                                <key>beta</key>
                                <value>20</value>
                            </li>
                        </data>
                    </SortedDictionaryDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedDictionaryDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(10, result.data["alpha"]);
            Assert.AreEqual(20, result.data["beta"]);
        }

        // SortedList tests

        public class SortedListDec : Dec.Dec
        {
            public SortedList<string, int> data;
        }

        [Test]
        public void SortedListBasic([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedListDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedListDec decName=""TestDec"">
                        <data>
                            <hello>1</hello>
                            <goodbye>2</goodbye>
                        </data>
                    </SortedListDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedListDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.data["hello"]);
            Assert.AreEqual(2, result.data["goodbye"]);
            Assert.AreEqual(2, result.data.Count);
            Assert.IsInstanceOf<SortedList<string, int>>(result.data);
        }

        // SortedSet tests

        public class SortedSetDec : Dec.Dec
        {
            public SortedSet<string> data;
        }

        [Test]
        public void SortedSetBasic([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedSetDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedSetDec decName=""TestDec"">
                        <data>
                            <Hello />
                            <li>Goodbye</li>
                        </data>
                    </SortedSetDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedSetDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new SortedSet<string> { "Hello", "Goodbye" }, result.data);
            Assert.IsInstanceOf<SortedSet<string>>(result.data);
        }

        public class SortedSetIntDec : Dec.Dec
        {
            public SortedSet<int> data;
        }

        [Test]
        public void SortedSetInt([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedSetIntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedSetIntDec decName=""TestDec"">
                        <data>
                            <li>5</li>
                            <li>3</li>
                            <li>1</li>
                            <li>4</li>
                            <li>2</li>
                        </data>
                    </SortedSetIntDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedSetIntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new SortedSet<int> { 1, 2, 3, 4, 5 }, result.data);
        }

        // Override tests - deserializing over pre-existing instances

        public class SortedDictionaryOverrideDec : Dec.Dec
        {
            public SortedDictionary<string, int> dataA = new SortedDictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
            public SortedDictionary<string, int> dataB = new SortedDictionary<string, int> { ["d"] = 4, ["e"] = 5, ["f"] = 6 };
            public SortedDictionary<string, int> dataC = new SortedDictionary<string, int> { ["g"] = 7, ["h"] = 8, ["i"] = 9 };
        }

        [Test]
        public void SortedDictionaryOverride([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedDictionaryOverrideDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedDictionaryOverrideDec decName=""TestDec"">
                        <dataA>
                            <x>10</x>
                        </dataA>
                        <dataB />
                    </SortedDictionaryOverrideDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedDictionaryOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new SortedDictionary<string, int> { ["x"] = 10 }, result.dataA);
            Assert.AreEqual(new SortedDictionary<string, int>(), result.dataB);
            Assert.AreEqual(new SortedDictionary<string, int> { ["g"] = 7, ["h"] = 8, ["i"] = 9 }, result.dataC);
        }

        public class SortedSetOverrideDec : Dec.Dec
        {
            public SortedSet<string> dataA = new SortedSet<string> { "a", "b", "c" };
            public SortedSet<string> dataB = new SortedSet<string> { "d", "e", "f" };
            public SortedSet<string> dataC = new SortedSet<string> { "g", "h", "i" };
        }

        [Test]
        public void SortedSetOverride([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedSetOverrideDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedSetOverrideDec decName=""TestDec"">
                        <dataA>
                            <x />
                        </dataA>
                        <dataB />
                    </SortedSetOverrideDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedSetOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new SortedSet<string> { "x" }, result.dataA);
            Assert.AreEqual(new SortedSet<string>(), result.dataB);
            Assert.AreEqual(new SortedSet<string> { "g", "h", "i" }, result.dataC);
        }

        public class SortedListOverrideDec : Dec.Dec
        {
            public SortedList<string, int> dataA = new SortedList<string, int> { ["a"] = 1, ["b"] = 2 };
            public SortedList<string, int> dataB = new SortedList<string, int> { ["c"] = 3, ["d"] = 4 };
            public SortedList<string, int> dataC = new SortedList<string, int> { ["e"] = 5, ["f"] = 6 };
        }

        [Test]
        public void SortedListOverride([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SortedListOverrideDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SortedListOverrideDec decName=""TestDec"">
                        <dataA>
                            <x>10</x>
                        </dataA>
                        <dataB />
                    </SortedListOverrideDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SortedListOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new SortedList<string, int> { ["x"] = 10 }, result.dataA);
            Assert.AreEqual(new SortedList<string, int>(), result.dataB);
            Assert.AreEqual(new SortedList<string, int> { ["e"] = 5, ["f"] = 6 }, result.dataC);
        }
    }
}
