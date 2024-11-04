using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ConverterWrite : Base
    {
        public class ConverterRecordable : Dec.IRecordable
        {
            public Converted convertible;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref convertible, "convertible");
            }
        }

        public class Converted
        {
            public int a;
            public int b;
            public int c;
        }

        public class ConvertedConverterString : Dec.ConverterString<Converted>
        {
            public override Converted Read(string input, Dec.InputContext context)
            {
                var match = Regex.Match(input, "(-?[0-9]+) (-?[0-9]+) (-?[0-9]+)");
                return new Converted { a = int.Parse(match.Groups[1].Value), b = int.Parse(match.Groups[2].Value), c = int.Parse(match.Groups[3].Value) };
            }

            public override string Write(Converted input)
            {
                return $"{input.a} {input.b} {input.c}";
            }
        }

        [Test]
        public void ConverterString([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterString) } });

            var converted = new ConverterRecordable();
            converted.convertible = new Converted();
            converted.convertible.a = 42;
            converted.convertible.b = 1234;
            converted.convertible.c = -40;

            var deserialized = DoRecorderRoundTrip(converted, mode);

            Assert.AreEqual(converted.convertible.a, deserialized.convertible.a);
            Assert.AreEqual(converted.convertible.b, deserialized.convertible.b);
            Assert.AreEqual(converted.convertible.c, deserialized.convertible.c);
        }

        public class ConvertedConverterRecord : Dec.ConverterRecord<Converted>
        {
            public override void Record(ref Converted converted, Dec.Recorder recorder)
            {
                recorder.Record(ref converted.a, "a");
                recorder.Record(ref converted.b, "b");
                recorder.Record(ref converted.c, "c");
            }
        }

        [Test]
        public void ConverterRecord([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterRecord) } });

            var converted = new ConverterRecordable();
            converted.convertible = new Converted();
            converted.convertible.a = 42;
            converted.convertible.b = 1234;
            converted.convertible.c = -40;

            var deserialized = DoRecorderRoundTrip(converted, mode);

            Assert.AreEqual(converted.convertible.a, deserialized.convertible.a);
            Assert.AreEqual(converted.convertible.b, deserialized.convertible.b);
            Assert.AreEqual(converted.convertible.c, deserialized.convertible.c);
        }

        public class ConverterReplacementRecordable : Dec.IRecordable
        {
            public Converted convertibleA;
            public Converted convertibleB;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref convertibleA, "convertibleA");
                record.Shared().Record(ref convertibleB, "convertibleB");
            }
        }

        [Test]
        public void ConverterStringRef([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterString) } });

            var parser = new Dec.Parser();
            parser.Finish();

            var converted = new ConverterReplacementRecordable();
            converted.convertibleA = new Converted();
            converted.convertibleB = converted.convertibleA;

            converted.convertibleA.a = 42;
            converted.convertibleA.b = 1234;
            converted.convertibleA.c = -40;

            var deserialized = DoRecorderRoundTrip(converted, mode);

            Assert.IsNotNull(deserialized);

            Assert.AreEqual(converted.convertibleA.a, deserialized.convertibleA.a);
            Assert.AreEqual(converted.convertibleA.b, deserialized.convertibleA.b);
            Assert.AreEqual(converted.convertibleA.c, deserialized.convertibleA.c);

            // no guarantees on what exactly they contain, though!
            Assert.AreSame(deserialized.convertibleA, deserialized.convertibleB);
        }

        [Test]
        public void ConverterReplacementWorking([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterRecord) } });

            var parser = new Dec.Parser();
            parser.Finish();

            var converted = new ConverterReplacementRecordable();
            converted.convertibleA = new Converted();
            converted.convertibleB = converted.convertibleA;

            var deserialized = DoRecorderRoundTrip(converted, mode);

            Assert.IsNotNull(deserialized);

            // no guarantees on what exactly they contain, though!
            Assert.IsNotNull(deserialized.convertibleA);
            Assert.IsNotNull(deserialized.convertibleB);
        }
    }
}
