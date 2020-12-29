namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class CollectionDictionary : Base
    {
        public class DictionaryStringDec : Dec.Dec
        {
            public Dictionary<string, string> data;
        }

        [Test]
        public void String([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DictionaryStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <hello>goodbye</hello>
                            <Nothing/>
                        </data>
                    </DictionaryStringDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "Nothing", "" } });
        }

        [Test]
        public void Li([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "Nothing", "" } });
        }

        [Test]
        public void Hybrid([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "one", "two" } });
        }

        [Test]
        public void Duplicate([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DictionaryStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <DictionaryStringDec decName=""TestDec"">
                        <data>
                            <dupe>5</dupe>
                            <dupe>10</dupe>
                        </data>
                    </DictionaryStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

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
        public void OverrideString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringOverrideDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<DictionaryStringOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new Dictionary<string, string> { { "u", "2020" }, { "v", "2021" } });
            Assert.AreEqual(result.dataB, new Dictionary<string, string> { });
            Assert.AreEqual(result.dataC, new Dictionary<string, string> { ["g"] = "7", ["h"] = "8", ["i"] = "9" });
        }

        [Test]
        public void NullKey([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<DictionaryStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.data.Count);
        }
    }
}
