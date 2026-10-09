using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ConverterFields : Base
    {
        public class FieldsDec : Dec.Dec
        {
        }

        public class FieldsDerived : StubRecordableInt
        {
        }

        public struct FieldsPoint : Dec.IRecordable
        {
            public int x;
            public int y;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref x, nameof(x));
                recorder.Record(ref y, nameof(y));
            }
        }

        // Recorded only through FieldsTargetConverter.
        public class FieldsTarget
        {
            public int number;
            public string text;
            public FieldsPoint point;
            public StubRecordableInt nested;
            public object polymorphic;
            public List<int> list;
            public FieldsDec dec;
            public StubRecordable empty;
            public StubRecordable sharedFirst;
            public StubRecordable sharedSecond;
            public readonly int fixedValue;
            private int hidden;

            public FieldsTarget()
            {
            }

            public FieldsTarget(int fixedValue, int hidden)
            {
                this.fixedValue = fixedValue;
                this.hidden = hidden;
            }

            public int Hidden
            {
                get
                {
                    return hidden;
                }
            }
        }

        private static Dec.RecordField Field(Type type, string name, string label, bool shared = false)
        {
            return new Dec.RecordField(type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), label, shared);
        }

        private class FieldsTargetConverter : Dec.ConverterFactoryFields
        {
            public FieldsTargetConverter() : base(typeof(FieldsTarget), new[]
            {
                Field(typeof(FieldsTarget), nameof(FieldsTarget.number), "number"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.text), "text"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.point), "point"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.nested), "nested"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.polymorphic), "polymorphic"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.list), "list"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.dec), "dec"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.empty), "empty"),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.sharedFirst), "sharedFirst", shared: true),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.sharedSecond), "sharedSecond", shared: true),
                Field(typeof(FieldsTarget), nameof(FieldsTarget.fixedValue), "fixedValue"),
                Field(typeof(FieldsTarget), "hidden", "secret"),
            }, new[] { "legacy" })
            {
            }

            public override object Create(Dec.Recorder recorder)
            {
                return new FieldsTarget();
            }
        }

        // Recorded only through FieldsPairConverter.
        public struct FieldsPair
        {
            public int first;
            public string second;
        }

        private class FieldsPairConverter : Dec.ConverterFactoryFields
        {
            public FieldsPairConverter() : base(typeof(FieldsPair), new[]
            {
                Field(typeof(FieldsPair), nameof(FieldsPair.first), "first"),
                Field(typeof(FieldsPair), nameof(FieldsPair.second), "second"),
            })
            {
            }

            public override object Create(Dec.Recorder recorder)
            {
                return new FieldsPair();
            }
        }

        public class FieldsBase
        {
            private int inherited;

            public int Inherited
            {
                get
                {
                    return inherited;
                }
                set
                {
                    inherited = value;
                }
            }
        }

        // Recorded only through FieldsChildConverter.
        public class FieldsChild : FieldsBase
        {
            public int own;
        }

        private class FieldsChildConverter : Dec.ConverterFactoryFields
        {
            public FieldsChildConverter() : base(typeof(FieldsChild), new[]
            {
                Field(typeof(FieldsBase), "inherited", "inherited"),
                Field(typeof(FieldsChild), nameof(FieldsChild.own), "own"),
            })
            {
            }

            public override object Create(Dec.Recorder recorder)
            {
                return new FieldsChild();
            }
        }

        public class FieldsHolder : Dec.IRecordable
        {
            public FieldsTarget target;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref target, nameof(target));
            }
        }

        // Reading builds this first, so its target already exists, with a shared field set, when the recorded target is read into it.
        public class FieldsHolderPrefilled : Dec.IRecordable
        {
            public FieldsTarget target = new FieldsTarget { number = 99, sharedFirst = new StubRecordable() };

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref target, nameof(target));
            }
        }

        [SetUp]
        public void PrepConverters()
        {
            Dec.Config.ConverterFactory = type =>
            {
                if (type == typeof(FieldsTarget))
                {
                    return new FieldsTargetConverter();
                }

                if (type == typeof(FieldsPair))
                {
                    return new FieldsPairConverter();
                }

                if (type == typeof(FieldsChild))
                {
                    return new FieldsChildConverter();
                }

                return null;
            };

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(FieldsDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <FieldsDec decName=""Sample"" />
                </Decs>");
            parser.Finish();
        }

        [TearDown]
        public void CleanConverters()
        {
            Dec.Config.ConverterFactory = null;
        }

        private static FieldsTarget TargetBuild()
        {
            var shared = new StubRecordable();
            return new FieldsTarget(fixedValue: 17, hidden: 23)
            {
                number = 5,
                text = "words",
                point = new FieldsPoint { x = 3, y = 4 },
                nested = new StubRecordableInt { data = 8 },
                polymorphic = new FieldsDerived { data = 9 },
                list = new List<int> { 1, 2, 3 },
                dec = Dec.Database<FieldsDec>.Get("Sample"),
                sharedFirst = shared,
                sharedSecond = shared,
            };
        }

        private static void TargetAssertMatches(FieldsTarget expected, FieldsTarget actual)
        {
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected.number, actual.number);
            Assert.AreEqual(expected.text, actual.text);
            Assert.AreEqual(expected.point.x, actual.point.x);
            Assert.AreEqual(expected.point.y, actual.point.y);
            Assert.AreEqual(expected.nested.data, actual.nested.data);
            Assert.AreEqual(typeof(FieldsDerived), actual.polymorphic.GetType());
            Assert.AreEqual(((FieldsDerived)expected.polymorphic).data, ((FieldsDerived)actual.polymorphic).data);
            Assert.AreEqual(expected.list, actual.list);
            Assert.AreSame(expected.dec, actual.dec);
            Assert.IsNull(actual.empty);
            Assert.IsNotNull(actual.sharedFirst);
            Assert.AreSame(actual.sharedFirst, actual.sharedSecond);
            Assert.AreEqual(expected.fixedValue, actual.fixedValue);
            Assert.AreEqual(expected.Hidden, actual.Hidden);
        }

        [Test]
        public void RoundTrip([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode, [Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var original = TargetBuild();
            var result = DoRecorderRoundTrip(new FieldsHolder { target = original }, mode);

            TargetAssertMatches(original, result.target);
        }

        [Test]
        public void RoundTripStruct([ValuesExcept(RecorderMode.Validation)] RecorderMode mode, [Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var result = DoRecorderRoundTrip(new FieldsPair { first = 6, second = "pair" }, mode);

            Assert.AreEqual(6, result.first);
            Assert.AreEqual("pair", result.second);
        }

        [Test]
        public void RoundTripStructInList([ValuesExcept(RecorderMode.Validation)] RecorderMode mode, [Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var original = new List<FieldsPair> { new FieldsPair { first = 1, second = "one" }, new FieldsPair { first = 2, second = "two" } };
            var result = DoRecorderRoundTrip(original, mode);

            Assert.AreEqual(original, result);
        }

        [Test]
        public void RoundTripBaseField([ValuesExcept(RecorderMode.Validation)] RecorderMode mode, [Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var result = DoRecorderRoundTrip(new FieldsChild { Inherited = 7, own = 8 }, mode);

            Assert.AreEqual(7, result.Inherited);
            Assert.AreEqual(8, result.own);
        }

        [Test]
        public void ReadIntoExisting([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode, [Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var original = new FieldsHolderPrefilled { target = TargetBuild() };
            var result = DoRecorderRoundTrip(original, mode);

            TargetAssertMatches(original.target, result.target);
        }

        [Test]
        public void ReadMissingFieldIsDefault([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var result = Dec.Recorder.Read<FieldsHolderPrefilled>(@"
                <Record>
                    <recordFormatVersion>1</recordFormatVersion>
                    <data>
                        <target>
                            <text>words</text>
                        </target>
                    </data>
                </Record>");

            Assert.AreEqual("words", result.target.text);
            Assert.AreEqual(0, result.target.number);
            Assert.IsNull(result.target.sharedFirst);
        }

        [Test]
        public void ReadIgnoredLabel([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var result = Dec.Recorder.Read<FieldsHolder>(@"
                <Record>
                    <recordFormatVersion>1</recordFormatVersion>
                    <data>
                        <target>
                            <number>4</number>
                            <legacy>12</legacy>
                        </target>
                    </data>
                </Record>");

            Assert.AreEqual(4, result.target.number);
        }

        [Test]
        public void ReadUnknownLabel([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            FieldsHolder result = null;
            ExpectWarnings(() => result = Dec.Recorder.Read<FieldsHolder>(@"
                <Record>
                    <recordFormatVersion>1</recordFormatVersion>
                    <data>
                        <target>
                            <number>4</number>
                            <unknown>12</unknown>
                        </target>
                    </data>
                </Record>"), wrn => wrn.Contains("unknown"));

            Assert.AreEqual(4, result.target.number);
        }

        [Test]
        public void FallbackMatchesEmitted()
        {
            var original = new FieldsHolder { target = TargetBuild() };

            // Otherwise this would only compare the fallback with itself.
            Assert.IsTrue(((Dec.ConverterFactoryFields)Dec.Serialization.ConverterFor(typeof(FieldsTarget))).Emitted);

            Dec.Config.TestForceFallbackEmit = false;
            string writtenEmitted = Dec.Recorder.Write(original);
            ulong checksumEmitted = Dec.Recorder.Checksum(original);
            var clonedEmitted = Dec.Recorder.Clone(original);

            Dec.Config.TestForceFallbackEmit = true;
            string writtenFallback = Dec.Recorder.Write(original);
            ulong checksumFallback = Dec.Recorder.Checksum(original);
            var clonedFallback = Dec.Recorder.Clone(original);

            Assert.AreEqual(writtenEmitted, writtenFallback);
            Assert.AreEqual(checksumEmitted, checksumFallback);

            // Each path's clone, checked by the other path.
            Assert.AreEqual(checksumEmitted, Dec.Recorder.Checksum(clonedEmitted));
            Dec.Config.TestForceFallbackEmit = false;
            Assert.AreEqual(checksumEmitted, Dec.Recorder.Checksum(clonedFallback));
        }

        public class FieldsInvalid
        {
            public int valid;
            public static int shared;
        }

        private class FieldsInvalidConverter : Dec.ConverterFactoryFields
        {
            public FieldsInvalidConverter(Type type, Dec.RecordField[] fields, string[] ignored = null) : base(type, fields, ignored)
            {
            }

            public override object Create(Dec.Recorder recorder)
            {
                return new FieldsInvalid();
            }
        }

        private static readonly Dec.RecordField FieldValid = Field(typeof(FieldsInvalid), nameof(FieldsInvalid.valid), "valid");

        [Test]
        public void InvalidStatic()
        {
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid, new Dec.RecordField(typeof(FieldsInvalid).GetField(nameof(FieldsInvalid.shared)), "shared", false) }), err => err.Contains("static"));
        }

        [Test]
        public void InvalidForeignField()
        {
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid, Field(typeof(StubRecordableInt), nameof(StubRecordableInt.data), "data") }), err => err.Contains(nameof(StubRecordableInt)));
        }

        [Test]
        public void InvalidNullableField()
        {
            // Nullable<int> is assignable from int, but its fields aren't int's.
            ExpectErrors(() => new FieldsInvalidConverter(typeof(int), new[] { new Dec.RecordField(typeof(int?).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance), "value", false) }), err => err.Contains("declared on"));
        }

        [Test]
        public void InvalidPointer()
        {
            // The test assembly can't declare pointers itself, so borrow one of the runtime's.
            var pointerField = typeof(Pointer).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(field => field.FieldType.IsPointer);
            if (pointerField == null)
            {
                Assert.Inconclusive("System.Reflection.Pointer has no pointer field on this runtime");
            }

            ExpectErrors(() => new FieldsInvalidConverter(typeof(Pointer), new[] { new Dec.RecordField(pointerField, "pointer", false) }), err => err.Contains("pointer"));
        }

        [Test]
        public void InvalidIgnored()
        {
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid }, new string[] { null }), err => err.Contains("null"));
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid }, new[] { "legacy", "legacy" }), err => err.Contains("`legacy`"));
        }

        [Test]
        public void InvalidMissing()
        {
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid, new Dec.RecordField(null, "nothing", false) }), err => err.Contains("missing"));
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid, new Dec.RecordField(typeof(FieldsInvalid).GetField(nameof(FieldsInvalid.valid)), null, false) }), err => err.Contains("missing"));
        }

        [Test]
        public void InvalidDuplicateLabel()
        {
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid, FieldValid }), err => err.Contains("`valid`"));
            ExpectErrors(() => new FieldsInvalidConverter(typeof(FieldsInvalid), new[] { FieldValid }, new[] { "valid" }), err => err.Contains("`valid`"));
        }

        [Test]
        public void InvalidOpenGeneric()
        {
            ExpectErrors(() => new FieldsInvalidConverter(typeof(List<>), new Dec.RecordField[0]), err => err.Contains("generic"));
        }
    }
}
