namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xml.Linq;

    [TestFixture]
    public class TypeSerialization : Base
    {
        private Func<Type, string> serializeType;
        private Func<string, Type> parseType;

        [OneTimeSetUp]
        public void CreateCallbacks()
        {
            var reflectionClass = Assembly.GetAssembly(typeof(Def.Def)).GetType("Def.UtilReflection");

            var serialize = reflectionClass.GetMethod("ToStringDefFormatted", BindingFlags.NonPublic | BindingFlags.Static);
            serializeType = type => (string)serialize.Invoke(null, new object[] { type });

            var parse = reflectionClass.GetMethod("ParseTypeDefFormatted", BindingFlags.NonPublic | BindingFlags.Static);
            parseType = str => (Type)parse.Invoke(null, new object[] { str, "XXX", -1 });
        }

        [SetUp]
        public void InitEnvironment()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();
        }

        public void TypeConversionBidirectional(Type type, string str)
        {
            Assert.AreEqual(str, serializeType(type));
            Assert.AreEqual(type, parseType(str));
        }

        public void TypeConversionOpaque(Type type)
        {
            Assert.AreEqual(type, parseType(serializeType(type)));
        }

        [Test]
	    public void Primitives()
	    {
            TypeConversionBidirectional(typeof(int), "System.Int32");
            TypeConversionBidirectional(typeof(float), "System.Single");
            TypeConversionBidirectional(typeof(char), "System.Char");
            TypeConversionBidirectional(typeof(string), "System.String");
	    }

        [Test]
        public void DefName()
        {
            TypeConversionBidirectional(typeof(Def.Def), "Def.Def");
            Assert.AreEqual(typeof(Def.Def), parseType("Def"));
        }

        [Test]
        public void OutsideDef()
        {
            Assert.AreEqual(typeof(Meta), parseType("Meta"));
            Assert.AreEqual(typeof(TypeSerialization), parseType("TypeSerialization"));
        }

        [Test]
        public void System()
        {
            Assert.AreEqual(typeof(XDocument), parseType("XDocument"));
        }

        [Test]
        public void Missing()
        {
            ExpectErrors(() => Assert.IsNull(parseType("Qwijibo")));
        }

        [Test]
        public void Overloaded()
        {
            ExpectErrors(() => Assert.IsNotNull(parseType("File")));
        }

        [Test]
        public void Generic()
        {
            TypeConversionOpaque(typeof(List<int>));
            TypeConversionOpaque(typeof(List<List<int>>));
            TypeConversionOpaque(typeof(List<float>));
            TypeConversionOpaque(typeof(Dictionary<List<float>, List<int>>));
        }

        [Test]
        public void GenericDef()
        {
            TypeConversionOpaque(typeof(List<Def.Def>));
        }

        [Test]
        public void GenericOutsideDef()
        {
            TypeConversionOpaque(typeof(List<Meta>));
        }

        [Test]
        public void GenericSystem()
        {
            TypeConversionOpaque(typeof(List<XDocument>));
        }

        [Test]
        public void GenericMixed()
        {
            TypeConversionOpaque(typeof(Dictionary<Def.Def, Meta>));
        }
    }
}
