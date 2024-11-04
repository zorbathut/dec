using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DecTest
{
    [TestFixture]
    public class Primitives : Base
    {
        public class IntDec : Dec.Dec
        {
            public int value = 4;
        }

        public class FloatDec : Dec.Dec
        {
            public float value = 4;
        }

        public class BoolDec : Dec.Dec
        {
            public bool value = true;
        }

        public class StringDec : Dec.Dec
        {
            public string value = "one";
        }

        [Test]
        public void EmptyIntParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value />
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void FailingIntParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>NotAnInt</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void FailingIntParse2([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>10NotAnInt</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void IntRange([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>1234123412341234123412341234123412341234</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void EmptyBoolParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BoolDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BoolDec decName=""TestDec"">
                        <value />
                    </BoolDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<BoolDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(true, result.value);
        }

        [Test]
        public void FailingBoolParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BoolDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BoolDec decName=""TestDec"">
                        <value>NotABool</value>
                    </BoolDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<BoolDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(true, result.value);
        }

        [Test]
        public void EmptyStringParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StringDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StringDec decName=""TestDec"">
                        <value />
                    </StringDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<StringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual("", result.value);
        }

        public class BulkParseDec : Dec.Dec
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
        public void BulkParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BulkParseDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BulkParseDec decName=""TestDec"">
                        <testIntA>35</testIntA>
                        <testIntB>-20</testIntB>
                        <testFloatA>0.1234</testFloatA>
                        <testFloatB>-8000000000000000</testFloatB>
                        <testStringA>Hello</testStringA>
                        <testStringB>Data, data, data</testStringB>
                        <testStringC>Forsooth</testStringC>
                        <testBoolA>true</testBoolA>
                        <testBoolB>false</testBoolB>
                    </BulkParseDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<BulkParseDec>.Get("TestDec");
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

        public class MissingMemberDec : Dec.Dec
        {
            public int value1;
            public int value3;
        }

        [Test]
        public void MissingMember([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MissingMemberDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MissingMemberDec decName=""TestDec"">
                        <value1>9</value1>
                        <value2>99</value2>
                        <value3>999</value3>
                    </MissingMemberDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<MissingMemberDec>.Get("TestDec");
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

        public class EnumDec : Dec.Dec
        {
            public ExampleEnum value;
        }

        [Test]
        public void Enum([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(EnumDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <EnumDec decName=""TestDec"">
                        <value>Two</value>
                    </EnumDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<EnumDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value, ExampleEnum.Two);
        }

        [Test]
        public void InvalidAttribute([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value invalid=""yes"">5</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value, 5);
        }

        public class TypeDec : Dec.Dec
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
        public void TypeBasic([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Example</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Example));
        }

        [Test]
        public void TypeNested([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives", "DecTest.Primitives.ContainerA", "DecTest.Primitives.ContainerB" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>NotOverridden</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(ContainerB.NotOverridden));
        }

        [Test]
        public void TypeStatic([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Static</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Static));
        }

        [Test]
        public void TypeAbstract([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Abstract</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Abstract));
        }

        [Test]
        public void TypeDecRef([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>TypeDec</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(TypeDec));
        }

        [Test]
        public void TypeGenericA([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Generic</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
        }

        [Test]
        public void TypeGenericB([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Generic&lt;&gt;</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
        }

        [Test]
        public void TypeGenericC([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Generic&lt;int&gt;</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
        }

        [Test]
        public void TypeOverridden([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives", "DecTest.Primitives.ContainerA", "DecTest.Primitives.ContainerB" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Overridden</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.type);
        }

        [Test]
        public void TypeComplete([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>DecTest.Primitives.Example</type> <!-- conveniently tests both namespaces and classes at the same time -->
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(typeof(Example), result.type);
        }

        public class Ieee754SpecialDec : Dec.Dec
        {
            public float floatNan;
            public float floatInfUpper;
            public float floatInfLower;
            public float floatInfSymbol;
            public float floatNinfUpper;
            public float floatNinfLower;
            public float floatNinfSymbol;
            public float floatEpsilon;
            public double doubleNan;
            public double doubleInfUpper;
            public double doubleInfLower;
            public double doubleInfSymbol;
            public double doubleNinfUpper;
            public double doubleNinfLower;
            public double doubleNinfSymbol;
            public double doubleEpsilon;
        }

        [Test]
        public void Ieee754Special([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Ieee754SpecialDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Ieee754SpecialDec decName=""TestDec"">
                        <floatNan>NaN</floatNan>
                        <floatInfUpper>Infinity</floatInfUpper>
                        <floatInfLower>infinity</floatInfLower>
                        <floatInfSymbol>∞</floatInfSymbol>
                        <floatNinfUpper>-Infinity</floatNinfUpper>
                        <floatNinfLower>-infinity</floatNinfLower>
                        <floatNinfSymbol>-∞</floatNinfSymbol>
                        <floatEpsilon>1.401298E-45</floatEpsilon>
                        <doubleNan>NaN</doubleNan>
                        <doubleInfUpper>Infinity</doubleInfUpper>
                        <doubleInfLower>infinity</doubleInfLower>
                        <doubleInfSymbol>∞</doubleInfSymbol>
                        <doubleNinfUpper>-Infinity</doubleNinfUpper>
                        <doubleNinfLower>-infinity</doubleNinfLower>
                        <doubleNinfSymbol>-∞</doubleNinfSymbol>
                        <doubleEpsilon>4.94065645841247E-324</doubleEpsilon>
                    </Ieee754SpecialDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<Ieee754SpecialDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(float.NaN, result.floatNan);
            Assert.AreEqual(float.PositiveInfinity, result.floatInfUpper);
            Assert.AreEqual(float.PositiveInfinity, result.floatInfLower);
            Assert.AreEqual(float.PositiveInfinity, result.floatInfSymbol);
            Assert.AreEqual(float.NegativeInfinity, result.floatNinfUpper);
            Assert.AreEqual(float.NegativeInfinity, result.floatNinfLower);
            Assert.AreEqual(float.NegativeInfinity, result.floatNinfSymbol);
            Assert.AreEqual(float.Epsilon, result.floatEpsilon);
            Assert.AreEqual(double.NaN, result.doubleNan);
            Assert.AreEqual(double.PositiveInfinity, result.doubleInfUpper);
            Assert.AreEqual(double.PositiveInfinity, result.doubleInfLower);
            Assert.AreEqual(double.PositiveInfinity, result.doubleInfSymbol);
            Assert.AreEqual(double.NegativeInfinity, result.doubleNinfUpper);
            Assert.AreEqual(double.NegativeInfinity, result.doubleNinfLower);
            Assert.AreEqual(double.NegativeInfinity, result.doubleNinfSymbol);
            Assert.AreEqual(double.Epsilon, result.doubleEpsilon);
        }

        public class StructConstructionDec : Dec.Dec
        {
            public List<StructData> directList;
            public List<IStructPointer> interfaceList;
            public List<object> objectList;
        }

        public interface IStructPointer { }

        public struct StructData
        {
            public int value;
        }

        public struct StructDataFromInterface : IStructPointer
        {
            public int value;
        }

        [Test]
        public void StructConstructionDirect([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StructConstructionDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StructConstructionDec decName=""TestDec"">
                        <directList>
                            <li><value>42</value></li>
                            <li><value>100</value></li>
                            <li><value>8</value></li>
                        </directList>
                    </StructConstructionDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<StructConstructionDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(3, result.directList.Count);

            Assert.AreEqual(42, result.directList[0].value);
            Assert.AreEqual(100, result.directList[1].value);
            Assert.AreEqual(8, result.directList[2].value);
        }

        [Test]
        public void StructConstructionInterface([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StructConstructionDec), typeof(StructDataFromInterface) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StructConstructionDec decName=""TestDec"">
                        <interfaceList>
                            <li class=""StructDataFromInterface""><value>42</value></li>
                            <li class=""StructDataFromInterface""><value>100</value></li>
                            <li class=""StructDataFromInterface""><value>8</value></li>
                        </interfaceList>
                    </StructConstructionDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<StructConstructionDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(3, result.interfaceList.Count);

            Assert.AreEqual(42, ((StructDataFromInterface)result.interfaceList[0]).value);
            Assert.AreEqual(100, ((StructDataFromInterface)result.interfaceList[1]).value);
            Assert.AreEqual(8, ((StructDataFromInterface)result.interfaceList[2]).value);
        }

        [Test]
        public void PrimitiveConstructionObject([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StructConstructionDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StructConstructionDec decName=""TestDec"">
                        <objectList>
                            <li class=""int"">42</li>
                            <li class=""int"">100</li>
                            <li class=""int"">8</li>
                        </objectList>
                    </StructConstructionDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<StructConstructionDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(3, result.objectList.Count);

            Assert.AreEqual(42, (int)result.objectList[0]);
            Assert.AreEqual(100, (int)result.objectList[1]);
            Assert.AreEqual(8, (int)result.objectList[2]);
        }

        public class NullDec : Dec.Dec
        {
            public Stub initialized = new Stub();
            public Stub setToNull = null;
        }

        [Test]
        public void Null([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NullDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NullDec decName=""LeaveDefault"" />
                    <NullDec decName=""TargetNull"">
                        <initialized null=""true"" />
                        <setToNull null=""true"" />
                    </NullDec>
                    <NullDec decName=""TargetFilled"">
                        <initialized />
                        <setToNull />
                    </NullDec>
                    <NullDec decName=""ParseExercise"">
                        <initialized null=""True""/>
                        <setToNull null=""TRUE""/>
                    </NullDec>
                    <NullDec decName=""ExplicitFalse"">
                        <initialized null=""false""/>
                        <setToNull null=""False""/>
                    </NullDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var leaveDefault = Dec.Database<NullDec>.Get("LeaveDefault");
            Assert.IsNotNull(leaveDefault);
            Assert.IsNotNull(leaveDefault.initialized);
            Assert.IsNull(leaveDefault.setToNull);

            var targetNull = Dec.Database<NullDec>.Get("TargetNull");
            Assert.IsNotNull(targetNull);
            Assert.IsNull(targetNull.initialized);
            Assert.IsNull(targetNull.setToNull);

            var targetFilled = Dec.Database<NullDec>.Get("TargetFilled");
            Assert.IsNotNull(targetFilled);
            Assert.IsNotNull(targetFilled.initialized);
            Assert.IsNotNull(targetFilled.setToNull);

            var parseExercise = Dec.Database<NullDec>.Get("ParseExercise");
            Assert.IsNotNull(parseExercise);
            Assert.IsNull(parseExercise.initialized);
            Assert.IsNull(parseExercise.setToNull);

            var explicitFalse = Dec.Database<NullDec>.Get("ExplicitFalse");
            Assert.IsNotNull(explicitFalse);
            Assert.IsNotNull(explicitFalse.initialized);
            Assert.IsNotNull(explicitFalse.setToNull);
        }

        [Test]
        public void StubWithString([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NullDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NullDec decName=""TestDec"">
                        <setToNull>horse</setToNull>
                    </NullDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), errorValidator: error => error.Contains("Text detected"));

            DoParserTests(mode);

            var testDec = Dec.Database<NullDec>.Get("TestDec");
            Assert.IsNotNull(testDec);
            Assert.IsNotNull(testDec.initialized);
            Assert.IsNull(testDec.setToNull);
        }

        [Test]
        public void FloatLocale([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(FloatDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <FloatDec decName=""TestDec"">
                        <value>2.34</value>
                    </FloatDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<FloatDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(2.34f, result.value);
        }

        [Test]
        public void NaNBoxFloat([Values] RecorderMode mode)
        {
            var list = new List<float>()
            {
                float.NaN,
                BitConverter.Int32BitsToSingle(0x7fc0beef),
                BitConverter.Int32BitsToSingle(0x7fc01234),
            };

            for (int i = 1; i < list.Count; ++i)
            {
                // make sure nan boxing actually worked
                Assert.AreNotEqual(BitConverter.SingleToInt32Bits(list[0]), BitConverter.SingleToInt32Bits(list[i]));
                Assert.IsTrue(float.IsNaN(list[i]));
            }

            var deserialized = DoRecorderRoundTrip(list, RecorderMode.Bare);

            for (int i = 0; i < list.Count; ++i)
            {
                Assert.AreEqual(BitConverter.SingleToInt32Bits(list[i]), BitConverter.SingleToInt32Bits(deserialized[i]));
            }
        }

        [Test]
        public void NaNBoxDouble([Values] RecorderMode mode)
        {
            var list = new List<double>()
            {
                double.NaN,
                BitConverter.Int64BitsToDouble(0x7ffcbeefbeefbeef),
                BitConverter.Int64BitsToDouble(0x7ffc123412341234),
            };

            for (int i = 1; i < list.Count; ++i)
            {
                // make sure nan boxing actually worked
                Assert.AreNotEqual(BitConverter.DoubleToInt64Bits(list[0]), BitConverter.DoubleToInt64Bits(list[i]));
                Assert.IsTrue(double.IsNaN(list[i]));
            }

            var deserialized = DoRecorderRoundTrip(list, mode);

            for (int i = 0; i < list.Count; ++i)
            {
                Assert.AreEqual(BitConverter.DoubleToInt64Bits(list[i]), BitConverter.DoubleToInt64Bits(deserialized[i]));
            }
        }

        [Test]
        public void SignalingNaN([Values] RecorderMode mode)
        {
            var pair = (
                flt: BitConverter.Int32BitsToSingle(0x7fa00000),
                dbl: BitConverter.Int64BitsToDouble(0x7ffa000000000000)
            );

            var deserialized = DoRecorderRoundTrip(pair, mode);

            Assert.AreEqual(BitConverter.SingleToInt32Bits(pair.flt), BitConverter.SingleToInt32Bits(deserialized.flt));
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(pair.dbl), BitConverter.DoubleToInt64Bits(deserialized.dbl));
        }

        public struct OptionalsValue : Dec.IRecordable
        {
            public int a;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref a, "a");
            }
        }
        public struct OptionalsStruct : Dec.IRecordable
        {
            public int int_val;
            public int? int_opt_nul;
            public int? int_opt_val;
            public OptionalsValue? struct_opt_nul;
            public OptionalsValue? struct_opt_val;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref int_val, "int_val");
                recorder.Record(ref int_opt_nul, "int_opt_nul");
                recorder.Record(ref int_opt_val, "int_opt_val");
                recorder.Record(ref struct_opt_nul, "struct_opt_nul");
                recorder.Record(ref struct_opt_val, "struct_opt_val");
            }
        }

        [Test]
        public void Optionals([Values] RecorderMode mode)
        {
            var original = new OptionalsStruct
            {
                int_val = 1,
                int_opt_nul = null,
                int_opt_val = 2,
                struct_opt_nul = null,
                struct_opt_val = new OptionalsValue { a = 3 },
            };

            var deserialized = DoRecorderRoundTrip(original, mode);

            Assert.AreEqual(original.int_val, deserialized.int_val);
            Assert.AreEqual(original.int_opt_nul, deserialized.int_opt_nul);
            Assert.AreEqual(original.int_opt_val, deserialized.int_opt_val);
            Assert.AreEqual(original.struct_opt_nul, deserialized.struct_opt_nul);
            Assert.AreEqual(original.struct_opt_val?.a, deserialized.struct_opt_val?.a);
        }
    }
}
