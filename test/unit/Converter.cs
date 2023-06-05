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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConverterPrivate) } };

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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConverterParameter) } };

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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(DerivedConverter) } };

            // we're only doing this to kick off the converter init
            new Dec.Parser().Finish();

            BaseType root = new DerivedType();
            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.GetType(), deserialized.GetType());
        }
    }
}
