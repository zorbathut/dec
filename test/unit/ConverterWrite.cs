namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

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

        public class ConvertedConverterSimple : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(Converted) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                var match = Regex.Match(input, "(-?[0-9]+) (-?[0-9]+) (-?[0-9]+)");
                return new Converted { a = int.Parse(match.Groups[1].Value), b = int.Parse(match.Groups[2].Value), c = int.Parse(match.Groups[3].Value) };
            }

            public override string ToString(object input)
            {
                var converted = input as Converted;
                return $"{converted.a} {converted.b} {converted.c}";
            }
        }

        [Test]
        public void ConverterSimple([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterSimple) } };

            var parser = new Dec.Parser();
            parser.Finish();

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

        public class ConvertedConverterRecord : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(Converted) };
            }

            public override object Record(object model, Type type, Dec.Recorder recorder)
            {
                var converted = model as Converted ?? new Converted();

                recorder.Record(ref converted.a, "a");
                recorder.Record(ref converted.b, "b");
                recorder.Record(ref converted.c, "c");

                return converted;
            }
        }

        [Test]
        public void ConverterRecord([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterRecord) } };

            var parser = new Dec.Parser();
            parser.Finish();

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
                record.Record(ref convertibleA, "convertibleA");
                record.Record(ref convertibleB, "convertibleB");
            }
        }

        [Test]
        public void ConverterReplacementDetection([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterSimple) } };

            var parser = new Dec.Parser();
            parser.Finish();

            var converted = new ConverterReplacementRecordable();
            converted.convertibleA = new Converted();
            converted.convertibleB = converted.convertibleA;

            var deserialized = DoRecorderRoundTrip(converted, mode, expectReadErrors: true);

            Assert.IsNotNull(deserialized);

            // no guarantees on what exactly they contain, though!
            Assert.IsNotNull(deserialized.convertibleA);
            Assert.IsNotNull(deserialized.convertibleB);
        }

        [Test]
        public void ConverterReplacementWorking([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverterRecord) } };

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

        public class ConverterUnsuppliedClass
        {
            public int x;
        }

        public class ConverterUnsuppliedConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(ConverterUnsuppliedClass) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new ConverterUnsuppliedClass();
            }

            // whoops we forgot to write a ToString function! how silly
        }

        [Test]
        public void ConverterUnsupplied([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConverterUnsuppliedConverter) } };

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new ConverterUnsuppliedClass();

            root.x = 42;

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteErrors: true);

            Assert.IsNotNull(deserialized); // even if we don't know how to store it and deserialize it, we should at least be able to create it
        }
    }
}
