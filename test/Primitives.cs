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
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(IntDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value />
                    </IntDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.value);
	    }

        [Test]
	    public void FailingIntParse()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(IntDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>NotAnInt</value>
                    </IntDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.value);
	    }

        [Test]
	    public void FailingIntParse2()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(IntDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>10NotAnInt</value>
                    </IntDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(0, result.value);
	    }

	    [Test]
	    public void EmptyBoolParse()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(BoolDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <BoolDef defName=""TestDef"">
                        <value />
                    </BoolDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<BoolDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(false, result.value);
	    }

	    [Test]
	    public void FailingBoolParse()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(BoolDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <BoolDef defName=""TestDef"">
                        <value>NotABool</value>
                    </BoolDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<BoolDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(false, result.value);
	    }

        [Test]
	    public void EmptyStringParse()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StringDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <StringDef defName=""TestDef"">
                        <value />
                    </StringDef>
                </Defs>");
            parser.Finish();

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
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(BulkParseDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
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
                </Defs>");
            parser.Finish();

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

        public class MissingMemberDef : Def.Def
        {
            public int value1;
            public int value3;
        }

        [Test]
	    public void MissingMember()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(MissingMemberDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <MissingMemberDef defName=""TestDef"">
                        <value1>9</value1>
                        <value2>99</value2>
                        <value3>999</value3>
                    </MissingMemberDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<MissingMemberDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value1, 9);
            Assert.AreEqual(result.value3, 999);
	    }

        public enum ExampleEnum
        {
            One,
            Two,
            Three,
        }

        public class EnumDef : Def.Def
        {
            public ExampleEnum value;
        }

        [Test]
	    public void Enum()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(EnumDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <EnumDef defName=""TestDef"">
                        <value>Two</value>
                    </EnumDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<EnumDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value, ExampleEnum.Two);
	    }

        [Test]
	    public void InvalidAttribute()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(IntDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value invalid=""yes"">5</value>
                    </IntDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value, 5);
	    }

        public class TypeDef : Def.Def
        {
            public Type type;
        }

        public class Example { }
        public class ContainerA { public class Overridden { } }
        public class ContainerB { public class Overridden { } public class NotOverridden { } }
        public static class Static { }
        public abstract class Abstract { }
        public class Generic<T> { }

        [Test]
	    public void TypeBasic()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>Example</type>
                    </TypeDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Example));
	    }

        [Test]
	    public void TypeNested()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>NotOverridden</type>
                    </TypeDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(ContainerB.NotOverridden));
	    }

        [Test]
	    public void TypeStatic()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>Static</type>
                    </TypeDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Static));
	    }

        [Test]
	    public void TypeAbstract()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>Abstract</type>
                    </TypeDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Abstract));
	    }

        [Test]
	    public void TypeDefRef()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>TypeDef</type>
                    </TypeDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(TypeDef));
	    }

        [Test]
	    public void TypeGenericA()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>Generic</type>
                    </TypeDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
	    }

        [Test]
	    public void TypeGenericB()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>Generic&lt;&gt;</type>
                    </TypeDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
	    }

        [Test]
	    public void TypeGenericC()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>Generic&lt;int&gt;</type>
                    </TypeDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
	    }

        [Test]
	    public void TypeOverridden()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(TypeDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <TypeDef defName=""TestDef"">
                        <type>Overridden</type>
                    </TypeDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<TypeDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.type);
	    }
    }
}
