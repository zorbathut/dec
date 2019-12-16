namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    [TestFixture]
    public class ConverterWrite : Base
    {
        public class ConverterRecordable : Def.IRecordable
        {
            public Converted convertable;

            public void Record(Def.Recorder record)
            {
                record.Record(ref convertable, "convertable");
            }
        }

        public class Converted
        {
            public int a;
            public int b;
            public int c;
        }

        public class ConvertedConverterSimple : Def.Converter
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
        public void ConverterSimple()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { }, explicitConverters: new Type[] { typeof(ConvertedConverterSimple) });
            parser.Finish();

            var converted = new ConverterRecordable();
            converted.convertable = new Converted();
            converted.convertable.a = 42;
            converted.convertable.b = 1234;
            converted.convertable.c = -40;

            string serialized = Def.Recorder.Write(converted, pretty: true);
            var deserialized = Def.Recorder.Read<ConverterRecordable>(serialized);

            Assert.AreEqual(converted.convertable.a, deserialized.convertable.a);
            Assert.AreEqual(converted.convertable.b, deserialized.convertable.b);
            Assert.AreEqual(converted.convertable.c, deserialized.convertable.c);
        }

        public class ConvertedConverterRecord : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(Converted) };
            }

            public override object Record(object model, Type type, Def.Recorder recorder)
            {
                var converted = model as Converted ?? new Converted();

                recorder.Record(ref converted.a, "a");
                recorder.Record(ref converted.b, "b");
                recorder.Record(ref converted.c, "c");

                return converted;
            }
        }

        [Test]
        public void ConverterRecord()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { }, explicitConverters: new Type[] { typeof(ConvertedConverterRecord) });
            parser.Finish();

            var converted = new ConverterRecordable();
            converted.convertable = new Converted();
            converted.convertable.a = 42;
            converted.convertable.b = 1234;
            converted.convertable.c = -40;

            string serialized = Def.Recorder.Write(converted, pretty: true);
            var deserialized = Def.Recorder.Read<ConverterRecordable>(serialized);

            Assert.AreEqual(converted.convertable.a, deserialized.convertable.a);
            Assert.AreEqual(converted.convertable.b, deserialized.convertable.b);
            Assert.AreEqual(converted.convertable.c, deserialized.convertable.c);
        }

        public class ConverterReplacementRecordable : Def.IRecordable
        {
            public Converted convertableA;
            public Converted convertableB;

            public void Record(Def.Recorder record)
            {
                record.Record(ref convertableA, "convertableA");
                record.Record(ref convertableB, "convertableB");
            }
        }

        [Test]
        public void ConverterReplacementDetection()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { }, explicitConverters: new Type[] { typeof(ConvertedConverterSimple) });
            parser.Finish();

            var converted = new ConverterReplacementRecordable();
            converted.convertableA = new Converted();
            converted.convertableB = converted.convertableA;

            string serialized = Def.Recorder.Write(converted, pretty: true);
            ConverterReplacementRecordable deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<ConverterReplacementRecordable>(serialized));

            Assert.IsNotNull(deserialized);

            // no guarantees on what exactly they contain, though!
            Assert.IsNotNull(deserialized.convertableA);
            Assert.IsNotNull(deserialized.convertableB);
        }

        [Test]
        public void ConverterReplacementWorking()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { }, explicitConverters: new Type[] { typeof(ConvertedConverterRecord) });
            parser.Finish();

            var converted = new ConverterReplacementRecordable();
            converted.convertableA = new Converted();
            converted.convertableB = converted.convertableA;

            string serialized = Def.Recorder.Write(converted, pretty: true);
            ConverterReplacementRecordable deserialized = Def.Recorder.Read<ConverterReplacementRecordable>(serialized);

            Assert.IsNotNull(deserialized);

            // no guarantees on what exactly they contain, though!
            Assert.IsNotNull(deserialized.convertableA);
            Assert.IsNotNull(deserialized.convertableB);
        }

        public class ConverterUnsuppliedClass
        {
            public int x;
        }

        public class ConverterUnsuppliedConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(ConverterUnsuppliedClass) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new ConverterUnsuppliedClass();
            }

            // whoops we forgot to write a ToString function
        }

        [Test]
        public void ConverterUnsupplied()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitConverters: new Type[] { typeof(ConverterUnsuppliedConverter) });
            parser.Finish();

            var root = new ConverterUnsuppliedClass();

            root.x = 42;

            string serialized = null;
            ExpectErrors(() => serialized = Def.Recorder.Write(root, pretty: true));
            var deserialized = Def.Recorder.Read<ConverterUnsuppliedClass>(serialized);

            Assert.IsNotNull(deserialized); // even if we don't know how to store it and deserialize it, we should at least be able to create it
        }
    }
}
