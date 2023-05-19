namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

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
        public void EmptyIntParse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value />
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void FailingIntParse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>NotAnInt</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void FailingIntParse2([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>10NotAnInt</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void IntRange([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>1234123412341234123412341234123412341234</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(4, result.value);
        }

        [Test]
        public void EmptyBoolParse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BoolDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <BoolDec decName=""TestDec"">
                        <value />
                    </BoolDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<BoolDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(true, result.value);
        }

        [Test]
        public void FailingBoolParse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BoolDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <BoolDec decName=""TestDec"">
                        <value>NotABool</value>
                    </BoolDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<BoolDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(true, result.value);
        }

        [Test]
        public void EmptyStringParse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <StringDec decName=""TestDec"">
                        <value />
                    </StringDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

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
        public void BulkParse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BulkParseDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

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
        public void MissingMember([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MissingMemberDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <MissingMemberDec decName=""TestDec"">
                        <value1>9</value1>
                        <value2>99</value2>
                        <value3>999</value3>
                    </MissingMemberDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

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
        public void Enum([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(EnumDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <EnumDec decName=""TestDec"">
                        <value>Two</value>
                    </EnumDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<EnumDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value, ExampleEnum.Two);
        }

        [Test]
        public void InvalidAttribute([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value invalid=""yes"">5</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

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
        public void TypeBasic([Values] BehaviorMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives" };
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Example</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Example));
        }

        [Test]
        public void TypeNested([Values] BehaviorMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives", "DecTest.Primitives.ContainerA", "DecTest.Primitives.ContainerB" };
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>NotOverridden</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(ContainerB.NotOverridden));
        }

        [Test]
        public void TypeStatic([Values] BehaviorMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives" };
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Static</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Static));
        }

        [Test]
        public void TypeAbstract([Values] BehaviorMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives" };
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Abstract</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(Abstract));
        }

        [Test]
        public void TypeDecRef([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>TypeDec</type>
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.type, typeof(TypeDec));
        }

        [Test]
        public void TypeGenericA([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Generic</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
        }

        [Test]
        public void TypeGenericB([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Generic&lt;&gt;</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
        }

        [Test]
        public void TypeGenericC([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Generic&lt;int&gt;</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.type);
        }

        [Test]
        public void TypeOverridden([Values] BehaviorMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Primitives", "DecTest.Primitives.ContainerA", "DecTest.Primitives.ContainerB" };
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>Overridden</type>
                    </TypeDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.type);
        }

        [Test]
        public void TypeComplete([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <TypeDec decName=""TestDec"">
                        <type>DecTest.Primitives+Example</type> <!-- conveniently tests both namespaces and classes at the same time -->
                    </TypeDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<TypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(typeof(Example), result.type);
        }

        public class Ieee754SpecialDec : Dec.Dec
        {
            public float floatNan;
            public float floatInf;
            public float floatNinf;
            public float floatEpsilon;
            public double doubleNan;
            public double doubleInf;
            public double doubleNinf;
            public double doubleEpsilon;
        }

        [Test]
        public void Ieee754Special([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Ieee754SpecialDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <Ieee754SpecialDec decName=""TestDec"">
                        <floatNan>NaN</floatNan>
                        <floatInf>Infinity</floatInf>
                        <floatNinf>-Infinity</floatNinf>
                        <floatEpsilon>1.401298E-45</floatEpsilon>
                        <doubleNan>NaN</doubleNan>
                        <doubleInf>Infinity</doubleInf>
                        <doubleNinf>-Infinity</doubleNinf>
                        <doubleEpsilon>4.94065645841247E-324</doubleEpsilon>
                    </Ieee754SpecialDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<Ieee754SpecialDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(float.NaN, result.floatNan);
            Assert.AreEqual(float.PositiveInfinity, result.floatInf);
            Assert.AreEqual(float.NegativeInfinity, result.floatNinf);
            Assert.AreEqual(float.Epsilon, result.floatEpsilon);
            Assert.AreEqual(double.NaN, result.doubleNan);
            Assert.AreEqual(double.PositiveInfinity, result.doubleInf);
            Assert.AreEqual(double.NegativeInfinity, result.doubleNinf);
            Assert.AreEqual(double.Epsilon, result.doubleEpsilon);

            // We currently don't support NaN-boxed values or signaling NaN.
            // Maybe someday we will.
            // (Does C# even accept those?)
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
        public void StructConstructionDirect([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StructConstructionDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<StructConstructionDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(3, result.directList.Count);

            Assert.AreEqual(42, result.directList[0].value);
            Assert.AreEqual(100, result.directList[1].value);
            Assert.AreEqual(8, result.directList[2].value);
        }

        [Test]
        public void StructConstructionInterface([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StructConstructionDec), typeof(StructDataFromInterface) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<StructConstructionDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(3, result.interfaceList.Count);

            Assert.AreEqual(42, ((StructDataFromInterface)result.interfaceList[0]).value);
            Assert.AreEqual(100, ((StructDataFromInterface)result.interfaceList[1]).value);
            Assert.AreEqual(8, ((StructDataFromInterface)result.interfaceList[2]).value);
        }

        [Test]
        public void PrimitiveConstructionObject([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StructConstructionDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

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
        public void Null([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NullDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

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
        public void FloatLocale([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(FloatDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <FloatDec decName=""TestDec"">
                        <value>2.34</value>
                    </FloatDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<FloatDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(2.34, result.value);
        }
    }
}
