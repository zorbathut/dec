using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Converter : Base
    {
        public class ConverterPrivate : Dec.ConverterRecord<Stub>
        {
            private ConverterPrivate() { }

            public override void Record(ref Stub input, Dec.Recorder recorder)
            {

            }
        }

        [Test]
        public void Private()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConverterPrivate) } });

            Dec.Parser parser = new Dec.Parser();
            parser.Finish();
        }

        public class ConverterParameter : Dec.ConverterRecord<Stub>
        {
            public ConverterParameter(int x) { }

            public override void Record(ref Stub input, Dec.Recorder recorder)
            {

            }
        }

        [Test]
        public void Parameter()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConverterParameter) } });

            Dec.Parser parser = null;
            ExpectErrors(() => parser = new Dec.Parser());
            parser.Finish();
        }

        public class MissingComposer { }

        [Test]
        public void MissingTypeError([Values] ParserMode mode)
        {
            object cmp = new MissingComposer();
            ExpectErrors(() => Dec.Recorder.Write(cmp), errorValidator: err => err.Contains("MissingComposer"));
        }

        public class BaseType { }
        public class DerivedType : BaseType { }

        public class DerivedConverter : Dec.ConverterRecord<DerivedType>
        {
            public override void Record(ref DerivedType input, Dec.Recorder recorder)
            {

            }
        }

        [Test]
        public void DerivedConverterTest([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(DerivedConverter) } });

            // we're only doing this to kick off the converter init
            new Dec.Parser().Finish();

            BaseType root = new DerivedType();
            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.GetType(), deserialized.GetType());
        }

        public class RegenericedConverter<T> : Dec.ConverterRecord<T>
        {
            public override void Record(ref T input, Dec.Recorder recorder)
            {

            }
        }

        [Test]
        public void Regenericed()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(RegenericedConverter<>) } });

            // so what happens here?
            ExpectErrors(() => new Dec.Parser().Finish());
        }

        public abstract class AbstractConverter : Dec.ConverterRecord<Stub>
        {

        }

        [Test]
        public void Abstract()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(AbstractConverter) } });

            // so what happens here?
            ExpectErrors(() => new Dec.Parser().Finish());
        }

        public struct Number
        {
            public int x;
        }

        public class NumberConverterString : Dec.ConverterString<Number>
        {
            public override string Write(Number input)
            {
                return input.x.ToString();
            }

            public override Number Read(string input, Dec.InputContext context)
            {
                return new Number { x = int.Parse(input) };
            }
        }

        public class NumberConverterRecord : Dec.ConverterRecord<Number>
        {
            public override void Record(ref Number input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.x, "x");
            }
        }

        public class NumberConverterFactory : Dec.ConverterFactory<Number>
        {
            public override void Write(Number input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.x, "x");
            }

            public override Number Create(Dec.Recorder recorder)
            {
                return new Number { };
            }

            public override void Read(ref Number input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.x, "x");
            }
        }

        public enum NumberConverterType
        {
            String,
            Record,
            Factory,
        }

        private Type GetConverterType(NumberConverterType type)
        {
            switch (type)
            {
                case NumberConverterType.String:
                    return typeof(NumberConverterString);
                case NumberConverterType.Record:
                    return typeof(NumberConverterRecord);
                case NumberConverterType.Factory:
                    return typeof(NumberConverterFactory);
                default:
                    throw new System.ArgumentException();
            }
        }

        [Test]
        public void NumberConverterTest([ValuesExcept(RecorderMode.Validation)] RecorderMode mode, [Values] NumberConverterType type)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { GetConverterType(type) } });

            Number root = new Number { x = 42 };
            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.x, deserialized.x);
        }

        public class NumberDec : Dec.Dec
        {
            public Number n;
        }

        [Test]
        public void UnusedField([Values] bool factory)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(NumberDec) }, explicitConverters = new Type[]{ factory ? typeof(NumberConverterFactory) : typeof(NumberConverterRecord) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NumberDec decName=""TestDecA"">
                        <n><InvalidField>6</InvalidField></n>
                    </NumberDec>
                </Decs>");
            ExpectWarnings(() => parser.Finish(), warningValidator: err => err.Contains("InvalidField"));
        }

        public class NumberNullableDec : Dec.Dec
        {
            public Number? a;
            public Number? b;
        }

        [Test]
        public void Nullable([ValuesExcept(ParserMode.Validation)] ParserMode mode, [Values] NumberConverterType type)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NumberNullableDec) }, explicitConverters = new Type[] { GetConverterType(type) }});

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NumberNullableDec decName=""TestDec"">
                        <a>" + (type == NumberConverterType.String ? "42" : "<x>42</x>") + @"</a>
                    </NumberNullableDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<NumberNullableDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsTrue(result.a.HasValue);
            Assert.AreEqual(42, result.a.Value.x);
            Assert.IsFalse(result.b.HasValue);
        }
    }
}
