namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

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

        [Test]
        public void NumberConverterStringTest([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(NumberConverterString) } });

            Number root = new Number { x = 42 };
            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.x, deserialized.x);
        }

        public class NumberConverterRecord : Dec.ConverterRecord<Number>
        {
            public override void Record(ref Number input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.x, "x");
            }
        }

        [Test]
        public void NumberConverterRecordTest([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(NumberConverterRecord) } });

            Number root = new Number { x = 42 };
            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.x, deserialized.x);
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

        [Test]
        public void NumberConverterFactoryTest([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(NumberConverterFactory) } });

            Number root = new Number { x = 42 };
            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.x, deserialized.x);
        }
    }
}
