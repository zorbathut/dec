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
            var parser = new Def.Parser();
            parser.ParseFromString(@"
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
                </Defs>",
                new Type[]{ typeof(ArrayDef) });

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
            var parser = new Def.Parser();
            parser.ParseFromString(@"
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
                </Defs>",
                new Type[]{ typeof(ListDef) });

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
            var parser = new Def.Parser();
            parser.ParseFromString(@"
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
                </Defs>",
                new Type[]{ typeof(NestedDef) });

            var result = Def.Database<NestedDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { new[] { 8, 16 }, new[] { 9, 81 } });
	    }
    }
}
