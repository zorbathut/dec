namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    public class WithinNamespace
    {
        public class NestedClass
        {

        }
    }

    [TestFixture]
    public class TypeSerialization : Base
    {
        private Func<Type, string> serializeType;
        private Func<string, Type> parseType;

        [OneTimeSetUp]
        public void CreateCallbacks()
        {
            var reflectionClass = Assembly.GetAssembly(typeof(Def.Def)).GetType("Def.UtilType");

            var serialize = reflectionClass.GetMethod("ComposeDefFormatted", BindingFlags.NonPublic | BindingFlags.Static);
            serializeType = type => (string)serialize.Invoke(null, new object[] { type });

            var parse = reflectionClass.GetMethod("ParseDefFormatted", BindingFlags.NonPublic | BindingFlags.Static);
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
            string serialized = serializeType(type);
            Type parsed = parseType(str);

            Assert.AreEqual(str, serialized);
            Assert.AreEqual(type, parsed);
        }

        public void TypeConversionOpaque(Type type)
        {
            Assert.AreEqual(type, parseType(serializeType(type)));
        }

        [Test]
	    public void Primitives()
	    {
            TypeConversionBidirectional(typeof(int), "int");
            TypeConversionBidirectional(typeof(float), "float");
            TypeConversionBidirectional(typeof(char), "char");
            TypeConversionBidirectional(typeof(string), "string");

            Assert.AreEqual(typeof(int), parseType("System.Int32"));
            Assert.AreEqual(typeof(float), parseType("System.Single"));
            Assert.AreEqual(typeof(char), parseType("System.Char"));
            Assert.AreEqual(typeof(string), parseType("System.String"));
        }

        [Test]
        public void DefName()
        {
            TypeConversionBidirectional(typeof(Def.Def), "Def.Def");
        }

        [Test]
        public void OutsideDef()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest" };

            TypeConversionBidirectional(typeof(Meta), "Meta");
            TypeConversionBidirectional(typeof(TypeSerialization), "TypeSerialization");
        }

        [Test]
        public void System()
        {
            Assert.AreEqual(typeof(XDocument), parseType("System.Xml.Linq.XDocument"));
        }

        [Test]
        public void Missing()
        {
            ExpectErrors(() => Assert.IsNull(parseType("Qwijibo")));
        }

        [Test]
        public void Overloaded()
        {
            // I'm not really guaranteed any of these besides System.IO, but this way at least I've got a good shot.
            Def.Config.UsingNamespaces = new string[] { "System.IO", "System.Internal", "NUnit.VisualStudio.TestAdapter.Dump" };

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

        public class WithinClass { }

        [Test]
        public void UsingNonexistent()
        {
            TypeConversionBidirectional(typeof(WithinNamespace), "DefTest.WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "DefTest.WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "DefTest.TypeSerialization.WithinClass");
        }

        [Test]
        public void UsingPartial()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest" };

            TypeConversionBidirectional(typeof(WithinNamespace), "WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "TypeSerialization.WithinClass");
        }

        [Test]
        public void UsingLeapfrog()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(WithinNamespace), "DefTest.WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "DefTest.WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "WithinClass");
        }

        [Test]
        public void UsingExists()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest", "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(WithinNamespace), "WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "WithinClass");

            // Fully specified always has to work
            Assert.AreEqual(typeof(WithinNamespace), parseType("DefTest.WithinNamespace"));
            Assert.AreEqual(typeof(WithinNamespace.NestedClass), parseType("DefTest.WithinNamespace.NestedClass"));
            Assert.AreEqual(typeof(WithinClass), parseType("DefTest.TypeSerialization.WithinClass"));
        }

    }
}
