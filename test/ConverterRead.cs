namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    [TestFixture]
    public class ConverterRead : Base
    {
        public class ConverterTestPayload
        {
            public int number = 0;
        }

        public class ConverterDec : Dec.Dec
        {
            public ConverterTestPayload payload;
        }

        public class ConverterBasicTest : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterTestPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new ConverterTestPayload() { number = int.Parse(input) };
            }

            public override object FromXml(XElement input, Type type, string inputName)
            {
                return new ConverterTestPayload() { number = int.Parse(input.Elements().First().Value) };
            }

            // This doesn't 100% preserve the original format because there's no way to tell if it should be contained in cargo or not
            // But honestly that's fine, I don't care for this purpose
            public override string ToString(object input)
            {
                return (input as ConverterTestPayload).number.ToString();
            }
        }

        [Test]
        public void BasicFunctionality([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterDec) }, explicitConverters = new Type[]{ typeof(ConverterBasicTest) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ConverterDec decName=""TestDecA"">
                        <payload>4</payload>
                    </ConverterDec>
                    <ConverterDec decName=""TestDecB"">
                        <payload><cargo>8</cargo></payload>
                    </ConverterDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(4, Dec.Database<ConverterDec>.Get("TestDecA").payload.number);
            Assert.AreEqual(8, Dec.Database<ConverterDec>.Get("TestDecB").payload.number);
        }

        public class EmptyConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { };
            }
        }

        [Test]
        public void EmptyConverterErr()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(EmptyConverter) } };

            Dec.Parser parser = null;
            ExpectErrors(() => parser = new Dec.Parser());
            parser.Finish();
        }

        public class StrConv1 : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(string) };
            }
        }

        public class StrConv2 : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(string) };
            }
        }

        [Test]
        public void OverlappingConverters()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(StrConv1), typeof(StrConv2) } };

            Dec.Parser parser = null;
            ExpectErrors(() => parser = new Dec.Parser());
            parser.Finish();
        }

        public class ConverterStringPayload
        {
            public string payload;
        }

        public class ConverterDictTest : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStringPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new ConverterStringPayload() { payload = input };
            }

            public override string ToString(object input)
            {
                return (input as ConverterStringPayload).payload;
            }
        }

        public class ConverterDictDec : Dec.Dec
        {
            public Dictionary<ConverterStringPayload, int> payload;
        }

        [Test]
        public void ConverterDict([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterDictDec) }, explicitConverters = new Type[]{ typeof(ConverterDictTest) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ConverterDictDec decName=""TestDec"">
                        <payload>
                            <yabba>1</yabba>
                            <dabba>4</dabba>
                            <doo>9</doo>
                        </payload>
                    </ConverterDictDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var testDec = Dec.Database<ConverterDictDec>.Get("TestDec");

            Assert.AreEqual(1, testDec.payload.Where(kvp => kvp.Key.payload == "yabba").First().Value);
            Assert.AreEqual(4, testDec.payload.Where(kvp => kvp.Key.payload == "dabba").First().Value);
            Assert.AreEqual(9, testDec.payload.Where(kvp => kvp.Key.payload == "doo").First().Value);
        }

        public class ConverterStringDec : Dec.Dec
        {
            public ConverterStringPayload payload;
        }

        [Test]
        public void EmptyInputConverter([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterStringDec) }, explicitConverters = new Type[]{ typeof(ConverterDictTest) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ConverterStringDec decName=""TestDec"">
                        <payload></payload>
                    </ConverterStringDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var testDec = Dec.Database<ConverterStringDec>.Get("TestDec");

            Assert.AreEqual("", testDec.payload.payload);
        }

        public class DefaultFailureConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStringPayload) };
            }
        }

        [Test]
        public void DefaultFailureTestString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterStringDec) }, explicitConverters = new Type[]{ typeof(DefaultFailureConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ConverterStringDec decName=""TestDec"">
                        <payload>stringfail</payload>
                    </ConverterStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var testDec = Dec.Database<ConverterStringDec>.Get("TestDec");
            Assert.IsNull(testDec.payload);
        }

        [Test]
        public void DefaultFailureTestXml([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterStringDec) }, explicitConverters = new Type[]{ typeof(DefaultFailureConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ConverterStringDec decName=""TestDec"">
                        <payload><xmlfail></xmlfail></payload>
                    </ConverterStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var testDec = Dec.Database<ConverterStringDec>.Get("TestDec");
            Assert.IsNull(testDec.payload);
        }

        public class NonEmptyPayloadDec : Dec.Dec
        {
            public ConverterStringPayload payload = new ConverterStringPayload();
        }

        public class DefaultNullConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStringPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return null;
            }
        }

        // Skipping BehaviorMode on this one; it's kind of weird that you can have a token that generates null without being "null", but it's even weirder to serialize that.
        // This works for now but I don't see much reason to make it work with serialization. (For now, at least.)
        // It could in theory be accomplished with XML . . .
        [Test]
        public void ConvertToNull()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(NonEmptyPayloadDec) }, explicitConverters = new Type[]{ typeof(DefaultNullConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <NonEmptyPayloadDec decName=""TestDefault"">
                    </NonEmptyPayloadDec>
                    <NonEmptyPayloadDec decName=""TestNull"">
                        <payload>makemenull</payload>
                    </NonEmptyPayloadDec>
                </Decs>");
            parser.Finish();

            Assert.IsNotNull(Dec.Database<NonEmptyPayloadDec>.Get("TestDefault").payload);
            Assert.IsNull(Dec.Database<NonEmptyPayloadDec>.Get("TestNull").payload);
        }

        public struct ConverterStructObj
        {
            public int intA;
            public int intB;
        }

        public class ConverterStructDec : Dec.Dec
        {
            public ConverterStructObj replacedDefault;
            public ConverterStructObj initializedUntouched = new ConverterStructObj { intA = 1, intB = 2 };
            public ConverterStructObj initializedReplaced = new ConverterStructObj { intA = 3, intB = 4 };
            public ConverterStructObj initializedTouched = new ConverterStructObj { intA = 5, intB = 6 };
        }

        public class ConverterStructConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ConverterStructObj) };
            }

            public override object Record(object model, Type type, Dec.Recorder recorder)
            {
                ConverterStructObj cso;

                if (model == null)
                {
                    cso = new ConverterStructObj();
                }
                else
                {
                    cso = (ConverterStructObj)model;
                }

                recorder.Record(ref cso.intA, "intA");
                recorder.Record(ref cso.intB, "intB");

                return cso;
            }
        }

        [Test]
        public void ConverterStruct([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConverterStructDec) }, explicitConverters = new Type[] { typeof(ConverterStructConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ConverterStructDec decName=""TestDec"">
                        <replacedDefault>
                            <intA>20</intA>
                            <intB>21</intB>
                        </replacedDefault>
                        <initializedReplaced>
                            <intA>22</intA>
                            <intB>23</intB>
                        </initializedReplaced>
                        <initializedTouched>
                            <intA>24</intA>
                        </initializedTouched>
                    </ConverterStructDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var testDec = Dec.Database<ConverterStructDec>.Get("TestDec");

            Assert.AreEqual(20, testDec.replacedDefault.intA);
            Assert.AreEqual(21, testDec.replacedDefault.intB);

            Assert.AreEqual(1, testDec.initializedUntouched.intA);
            Assert.AreEqual(2, testDec.initializedUntouched.intB);

            Assert.AreEqual(22, testDec.initializedReplaced.intA);
            Assert.AreEqual(23, testDec.initializedReplaced.intB);

            Assert.AreEqual(24, testDec.initializedTouched.intA);
            Assert.AreEqual(6, testDec.initializedTouched.intB);
        }

        public class FallbackPayload
        {
            public int number = 0;
        }

        public class FallbackDec : Dec.Dec
        {
            public FallbackPayload payload;
        }

        public class FallbackConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(FallbackPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new FallbackPayload() { number = int.Parse(input) };
            }

            public override string ToString(object input)
            {
                return (input as FallbackPayload).number.ToString();
            }
        }

        [Test]
        public void Fallback([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(FallbackDec) }, explicitConverters = new Type[] { typeof(FallbackConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <FallbackDec decName=""TestDec"">
                        <payload>
                            4
                            <garbage />
                        </payload>
                    </FallbackDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            Assert.AreEqual(4, Dec.Database<FallbackDec>.Get("TestDec").payload.number);
        }

        public class ExceptionPayload
        {
            public int value = 0;
        }

        public struct ExceptionPayloadStruct
        {

        }

        public class ExceptionDec : Dec.Dec
        {
            public ExceptionPayload payload;

            public int before;
            public int after;
        }

        public class ExceptionConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(ExceptionPayload), typeof(ExceptionPayloadStruct) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                throw new InvalidOperationException();
            }

            public override string ToString(object input)
            {
                throw new InvalidOperationException();
            }
        }

        [Test]
        public void Exception([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionDec) }, explicitConverters = new Type[] { typeof(ExceptionConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ExceptionDec decName=""TestDecA"">
                        <before>1</before>
                        <payload />
                        <after>2</after>
                    </ExceptionDec>
                    <ExceptionDec decName=""TestDecB"">
                        <before>3</before>
                        <payload />
                        <after>4</after>
                    </ExceptionDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            Assert.AreEqual(1, Dec.Database<ExceptionDec>.Get("TestDecA").before);
            Assert.AreEqual(2, Dec.Database<ExceptionDec>.Get("TestDecA").after);
            Assert.AreEqual(3, Dec.Database<ExceptionDec>.Get("TestDecB").before);
            Assert.AreEqual(4, Dec.Database<ExceptionDec>.Get("TestDecB").after);
        }

        public class ExceptionRecoveryDec : Dec.Dec
        {
            public ExceptionPayload payloadNull;
            public ExceptionPayload payloadNonNull = new ExceptionPayload() { value = 42 };
            public List<ExceptionPayloadStruct> payloadStruct;  // needs to be a list because that's the only way to make it null
        }

        [Test]
        public void ExceptionRecoveryNull()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionRecoveryDec) }, explicitConverters = new Type[] { typeof(ExceptionConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ExceptionRecoveryDec decName=""TestDec"">
                        <payloadNull>cube</payloadNull>
                    </ExceptionRecoveryDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(BehaviorMode.Bare);

            Assert.IsNull(Dec.Database<ExceptionRecoveryDec>.Get("TestDec").payloadNull);
        }

        [Test]
        public void ExceptionRecoveryNonNull()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionRecoveryDec) }, explicitConverters = new Type[] { typeof(ExceptionConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ExceptionRecoveryDec decName=""TestDec"">
                        <payloadNonNull>cube</payloadNonNull>
                    </ExceptionRecoveryDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(BehaviorMode.Bare);

            // won't overwrite it
            Assert.AreEqual(42, Dec.Database<ExceptionRecoveryDec>.Get("TestDec").payloadNonNull.value);
        }

        [Test]
        public void ExceptionRecoveryStruct()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionRecoveryDec) }, explicitConverters = new Type[] { typeof(ExceptionConverter) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ExceptionRecoveryDec decName=""TestDec"">
                        <payloadStruct><li>cube</li></payloadStruct>
                    </ExceptionRecoveryDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(BehaviorMode.Bare);

            // won't overwrite it
            Assert.AreEqual(1, Dec.Database<ExceptionRecoveryDec>.Get("TestDec").payloadStruct.Count);
        }
    }
}
