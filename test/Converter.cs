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

            Dec.Parser parser = null;
            ExpectErrors(() => parser = new Dec.Parser());
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
    }
}
