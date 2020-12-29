namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class Collection : Base
    {
        public class ArrayDec : Dec.Dec
        {
            public int[] dataEmpty = null;
            public int[] dataProvided = new int[] { 10, 20 };
        }

        [Test]
        public void Array([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 7, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 7, 6 });
        }

        [Test]
        public void ArrayAsStringError([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>nope</dataEmpty>
                        <dataProvided>nope</dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            // error should default to existing data
            Assert.IsNull(result.dataEmpty);
            Assert.AreEqual(result.dataProvided, new[] { 10, 20 });
        }

        [Test]
        public void ArrayZero([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty></dataEmpty>
                        <dataProvided></dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new int[] { });
            Assert.AreEqual(result.dataProvided, new int[] { });
        }

        [Test]
        public void ArrayNull([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty null=""true""></dataEmpty>
                        <dataProvided null=""true""></dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.dataEmpty);
            Assert.IsNull(result.dataProvided);
        }

        [Test]
        public void ArrayElementMisparse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>dog</li>
                            <li>6</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>dog</li>
                            <li>6</li>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 0, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 0, 6 });
        }

        public class ListDec : Dec.Dec
        {
            public List<int> data;
        }

        [Test]
        public void List([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ListDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ListDec decName=""TestDec"">
                        <data>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </data>
                    </ListDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ListDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { 10, 9, 8, 7, 6 });
        }

        public class ListOverrideDec : Dec.Dec
        {
            public List<int> dataA = new List<int> { 3, 4, 5 };
            public List<int> dataB = new List<int> { 6, 7, 8 };
            public List<int> dataC = new List<int> { 9, 10, 11 };
        }

        [Test]
        public void ListOverride([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ListOverrideDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ListOverrideDec decName=""TestDec"">
                        <dataA>
                            <li>2020</li>
                        </dataA>
                        <dataB />
                    </ListOverrideDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ListOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new[] { 2020 });
            Assert.AreEqual(result.dataB, new int[0] );
            Assert.AreEqual(result.dataC, new[] { 9, 10, 11 });
        }

        public class NestedDec : Dec.Dec
        {
            public int[][] data;
        }

        [Test]
        public void Nested([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(NestedDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <NestedDec decName=""TestDec"">
                        <data>
                            <li>
                                <li>8</li>
                                <li>16</li>
                            </li>
                            <li>
                                <li>9</li>
                                <li>81</li>
                            </li>
                        </data>
                    </NestedDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<NestedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { new[] { 8, 16 }, new[] { 9, 81 } });
        }

        public class DictionaryStringDec : Dec.Dec
        {
            public Dictionary<string, string> data;
        }

        [Test]
        public void DictionaryString([Values] BehaviorMode mode)
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
        public void DictionaryLi([Values] BehaviorMode mode)
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
        public void DictionaryHybrid([Values] BehaviorMode mode)
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
        public void DictionaryDuplicate([Values] BehaviorMode mode)
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
        public void DictionaryOverrideString([Values] BehaviorMode mode)
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
        public void DictionaryNullKey([Values] BehaviorMode mode)
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

        public class HashSetStringDec : Dec.Dec
        {
            public HashSet<string> data;
        }

        [Test]
        public void HashSetString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <Hello />
                            <li>Goodbye</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Hello", "Goodbye" });
        }

        [Test]
        public void HashSetDuplicate([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

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
        public void HashSetOverrideString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringOverrideDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <HashSetStringOverrideDec decName=""TestDec"">
                        <dataA>
                            <u />
                        </dataA>
                        <dataB />
                    </HashSetStringOverrideDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<HashSetStringOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new HashSet<string> { "u" });
            Assert.AreEqual(result.dataB, new HashSet<string> { });
            Assert.AreEqual(result.dataC, new HashSet<string> { "g", "h", "i" });
        }

        [Test]
        public void HashSetEmptyString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <li></li>
                            <li>four</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "", "four" });
        }
    }
}
