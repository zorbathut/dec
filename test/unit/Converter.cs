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
        public class ConverterPrivate : Dec.Converter
        {
            private ConverterPrivate() { }

            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(Converter) };
            }
        }

        [Test]
        public void Private()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConverterPrivate) } };

            Dec.Parser parser = new Dec.Parser();
            parser.Finish();
        }

        public class ConverterParameter : Dec.Converter
        {
            public ConverterParameter(int x) { }

            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(Converter) };
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
        public void MissingTypeError([Values] BehaviorMode mode)
        {
            object cmp = new MissingComposer();
            ExpectErrors(() => Dec.Recorder.Write(cmp), err => err.Contains("MissingComposer"));
        }

        public class BaseType { }
        public class DerivedType : BaseType { }

        public class DerivedConverter : Dec.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type> { typeof(DerivedType) };
            }

            public override object Record(object model, Type type, Dec.Recorder recorder)
            {
                return model ?? new DerivedType();
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
