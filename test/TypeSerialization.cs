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
            string serialized = serializeType(type);
            Type parsed = parseType(serialized);

            Assert.AreEqual(type, parsed);
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
        public void GenericContainer()
        {
            Def.Config.UsingNamespaces = new string[] { "System.Collections.Generic" };

            TypeConversionBidirectional(typeof(List<int>), "List<int>");
            TypeConversionBidirectional(typeof(List<List<int>>), "List<List<int>>");
            TypeConversionBidirectional(typeof(List<float>), "List<float>");
            TypeConversionBidirectional(typeof(Dictionary<List<float>, List<int>>), "Dictionary<List<float>, List<int>>");
        }

        [Test]
        public void GenericDef()
        {
            Def.Config.UsingNamespaces = new string[] { "System.Collections.Generic" };

            TypeConversionBidirectional(typeof(List<Def.Def>), "List<Def.Def>");
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

        public class NestedA
        {
            public class NestedB
            {
                public class NestedC
                {
                    public class NestedD
                    {
                    }
                }
            }
        }

        [Test]
        public void Nesting()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(NestedA.NestedB.NestedC.NestedD), "NestedA.NestedB.NestedC.NestedD");
        }

        public class Generic<T>
        {
            public class NestedStandard
            {

            }

            public class NestedGeneric<U>
            {

            }
        }

        public class Generic2Param<T, U>
        {
        }

        [Test]
        public void GenericSimple()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<int>), "Generic<int>");

            Assert.AreEqual(typeof(Generic<int>), parseType("Generic< int>"));
            Assert.AreEqual(typeof(Generic<int>), parseType("Generic<int >"));
            Assert.AreEqual(typeof(Generic<int>), parseType("Generic< int >"));
        }

        [Test]
        public void GenericMultiple()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic2Param<int, double>), "Generic2Param<int, double>");
            Assert.AreEqual(typeof(Generic2Param<int, double>), parseType("Generic2Param<int,double>"));
        }

        /*
        // These currently don't work because nested generics turn out to not function like I expected.
        // I'm gonna worry about this later - I don't know if anyone will *ever* use this functionality.
        [Test]
        public void GenericNestedSimple()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<int>.NestedStandard), "Generic<int>.NestedStandard");
        }

        [Test]
        public void GenericNestedGeneric()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<int>.NestedGeneric<double>), "Generic<int>.NestedGeneric<double>");
        }*/

        [Test]
        public void GenericRecursive()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<Generic<Generic<int>>>), "Generic<Generic<Generic<int>>>");
        }

        [Test]
        public void GenericErrors()
        {
            Def.Config.UsingNamespaces = new string[] { "DefTest.TypeSerialization" };

            // This is just me verifying a bunch of template error behaviors.
            ExpectErrors(() => parseType("int<int>"));
            ExpectErrors(() => parseType("Generic<>"));
            ExpectErrors(() => parseType("Generic<int"));
            ExpectErrors(() => parseType("Generic<int, int>"));
            ExpectErrors(() => parseType("Generic<int><int>"));
            ExpectErrors(() => parseType("Generic<int>>"));
            ExpectErrors(() => parseType("Generic<int>."));
            ExpectErrors(() => parseType(".Generic<int>"));
            ExpectErrors(() => parseType("Generic<int>NestedStandard"));
            ExpectErrors(() => parseType("Generic<int>..NestedStandard"));

            ExpectErrors(() => parseType(""));
            ExpectErrors(() => parseType("."));
            ExpectErrors(() => parseType("<"));
            ExpectErrors(() => parseType(">"));
            ExpectErrors(() => parseType("<>"));
        }
    }
}
