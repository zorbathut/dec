namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class Collection : Base
    {
        public class ArrayDef : Def.Def
        {
            public int[] data;
        }

        [Test]
	    public void Array([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[]{ typeof(ArrayDef) } });
            parser.AddString(@"
                <Defs>
                    <ArrayDef defName=""TestDef"">
                        <data>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </data>
                    </ArrayDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<ArrayDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { 10, 9, 8, 7, 6 });
	    }

        public class ListDef : Def.Def
        {
            public List<int> data;
        }

        [Test]
	    public void List([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[]{ typeof(ListDef) } });
            parser.AddString(@"
                <Defs>
                    <ListDef defName=""TestDef"">
                        <data>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </data>
                    </ListDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<ListDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { 10, 9, 8, 7, 6 });
	    }

        public class ListOverrideDef : Def.Def
        {
            public List<int> dataA = new List<int> { 3, 4, 5 };
            public List<int> dataB = new List<int> { 6, 7, 8 };
            public List<int> dataC = new List<int> { 9, 10, 11 };
        }

        [Test]
        public void ListOverride([Values] BehaviorMode mode)
        {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[] { typeof(ListOverrideDef) } });
            parser.AddString(@"
                <Defs>
                    <ListOverrideDef defName=""TestDef"">
                        <dataA>
                            <li>2020</li>
                        </dataA>
                        <dataB />
                    </ListOverrideDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<ListOverrideDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new[] { 2020 });
            Assert.AreEqual(result.dataB, new int[0] );
            Assert.AreEqual(result.dataC, new[] { 9, 10, 11 });
        }

        public class NestedDef : Def.Def
        {
            public int[][] data;
        }

        [Test]
	    public void Nested([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[]{ typeof(NestedDef) } });
            parser.AddString(@"
                <Defs>
                    <NestedDef defName=""TestDef"">
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
                    </NestedDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<NestedDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { new[] { 8, 16 }, new[] { 9, 81 } });
	    }

        public class DictionaryStringDef : Def.Def
        {
            public Dictionary<string, string> data;
        }

        [Test]
	    public void DictionaryString([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[]{ typeof(DictionaryStringDef) } });
            parser.AddString(@"
                <Defs>
                    <DictionaryStringDef defName=""TestDef"">
                        <data>
                            <hello>goodbye</hello>
                            <Nothing/>
                        </data>
                    </DictionaryStringDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<DictionaryStringDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "Nothing", "" } });
	    }

        [Test]
        public void DictionaryLi([Values] BehaviorMode mode)
        {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDef) } });
            parser.AddString(@"
                <Defs>
                    <DictionaryStringDef defName=""TestDef"">
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
                    </DictionaryStringDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<DictionaryStringDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "Nothing", "" } });
        }

        [Test]
        public void DictionaryHybrid([Values] BehaviorMode mode)
        {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringDef) } });
            parser.AddString(@"
                <Defs>
                    <DictionaryStringDef defName=""TestDef"">
                        <data>
                            <hello>goodbye</hello>
                            <li>
                                <key>one</key>
                                <value>two</value>
                            </li>
                        </data>
                    </DictionaryStringDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<DictionaryStringDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "one", "two" } });
        }

        [Test]
	    public void DictionaryDuplicate([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[]{ typeof(DictionaryStringDef) } });
            parser.AddString(@"
                <Defs>
                    <DictionaryStringDef defName=""TestDef"">
                        <data>
                            <dupe>5</dupe>
                            <dupe>10</dupe>
                        </data>
                    </DictionaryStringDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Def.Database<DictionaryStringDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "dupe", "10" } });
	    }

        public class DictionaryStringOverrideDef : Def.Def
        {
            public Dictionary<string, string> dataA = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2", ["c"] = "3" };
            public Dictionary<string, string> dataB = new Dictionary<string, string> { ["d"] = "4", ["e"] = "5", ["f"] = "6" };
            public Dictionary<string, string> dataC = new Dictionary<string, string> { ["g"] = "7", ["h"] = "8", ["i"] = "9" };
        }

        [Test]
        public void DictionaryOverrideString([Values] BehaviorMode mode)
        {
            var parser = CreateParserForBehavior(new Def.Parser.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryStringOverrideDef) } });
            parser.AddString(@"
                <Defs>
                    <DictionaryStringOverrideDef defName=""TestDef"">
                        <dataA>
                            <u>2020</u>
                            <v>2021</v>
                        </dataA>
                        <dataB />
                    </DictionaryStringOverrideDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<DictionaryStringOverrideDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new Dictionary<string, string> { { "u", "2020" }, { "v", "2021" } });
            Assert.AreEqual(result.dataB, new Dictionary<string, string> { });
            Assert.AreEqual(result.dataC, new Dictionary<string, string> { ["g"] = "7", ["h"] = "8", ["i"] = "9" });
        }
    }
}
