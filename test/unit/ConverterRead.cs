using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DecTest
{
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

        public class ConverterBasicTest : Dec.ConverterString<ConverterTestPayload>
        {
            public override ConverterTestPayload Read(string input, Dec.InputContext context)
            {
                return new ConverterTestPayload() { number = int.Parse(input) };
            }

            public override string Write(ConverterTestPayload input)
            {
                return input.number.ToString();
            }
        }

        [Test]
        public void BasicFunctionality([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterDec) }, explicitConverters = new Type[]{ typeof(ConverterBasicTest) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ConverterDec decName=""TestDecA"">
                        <payload>4</payload>
                    </ConverterDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(4, Dec.Database<ConverterDec>.Get("TestDecA").payload.number);
        }

        public class StubConv1 : Dec.ConverterRecord<Stub>
        {
            public override void Record(ref Stub input, Dec.Recorder recorder)
            {

            }
        }

        public class StubConv2 : Dec.ConverterRecord<Stub>
        {
            public override void Record(ref Stub input, Dec.Recorder recorder)
            {

            }
        }

        [Test]
        public void OverlappingConverters()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(StubConv1), typeof(StubConv2) } });

            Dec.Parser parser = null;
            ExpectErrors(() => parser = new Dec.Parser());
            parser.Finish();
        }

        public class ConverterStringPayload
        {
            public string payload;
        }

        public class ConverterDictTest : Dec.ConverterString<ConverterStringPayload>
        {
            public override ConverterStringPayload Read(string input, Dec.InputContext context)
            {
                return new ConverterStringPayload() { payload = input };
            }

            public override string Write(ConverterStringPayload input)
            {
                return input.payload;
            }
        }

        public class ConverterDictDec : Dec.Dec
        {
            public Dictionary<ConverterStringPayload, int> payload;
        }

        [Test]
        public void ConverterDict([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterDictDec) }, explicitConverters = new Type[]{ typeof(ConverterDictTest) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            DoParserTests(mode);

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
        public void EmptyInputConverter([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConverterStringDec) }, explicitConverters = new Type[]{ typeof(ConverterDictTest) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ConverterStringDec decName=""TestDec"">
                        <payload></payload>
                    </ConverterStringDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var testDec = Dec.Database<ConverterStringDec>.Get("TestDec");

            Assert.AreEqual("", testDec.payload.payload);
        }

        public class NonEmptyPayloadDec : Dec.Dec
        {
            public ConverterStringPayload payload = new ConverterStringPayload();
        }

        public class DefaultNullConverter : Dec.ConverterString<ConverterStringPayload>
        {
            public override ConverterStringPayload Read(string input, Dec.InputContext context)
            {
                return null;
            }

            public override string Write(ConverterStringPayload input)
            {
                return "";
            }
        }

        // Skipping ParserMode on this one; it's kind of weird that you can have a token that generates null without being "null", but it's even weirder to serialize that.
        // This works for now but I don't see much reason to make it work with serialization. (For now, at least.)
        // It could in theory be accomplished with XML . . .
        [Test]
        public void ConvertToNull()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(NonEmptyPayloadDec) }, explicitConverters = new Type[]{ typeof(DefaultNullConverter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

        public class ConverterStructConverter : Dec.ConverterRecord<ConverterStructObj>
        {
            public override void Record(ref ConverterStructObj cso, Dec.Recorder recorder)
            {
                recorder.Record(ref cso.intA, "intA");
                recorder.Record(ref cso.intB, "intB");
            }
        }

        [Test]
        public void ConverterStruct([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConverterStructDec) }, explicitConverters = new Type[] { typeof(ConverterStructConverter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            DoParserTests(mode);

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

        public class FallbackConverter : Dec.ConverterString<FallbackPayload>
        {
            public override FallbackPayload Read(string input, Dec.InputContext inputContext)
            {
                return new FallbackPayload() { number = int.Parse(input) };
            }

            public override string Write(FallbackPayload input)
            {
                return input.number.ToString();
            }
        }

        [Test]
        public void Fallback([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(FallbackDec) }, explicitConverters = new Type[] { typeof(FallbackConverter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <FallbackDec decName=""TestDec"">
                        <payload>
                            4
                            <garbage />
                        </payload>
                    </FallbackDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

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

        public class ExceptionPayloadConverter : Dec.ConverterString<ExceptionPayload>
        {
            public override ExceptionPayload Read(string input, Dec.InputContext inputContext)
            {
                throw new InvalidOperationException("EasilyDetectableMessage");
            }

            public override string Write(ExceptionPayload input)
            {
                throw new InvalidOperationException("EasilyDetectableMessage");
            }
        }

        public class ExceptionPayloadStructConverter : Dec.ConverterString<ExceptionPayloadStruct>
        {
            public override ExceptionPayloadStruct Read(string input, Dec.InputContext inputContext)
            {
                throw new InvalidOperationException("EasilyDetectableMessage");
            }

            public override string Write(ExceptionPayloadStruct input)
            {
                throw new InvalidOperationException("EasilyDetectableMessage");
            }
        }

        [Test]
        public void Exception([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionDec) }, explicitConverters = new Type[] { typeof(ExceptionPayloadConverter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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
                </Decs>", identifier: "UniqueIdentifier");
            ExpectErrors(() => parser.Finish(), errorValidator: error => error.Contains("EasilyDetectableMessage") && error.Contains("UniqueIdentifier"));

            DoParserTests(mode);

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
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionRecoveryDec) }, explicitConverters = new Type[] { typeof(ExceptionPayloadConverter), typeof(ExceptionPayloadStructConverter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExceptionRecoveryDec decName=""TestDec"">
                        <payloadNull>cube</payloadNull>
                    </ExceptionRecoveryDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), errorValidator: error => error.Contains("EasilyDetectableMessage"));

            DoParserTests(ParserMode.Bare);

            Assert.IsNull(Dec.Database<ExceptionRecoveryDec>.Get("TestDec").payloadNull);
        }

        [Test]
        public void ExceptionRecoveryNonNull()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionRecoveryDec) }, explicitConverters = new Type[] { typeof(ExceptionPayloadConverter), typeof(ExceptionPayloadStructConverter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExceptionRecoveryDec decName=""TestDec"">
                        <payloadNonNull>cube</payloadNonNull>
                    </ExceptionRecoveryDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), errorValidator: error => error.Contains("EasilyDetectableMessage"));

            DoParserTests(ParserMode.Bare);

            // won't overwrite it
            Assert.AreEqual(42, Dec.Database<ExceptionRecoveryDec>.Get("TestDec").payloadNonNull.value);
        }

        [Test]
        public void ExceptionRecoveryStruct()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExceptionRecoveryDec) }, explicitConverters = new Type[] { typeof(ExceptionPayloadStructConverter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExceptionRecoveryDec decName=""TestDec"">
                        <payloadStruct><li>cube</li></payloadStruct>
                    </ExceptionRecoveryDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), errorValidator: error => error.Contains("EasilyDetectableMessage"));

            DoParserTests(ParserMode.Bare);

            // won't overwrite it
            Assert.AreEqual(1, Dec.Database<ExceptionRecoveryDec>.Get("TestDec").payloadStruct.Count);
        }

        public class ExceptionConverterHolder : Dec.IRecordable
        {
            public ExceptionConverterClass a;
            public ExceptionConverterClass b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, "a");
                recorder.Shared().Record(ref b, "b");
            }
        }

        public class ExceptionConverterDict : Dec.IRecordable
        {
            public Dictionary<ExceptionConverterClass, int> data;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref data, "data");
            }
        }

        public class ExceptionConverterClass
        {
            public string payload;
        }

        public class ExceptionStringConverter : Dec.ConverterString<ExceptionConverterClass>
        {
            public override ExceptionConverterClass Read(string input, Dec.InputContext context)
            {
                throw new InvalidOperationException("EasilyDetectableMessage");
            }

            public override string Write(ExceptionConverterClass input)
            {
                return input.payload;
            }
        }

        public class ExceptionRecordConverter : Dec.ConverterRecord<ExceptionConverterClass>
        {
            public override void Record(ref ExceptionConverterClass input, Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Read)
                {
                    throw new InvalidOperationException("EasilyDetectableMessage");
                }

                recorder.Record(ref input.payload, "payload");
            }
        }

        public class ExceptionFactoryCreateConverter : Dec.ConverterFactory<ExceptionConverterClass>
        {
            public override void Write(ExceptionConverterClass input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.payload, "payload");
            }

            public override ExceptionConverterClass Create(Dec.Recorder recorder)
            {
                throw new InvalidOperationException("EasilyDetectableMessage");
            }

            public override void Read(ref ExceptionConverterClass input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.payload, "payload");
            }
        }

        public class ExceptionFactoryReadConverter : Dec.ConverterFactory<ExceptionConverterClass>
        {
            public override void Write(ExceptionConverterClass input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.payload, "payload");
            }

            public override ExceptionConverterClass Create(Dec.Recorder recorder)
            {
                return new ExceptionConverterClass();
            }

            public override void Read(ref ExceptionConverterClass input, Dec.Recorder recorder)
            {
                throw new InvalidOperationException("EasilyDetectableMessage");
            }
        }

        [Test]
        public void ExceptionStringRead([ValuesExcept(RecorderMode.Clone, RecorderMode.Validation)] RecorderMode mode, [Values] bool asRef)
        {
            if (mode == RecorderMode.Simple && asRef)
            {
                // this is not a combo we care about
                Assert.Ignore();
                return;
            }

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ExceptionStringConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new ExceptionConverterHolder();
            dat.a = new ExceptionConverterClass();
            dat.a.payload = "hello";

            if (asRef)
            {
                dat.b = dat.a;
            }

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: true, readErrorValidator: err => err.Contains("EasilyDetectableMessage") && err.Contains("recorderTestInput"));

            // not readable gets the firehose
            Assert.IsNull(deserialized.a);
            Assert.IsNull(deserialized.b);
        }

        [Test]
        public void ExceptionStringReadAsKey([ValuesExcept(RecorderMode.Clone, RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ExceptionStringConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new ExceptionConverterDict();
            dat.data = new Dictionary<ExceptionConverterClass, int>();
            dat.data[new ExceptionConverterClass() { payload = "hello" }] = 42;

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: true, readErrorValidator: err =>
            {
                if (err.Contains("EasilyDetectableMessage") && err.Contains("recorderTestInput"))
                {
                    return true;
                }

                // multiple errors here
                return err.Contains("Dictionary includes null key");
            });

            Assert.AreEqual(0, deserialized.data.Count);
        }

        [Test]
        public void ExceptionRecordRead([ValuesExcept(RecorderMode.Clone, RecorderMode.Validation)] RecorderMode mode, [Values] bool asRef)
        {
            if (mode == RecorderMode.Simple && asRef)
            {
                // this is not a combo we care about
                Assert.Ignore();
                return;
            }

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ExceptionRecordConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new ExceptionConverterHolder();
            dat.a = new ExceptionConverterClass();
            dat.a.payload = "hello";

            if (asRef)
            {
                dat.b = dat.a;
            }

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: true, readErrorValidator: err => err.Contains("EasilyDetectableMessage") && err.Contains("recorderTestInput"));

            // should I clear this? I don't know
            Assert.IsNotNull(deserialized.a);
            if (asRef)
            {
                Assert.AreSame(deserialized.a, deserialized.b);
            }
            else
            {
                Assert.IsNull(deserialized.b);
            }
        }

        [Test]
        public void ExceptionFactoryCreate([ValuesExcept(RecorderMode.Clone, RecorderMode.Validation)] RecorderMode mode, [Values] bool asRef)
        {
            if (mode == RecorderMode.Simple && asRef)
            {
                // this is not a combo we care about
                Assert.Ignore();
                return;
            }

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ExceptionFactoryCreateConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new ExceptionConverterHolder();
            dat.a = new ExceptionConverterClass();
            dat.a.payload = "hello";

            if (asRef)
            {
                dat.b = dat.a;
            }

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: true, readErrorValidator: err => err.Contains("EasilyDetectableMessage") && err.Contains("recorderTestInput"));

            // not readable gets the firehose
            Assert.IsNull(deserialized.a);
            Assert.IsNull(deserialized.b);
        }

        [Test]
        public void ExceptionFactoryRead([ValuesExcept(RecorderMode.Clone, RecorderMode.Validation)] RecorderMode mode, [Values] bool asRef)
        {
            if (mode == RecorderMode.Simple && asRef)
            {
                // this is not a combo we care about
                Assert.Ignore();
                return;
            }

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ExceptionFactoryReadConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new ExceptionConverterHolder();
            dat.a = new ExceptionConverterClass();
            dat.a.payload = "hello";

            if (asRef)
            {
                dat.b = dat.a;
            }

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: true, readErrorValidator: err => err.Contains("EasilyDetectableMessage") && err.Contains("recorderTestInput"));

            // should I clear this? I don't know
            Assert.IsNotNull(deserialized.a);
            if (asRef)
            {
                Assert.AreSame(deserialized.a, deserialized.b);
            }
            else
            {
                Assert.IsNull(deserialized.b);
            }
        }

        public class RefsForThings
        {
            public List<int> listA;
            public List<int> listB;
        }

        public class RefsForThingsPair : Dec.IRecordable
        {
            public RefsForThings a;
            public RefsForThings b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, "a");
                recorder.Shared().Record(ref b, "b");
            }
        }

        public class RefsInWrongPlacesConverter : Dec.ConverterFactory<RefsForThings>
        {
            public override RefsForThings Create(Dec.Recorder recorder)
            {
                var rv = new RefsForThings();

                recorder.Shared().Record(ref rv.listA, "listA");
                recorder.Shared().Record(ref rv.listB, "listB");

                return rv;
            }

            public override void Read(ref RefsForThings input, Dec.Recorder recorder)
            {

            }

            public override void Write(RefsForThings input, Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref input.listA, "listA");
                recorder.Shared().Record(ref input.listB, "listB");
            }
        }

        public class RefsInRightPlacesConverter : Dec.ConverterFactory<RefsForThings>
        {
            public override RefsForThings Create(Dec.Recorder recorder)
            {
                var rv = new RefsForThings();

                return rv;
            }

            public override void Read(ref RefsForThings input, Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref input.listA, "listA");
                recorder.Shared().Record(ref input.listB, "listB");
            }

            public override void Write(RefsForThings input, Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref input.listA, "listA");
                recorder.Shared().Record(ref input.listB, "listB");
            }
        }

        [Test]
        public void RefsInWrongPlaces([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(RefsInWrongPlacesConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new RefsForThings();
            dat.listA = dat.listB = new List<int>() { 1, 3, 5, 7, 11 };

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: mode != RecorderMode.Clone);

            if (mode != RecorderMode.RefEverything)
            {
                // this actually *can* work because the RefsForThings instance is not, itself, shared
                Assert.AreEqual(dat.listA, deserialized.listA);
            }
            else
            {
                Assert.IsNull(deserialized.listA);
            }

            Assert.AreSame(dat.listA, dat.listB);
        }

        [Test]
        public void RefsInWrongPlacesSubtle([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(RefsInWrongPlacesConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new RefsForThings();
            // these are null, so we'll get the right result, but we want to make sure the errors happen as well

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: mode != RecorderMode.Clone);

            Assert.IsNull(deserialized.listA);
            Assert.IsNull(deserialized.listB);
        }

        [Test]
        public void RefsInWrongPlacesBroken([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(RefsInWrongPlacesConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new RefsForThingsPair();
            dat.a = dat.b = new RefsForThings();
            dat.a.listA = dat.a.listB = new List<int>() { 1, 3, 5, 7, 11 };

            var deserialized = DoRecorderRoundTrip(dat, mode, expectReadErrors: mode != RecorderMode.Clone);

            Assert.IsNotNull(deserialized.a);
            Assert.AreSame(deserialized.a, deserialized.b);

            if (mode != RecorderMode.Clone)
            {
                Assert.IsNull(deserialized.a.listA);
                Assert.IsNull(deserialized.a.listB);
            }
        }

        [Test]
        public void RefsInRightPlaces([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(RefsInRightPlacesConverter) } });
            new Dec.Parser().Finish(); // we're only doing this to kick off the converter init; this is bad and I should fix it

            var dat = new RefsForThings();
            dat.listA = dat.listB = new List<int>() { 1, 3, 5, 7, 11 };

            var deserialized = DoRecorderRoundTrip(dat, mode);

            Assert.AreEqual(dat.listA, deserialized.listA);
            Assert.AreSame(dat.listA, dat.listB);
        }

        public class GenericClass<T>
        {
            public T item;
        }

        public class GenericConverter<T> : Dec.ConverterRecord<GenericClass<T>>
        {
            public override void Record(ref GenericClass<T> input, Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref input.item);
            }
        }

        public class GenericConverterDec : Dec.Dec
        {
            public GenericClass<int> genericInt;
            public GenericClass<string> genericString;
        }

        [Test]
        public void Generic([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(GenericConverterDec) }, explicitConverters = new Type[] { typeof(GenericConverter<>) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <GenericConverterDec decName=""TestDec"">
                        <genericInt>42</genericInt>
                        <genericString>hello</genericString>
                    </GenericConverterDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var dec = Dec.Database<GenericConverterDec>.Get("TestDec");
            Assert.IsNotNull(dec);
            Assert.AreEqual(42, dec.genericInt.item);
            Assert.AreEqual("hello", dec.genericString.item);
        }
    }
}
