using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DecTest
{
    [TestFixture]
    public class CollectionHashSet : Base
    {
        public class HashSetStringDec : Dec.Dec
        {
            public HashSet<string> data;
        }

        [Test]
        public void String([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <Hello />
                            <li>Goodbye</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Hello", "Goodbye" });
        }

        [Test]
        public void InvalidDataElement([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <Hello />
                            <Catastrophe><elem></elem></Catastrophe>
                            <li>Goodbye</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Hello", "Catastrophe", "Goodbye" });
        }

        [Test]
        public void InvalidDataText([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <Hello />
                            <Catastrophe>no</Catastrophe>
                            <li>Goodbye</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Hello", "Catastrophe", "Goodbye" });
        }

        [Test]
        public void DuplicateLi([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <li>Prefix</li>
                            <li>Dupe</li>
                            <li>Dupe</li>
                            <li>Suffix</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Prefix", "Dupe", "Suffix" });
        }

        [Test]
        public void DuplicateTag([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <Prefix />
                            <Dupe />
                            <Dupe />
                            <Suffix />
                        </data>
                    </HashSetStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Prefix", "Dupe", "Suffix" });
        }

        public class HashSetStringOverrideDec : Dec.Dec
        {
            public HashSet<string> dataA = new HashSet<string> { "a", "b", "c" };
            public HashSet<string> dataB = new HashSet<string> { "d", "e", "f" };
            public HashSet<string> dataC = new HashSet<string> { "g", "h", "i" };
        }

        [Test]
        public void OverrideString([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringOverrideDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetStringOverrideDec decName=""TestDec"">
                        <dataA>
                            <u />
                        </dataA>
                        <dataB />
                    </HashSetStringOverrideDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<HashSetStringOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new HashSet<string> { "u" });
            Assert.AreEqual(result.dataB, new HashSet<string> { });
            Assert.AreEqual(result.dataC, new HashSet<string> { "g", "h", "i" });
        }

        [Test]
        public void EmptyString([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <li></li>
                            <li>four</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "", "four" });
        }

        public class HashSetEnumDec : Dec.Dec
        {
            public HashSet<GenericEnum> data;
        }

        [Test]
        public void EnumKey([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetEnumDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HashSetEnumDec decName=""TestDec"">
                        <data>
                            <Alpha />
                            <Beta />
                            <Gamma />
                            <Delta />
                        </data>
                    </HashSetEnumDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<HashSetEnumDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<GenericEnum> {
                GenericEnum.Alpha,
                GenericEnum.Beta,
                GenericEnum.Gamma,
                GenericEnum.Delta,
            });
        }

        public class HashSetDecDec : Dec.Dec
        {
            public HashSet<StubDec> data;
        }

        [Test]
        public void DecKey([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetDecDec), typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""Alpha"" />
                    <StubDec decName=""Beta"" />
                    <StubDec decName=""Gamma"" />
                    <StubDec decName=""Delta"" />

                    <HashSetDecDec decName=""TestDec"">
                        <data>
                            <Alpha />
                            <Beta />
                            <Gamma />
                            <Delta />
                        </data>
                    </HashSetDecDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<HashSetDecDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<StubDec> {
                Dec.Database<StubDec>.Get("Alpha"),
                Dec.Database<StubDec>.Get("Beta"),
                Dec.Database<StubDec>.Get("Gamma"),
                Dec.Database<StubDec>.Get("Delta"),
            });
        }
    }
}
