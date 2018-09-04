namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Primitives : Base
    {
        public class IntDef : Def.Def
        {
            public int value = 4;
        }

        public class BoolDef : Def.Def
        {
            public bool value = true;
        }

        public class StringDef : Def.Def
        {
            public string value = "one";
        }

	    [Test]
	    public void EmptyIntParse()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value />
                    </IntDef>
                </Defs>",
                new Type[]{ typeof(IntDef) }));

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.value);
	    }

        [Test]
	    public void FailingIntParse()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>NotAnInt</value>
                    </IntDef>
                </Defs>",
                new Type[]{ typeof(IntDef) }));

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.value);
	    }

        [Test]
	    public void FailingIntParse2()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>10NotAnInt</value>
                    </IntDef>
                </Defs>",
                new Type[]{ typeof(IntDef) }));

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.value);
	    }

	    [Test]
	    public void EmptyBoolParse()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <BoolDef defName=""TestDef"">
                        <value />
                    </BoolDef>
                </Defs>",
                new Type[]{ typeof(BoolDef) }));

            var result = Def.Database<BoolDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(false, result.value);
	    }

	    [Test]
	    public void FailingBoolParse()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <BoolDef defName=""TestDef"">
                        <value>NotABool</value>
                    </BoolDef>
                </Defs>",
                new Type[]{ typeof(BoolDef) }));

            var result = Def.Database<BoolDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(false, result.value);
	    }

        [Test]
	    public void EmptyStringParse()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <StringDef defName=""TestDef"">
                        <value />
                    </StringDef>
                </Defs>",
                new Type[]{ typeof(StringDef) });

            var result = Def.Database<StringDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual("", result.value);
	    }

        public class BulkParseDef : Def.Def
        {
            public int testIntA = 1;
            public int testIntB = 2;
            public int testIntC = 3;
            public float testFloatA = 1;
            public float testFloatB = 2;
            public float testFloatC = 3;
            public string testStringA = "one";
            public string testStringB = "two";
            public string testStringC = "three";
            public string testStringD = "four";
            public bool testBoolA = false;
            public bool testBoolB = false;
            public bool testBoolC = false;
        }

	    [Test]
	    public void BulkParse()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <BulkParseDef defName=""TestDef"">
                        <testIntA>35</testIntA>
                        <testIntB>-20</testIntB>
                        <testFloatA>0.1234</testFloatA>
                        <testFloatB>-8000000000000000</testFloatB>
                        <testStringA>Hello</testStringA>
                        <testStringB>Data, data, data</testStringB>
                        <testStringC>Forsooth</testStringC>
                        <testBoolA>true</testBoolA>
                        <testBoolB>false</testBoolB>
                    </BulkParseDef>
                </Defs>",
                new Type[]{ typeof(BulkParseDef) });

            var result = Def.Database<BulkParseDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(35, result.testIntA);
            Assert.AreEqual(-20, result.testIntB);
            Assert.AreEqual(3, result.testIntC);
            Assert.AreEqual(0.1234f, result.testFloatA);
            Assert.AreEqual(-8000000000000000f, result.testFloatB);
            Assert.AreEqual(3, result.testFloatC);
            Assert.AreEqual("Hello", result.testStringA);
            Assert.AreEqual("Data, data, data", result.testStringB);
            Assert.AreEqual("Forsooth", result.testStringC);
            Assert.AreEqual("four", result.testStringD);
            Assert.AreEqual(true, result.testBoolA);
            Assert.AreEqual(false, result.testBoolB);
            Assert.AreEqual(false, result.testBoolC);
	    }
    }
}
