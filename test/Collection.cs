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
	    public void Array()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(ArrayDef) }, explicitStaticRefs: new Type[]{ });
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

            var result = Def.Database<ArrayDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { 10, 9, 8, 7, 6 });
	    }

        public class ListDef : Def.Def
        {
            public List<int> data;
        }

        [Test]
	    public void List()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(ListDef) }, explicitStaticRefs: new Type[]{ });
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

            var result = Def.Database<ListDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { 10, 9, 8, 7, 6 });
	    }

        public class NestedDef : Def.Def
        {
            public int[][] data;
        }

        [Test]
	    public void Nested()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(NestedDef) }, explicitStaticRefs: new Type[]{ });
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

            var result = Def.Database<NestedDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { new[] { 8, 16 }, new[] { 9, 81 } });
	    }

        public class DictionaryStringDef : Def.Def
        {
            public Dictionary<string, string> data;
        }

        [Test]
	    public void DictionaryString()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(DictionaryStringDef) }, explicitStaticRefs: new Type[]{ });
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

            var result = Def.Database<DictionaryStringDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "hello", "goodbye" }, { "Nothing", "" } });
	    }

        [Test]
	    public void DictionaryDuplicate()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(DictionaryStringDef) }, explicitStaticRefs: new Type[]{ });
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

            var result = Def.Database<DictionaryStringDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new Dictionary<string, string> { { "dupe", "10" } });
	    }
    }
}
