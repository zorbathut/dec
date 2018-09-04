namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Primitives : Base
    {
                public class BasicParseDef : Def.Def
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
	    public void BasicParse()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <BasicParseDef defName=""TestDef"">
                        <testIntA>35</testIntA>
                        <testIntB>-20</testIntB>
                        <testFloatA>0.1234</testFloatA>
                        <testFloatB>-8000000000000000</testFloatB>
                        <testStringA>Hello</testStringA>
                        <testStringB>Data, data, data</testStringB>
                        <testStringC>Forsooth</testStringC>
                        <testBoolA>true</testBoolA>
                        <testBoolB>false</testBoolB>
                    </BasicParseDef>
                </Defs>",
                new Type[]{ typeof(BasicParseDef) });

            var result = Def.Database<BasicParseDef>.Get("TestDef");
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

        public class EmptyStringParseDef : Def.Def
        {
            public string testStringA = "one";
            public string testStringB = "two";
        }

	    [Test]
	    public void EmptyStringParse()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <EmptyStringParseDef defName=""TestDef"">
                        <testStringA></testStringA>
                        <testStringB />
                    </EmptyStringParseDef>
                </Defs>",
                new Type[]{ typeof(EmptyStringParseDef) });

            var result = Def.Database<EmptyStringParseDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual("", result.testStringA);
            Assert.AreEqual("", result.testStringB);
	    }

        public class EmptyIntParseDef : Def.Def
        {
            public int testInt = 4;
        }

	    [Test]
	    public void EmptyIntParse()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <EmptyIntParseDef defName=""TestDef"">
                        <testInt />
                    </EmptyIntParseDef>
                </Defs>",
                new Type[]{ typeof(EmptyIntParseDef) }));

            var result = Def.Database<EmptyIntParseDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.testInt);
	    }

        public class EmptyBoolParseDef : Def.Def
        {
            public bool testBool = true;
        }

	    [Test]
	    public void EmptyBoolParse()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <EmptyBoolParseDef defName=""TestDef"">
                        <testBool />
                    </EmptyBoolParseDef>
                </Defs>",
                new Type[]{ typeof(EmptyBoolParseDef) }));

            var result = Def.Database<EmptyBoolParseDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(false, result.testBool);
	    }
    }
}
