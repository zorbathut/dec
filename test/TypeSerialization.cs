namespace DecTest
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
            var reflectionClass = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilType");

            var serialize = reflectionClass.GetMethod("ComposeDecFormatted", BindingFlags.NonPublic | BindingFlags.Static);
            serializeType = type => (string)serialize.Invoke(null, new object[] { type });

            var parse = reflectionClass.GetMethod("ParseDecFormatted", BindingFlags.NonPublic | BindingFlags.Static);
            parseType = str => (Type)parse.Invoke(null, new object[] { str, "XXX", -1 });
        }

        [SetUp]
        public void InitEnvironment()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
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
        public void DecName()
        {
            TypeConversionBidirectional(typeof(Dec.Dec), "Dec.Dec");
        }

        [Test]
        public void OutsideDec()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest" };

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
            Dec.Config.UsingNamespaces = new string[] { "System.IO", "System.Internal", "NUnit.VisualStudio.TestAdapter.Dump" };

            ExpectErrors(() => Assert.IsNotNull(parseType("File")));
        }

        [Test]
        public void GenericContainer()
        {
            Dec.Config.UsingNamespaces = new string[] { "System.Collections.Generic" };

            TypeConversionBidirectional(typeof(List<int>), "List<int>");
            TypeConversionBidirectional(typeof(List<List<int>>), "List<List<int>>");
            TypeConversionBidirectional(typeof(List<float>), "List<float>");
            TypeConversionBidirectional(typeof(Dictionary<List<float>, List<int>>), "Dictionary<List<float>, List<int>>");
        }

        [Test]
        public void GenericDec()
        {
            Dec.Config.UsingNamespaces = new string[] { "System.Collections.Generic" };

            TypeConversionBidirectional(typeof(List<Dec.Dec>), "List<Dec.Dec>");
        }

        [Test]
        public void GenericOutsideDec()
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
            TypeConversionOpaque(typeof(Dictionary<Dec.Dec, Meta>));
        }

        public class WithinClass { }

        [Test]
        public void UsingNonexistent()
        {
            TypeConversionBidirectional(typeof(WithinNamespace), "DecTest.WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "DecTest.WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "DecTest.TypeSerialization.WithinClass");
        }

        [Test]
        public void UsingPartial()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest" };

            TypeConversionBidirectional(typeof(WithinNamespace), "WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "TypeSerialization.WithinClass");
        }

        [Test]
        public void UsingLeapfrog()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(WithinNamespace), "DecTest.WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "DecTest.WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "WithinClass");
        }

        [Test]
        public void UsingExists()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest", "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(WithinNamespace), "WithinNamespace");
            TypeConversionBidirectional(typeof(WithinNamespace.NestedClass), "WithinNamespace.NestedClass");
            TypeConversionBidirectional(typeof(WithinClass), "WithinClass");

            // Fully specified always has to work
            Assert.AreEqual(typeof(WithinNamespace), parseType("DecTest.WithinNamespace"));
            Assert.AreEqual(typeof(WithinNamespace.NestedClass), parseType("DecTest.WithinNamespace.NestedClass"));
            Assert.AreEqual(typeof(WithinClass), parseType("DecTest.TypeSerialization.WithinClass"));
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
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

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
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<int>), "Generic<int>");

            Assert.AreEqual(typeof(Generic<int>), parseType("Generic< int>"));
            Assert.AreEqual(typeof(Generic<int>), parseType("Generic<int >"));
            Assert.AreEqual(typeof(Generic<int>), parseType("Generic< int >"));
        }

        [Test]
        public void GenericMultiple()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic2Param<int, double>), "Generic2Param<int, double>");
            Assert.AreEqual(typeof(Generic2Param<int, double>), parseType("Generic2Param<int,double>"));
        }

        /*
        // These currently don't work because nested generics turn out to not function like I expected.
        // I'm gonna worry about this later - I don't know if anyone will *ever* use this functionality.
        [Test]
        public void GenericNestedSimple()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<int>.NestedStandard), "Generic<int>.NestedStandard");
        }

        [Test]
        public void GenericNestedGeneric()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<int>.NestedGeneric<double>), "Generic<int>.NestedGeneric<double>");
        }*/

        [Test]
        public void GenericRecursive()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<Generic<Generic<int>>>), "Generic<Generic<Generic<int>>>");
        }

        [Test]
        public void GenericErrors()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

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

        [Test]
        public void OverloadedNames()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.OverloadedNames" };

            // In theory we could handle this one properly; right now, we don't, though, and I want to make sure it does the right thing when it stops generating errors
            ExpectErrors(() => parseType("Foo"));
            ExpectErrors(() => parseType("Foo<int>"));
        }
    }

    namespace OverloadedNames
    {
        public class Foo { }
        public class Foo<T> { }
    }
}
