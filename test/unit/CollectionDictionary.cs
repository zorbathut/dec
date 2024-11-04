using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class CollectionDictionary : Base
    {
        public class DictionaryStringDec : Dec.Dec
        {
            public Dictionary<string, string> data;
        }

        [Test]
        public void String([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DictionaryStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <hello>goodbye</hello>
                            <Nothing/>
                        </data>
                    </DictionaryStringDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "Nothing", "" } });
        }

        [Test]
        public void Li([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <li>
                                <key>hello</key>
                                <value>goodbye</value>
                            </li>
                            <li>
                                <key>Nothing</key>
                                <value></value>
                            </li>
                        </data>
                    </DictionaryStringDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "Nothing", "" } });
        }

        [Test]
        public void Hybrid([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <hello>goodbye</hello>
                            <li>
                                <key>one</key>
                                <value>two</value>
                            </li>
                        </data>
                    </DictionaryStringDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "one", "two" } });
        }

        [Test]
        public void DuplicateTag([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DictionaryStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <dupe>5</dupe>
                            <dupe>10</dupe>
                        </data>
                    </DictionaryStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "dupe", "10" } });
        }

        public class DictionaryStringOverrideDec : Dec.Dec
        {
            public Dictionary<string, string> dataA = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" };
            public Dictionary<string, string> dataB = new Dictionary<string, string> { ["d"] = "4", ["e"] = "5", ["f"] = "6" };
            public Dictionary<string, string> dataC = new Dictionary<string, string> { ["g"] = "7", ["h"] = "8", ["i"] = "9" };
        }

        [Test]
        public void OverrideString([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringOverrideDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictionaryStringOverrideDec decName=""TestDec"">
                        <dataA>
                            <u>2020</u>
                            <v>2021</v>
                        </dataA>
                        <dataB />
                    </DictionaryStringOverrideDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new Dictionary<string, string> { { "u", "2020" }, { "v", "2021" } });
            Assert.AreEqual(result.dataB, new Dictionary<string, string> { });
            Assert.AreEqual(result.dataC, new Dictionary<string, string> { ["g"] = "7", ["h"] = "8", ["i"] = "9" });
        }

        [Test]
        public void NullKey([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <li>
                                <key null=""true"" />
                                <value>goodbye</value>
                            </li>
                        </data>
                    </DictionaryStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.data.Count);
        }

        public class DictionaryEnumDec : Dec.Dec
        {
            public Dictionary<GenericEnum, string> data;
        }

        [Test]
        public void EnumKey([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryEnumDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictionaryEnumDec decName=""TestDec"">
                        <data>
                            <Alpha>Alpha</Alpha>
                            <Beta>Bravo</Beta>
                            <Gamma>Charlie</Gamma>
                            <Delta>Delta</Delta>
                        </data>
                    </DictionaryEnumDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<DictionaryEnumDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<GenericEnum, string> {
                { GenericEnum.Alpha, "Alpha" },
                { GenericEnum.Beta, "Bravo" },
                { GenericEnum.Gamma, "Charlie" },
                { GenericEnum.Delta, "Delta" },
            });
        }

        public class DictionaryDecDec : Dec.Dec
        {
            public Dictionary<StubDec, string> data;
        }

        [Test]
        public void DecKey([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryDecDec), typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""Alpha"" />
                    <StubDec decName=""Beta"" />
                    <StubDec decName=""Gamma"" />
                    <StubDec decName=""Delta"" />

                    <DictionaryDecDec decName=""TestDec"">
                        <data>
                            <Alpha>Alpha</Alpha>
                            <Beta>Bravo</Beta>
                            <Gamma>Charlie</Gamma>
                            <Delta>Delta</Delta>
                        </data>
                    </DictionaryDecDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<DictionaryDecDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<StubDec, string> {
                { Dec.Database<StubDec>.Get("Alpha"), "Alpha" },
                { Dec.Database<StubDec>.Get("Beta"), "Bravo" },
                { Dec.Database<StubDec>.Get("Gamma"), "Charlie" },
                { Dec.Database<StubDec>.Get("Delta"), "Delta" },
            });
        }

        [Test]
        public void CaseProblem([Values] ParserMode mode, [Values] bool badKey, [Values] bool badValue)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec), typeof(StubDec) } });

            string key = badKey ? "Key" : "key";
            string value = badValue ? "Value" : "value";

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <li>
                                <{key}>Key</{key}>
                                <{value}>Value</{value}>
                            </li>
                        </data>
                    </DictionaryStringDec>
                </Decs>");

            if (badKey || badValue)
            {
                ExpectErrors(() => parser.Finish());
            }
            else
            {
                parser.Finish();
            }

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> {
                { "Key", "Value" },
            });
        }

        public enum MissingItemsSettings
        {
            Key,
            Value,
            Both,
        }
        [Test]
        public void MissingLiItems([Values] ParserMode mode, [Values] MissingItemsSettings mis)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } });

            string midpoint;
            if (mis == MissingItemsSettings.Key)
            {
                midpoint = "<li><value>Value</value></li>";
            }
            else if (mis == MissingItemsSettings.Value)
            {
                midpoint = "<li><key>Key</key></li>";
            }
            else if (mis == MissingItemsSettings.Both)
            {
                midpoint = "<li></li>";
            }
            else
            {
                throw new System.ArgumentException();
            }

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <li>
                                <key>Prefix</key>
                                <value>Prefix</value>
                            </li>
                            {midpoint}
                            <li>
                                <key>Postfix</key>
                                <value>Postfix</value>
                            </li>
                        </data>
                    </DictionaryStringDec>
                </Decs>");

            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> {
                { "Prefix", "Prefix" },
                { "Postfix", "Postfix" },
            });
        }

        [Test]
        public void DuplicateLi([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <li>
                                <key>Alpha</key>
                                <value>Alpha</value>
                            </li>
                            <li>
                                <key>Beta</key>
                                <value>Beta</value>
                            </li>
                            <li>
                                <key>Beta</key>
                                <value>Gamma</value>
                            </li>
                        </data>
                    </DictionaryStringDec>
                </Decs>");

            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> {
                { "Alpha", "Alpha" },
                { "Beta", "Gamma" },
            });
        }
    }
}
