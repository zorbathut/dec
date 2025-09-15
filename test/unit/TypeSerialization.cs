using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;

namespace DecTest
{
    namespace OverloadedSampleA { public class TooManyOfThese { } }
    namespace OverloadedSampleB { public class TooManyOfThese { } }

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
            var contextConstructor =
                typeof(Dec.Context)
                .GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(string), typeof(System.Xml.Linq.XElement), typeof(Dec.Path) },
                    null);
            var context = contextConstructor.Invoke(new object[] { "(testing)", null, null });

            var reflectionClass = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilType");

            var serialize = reflectionClass.GetMethod("ComposeDecFormatted", BindingFlags.NonPublic | BindingFlags.Static);
            serializeType = type => (string)serialize.Invoke(null, new object[] { type });

            var parse = reflectionClass.GetMethod("ParseDecFormatted", BindingFlags.NonPublic | BindingFlags.Static);
            parseType = str => (Type)parse.Invoke(null, new object[] { str, context });
        }

        [SetUp]
        public void InitEnvironment()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

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
            TypeConversionBidirectional(typeof(bool), "bool");
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
        public void PrimitiveArray()
        {
            TypeConversionBidirectional(typeof(int[]), "int[]");
            TypeConversionBidirectional(typeof(int[,]), "int[,]");
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
            Dec.Config.UsingNamespaces = new string[] { "DecTest.OverloadedSampleA", "DecTest.OverloadedSampleB" };

            ExpectErrors(() => Assert.IsNotNull(parseType("TooManyOfThese")));
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

            // this specifically tests for spaces in parameter names that are not the trailing parameter
            TypeConversionBidirectional(typeof(Func<int, string, double, bool>), "System.Func<int, string, double, bool>");
        }

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
        }

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

            // This is just me verifying a bunch of generic error behaviors.
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
        public void GenericWithMultirankArray()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<System.Type[,]>), "Generic<System.Type[,]>");
        }

        [Test]
        public void GenericWithPrimitiveArray()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            TypeConversionBidirectional(typeof(Generic<int[]>), "Generic<int[]>");
        }

        [Test]
        public void ErrorCache()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };

            // make sure caching doesn't suppress errors
            ExpectErrors(() => parseType("horse"));
            ExpectErrors(() => parseType("horse"));
        }

        [Test]
        public void OverloadedNames()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.OverloadedNames" };

            TypeConversionBidirectional(typeof(OverloadedNames.Foo.Solo), "Foo.Solo");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo.Generic<int>), "Foo.Generic<int>");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo.Overloaded), "Foo.Overloaded");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo.Overloaded<string>), "Foo.Overloaded<string>");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo.Overloaded<int, string>), "Foo.Overloaded<int, string>");

            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int>.Solo), "Foo<int>.Solo");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int>.Generic<double>), "Foo<int>.Generic<double>");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int>.Overloaded), "Foo<int>.Overloaded");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int>.Overloaded<char>), "Foo<int>.Overloaded<char>");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int>.Overloaded<double, char>), "Foo<int>.Overloaded<double, char>");

            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int, double>.Solo), "Foo<int, double>.Solo");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int, double>.Generic<string>), "Foo<int, double>.Generic<string>");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int, double>.Overloaded), "Foo<int, double>.Overloaded");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int, double>.Overloaded<float>), "Foo<int, double>.Overloaded<float>");
            TypeConversionBidirectional(typeof(OverloadedNames.Foo<int, double>.Overloaded<string, float>), "Foo<int, double>.Overloaded<string, float>");

            TypeConversionBidirectional(typeof(OverloadedNames.Foo<Dictionary<int, double>, double>.Overloaded<string, Dictionary<int, double>>), "Foo<System.Collections.Generic.Dictionary<int, double>, double>.Overloaded<string, System.Collections.Generic.Dictionary<int, double>>");
        }

        [Test]
        public void BracketReplacement()
        {
            Assert.AreEqual(typeof(System.Collections.Generic.List<int>), parseType("System.Collections.Generic.List{int}"));
        }

        [Test]
        public void ArrayCachingError()
        {
            TypeConversionBidirectional(typeof(Base.Stub[]), "DecTest.Base.Stub[]");
            TypeConversionBidirectional(typeof(Base.Stub), "DecTest.Base.Stub");
        }

        [Test]
        public void CompatTypeLookupBasic()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldTypeName", typeof(int) },
                { "AnotherOldName", typeof(string) }
            };

            Assert.AreEqual(typeof(int), parseType("OldTypeName"));
            Assert.AreEqual(typeof(string), parseType("AnotherOldName"));

            // Should still work for non-mapped types
            Assert.AreEqual(typeof(float), parseType("float"));
        }

        [Test]
        public void CompatTypeLookupArray()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldArrayType", typeof(int) }
            };

            // Test that array notation works with compat lookup
            Assert.AreEqual(typeof(int[]), parseType("OldArrayType[]"));
            Assert.AreEqual(typeof(int[,]), parseType("OldArrayType[,]"));
            Assert.AreEqual(typeof(int[,,]), parseType("OldArrayType[,,]"));
        }

        [Test]
        public void CompatTypeLookupGeneric()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldListType", typeof(System.Collections.Generic.List<>) }
            };

            // Test that generic parameters work with compat lookup
            Assert.AreEqual(typeof(System.Collections.Generic.List<int>), parseType("OldListType<int>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<string>), parseType("OldListType<string>"));
        }

        [Test]
        public void CompatTypeLookupPriority()
        {
            // Create a scenario where compat lookup should override normal resolution
            Dec.Config.UsingNamespaces = new string[] { "System" };
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "String", typeof(int) } // Map "String" to int instead of string
            };

            // CompatTypeLookup should take priority over normal type resolution
            Assert.AreEqual(typeof(int), parseType("String"));

            // But full qualification should still work normally
            Assert.AreEqual(typeof(string), parseType("System.String"));
        }

        [Test]
        public void CompatTypeLookupCacheClearing()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest" };

            // First, parse a type normally to get it in cache
            Assert.AreEqual(typeof(Meta), parseType("Meta"));

            // Now set up compat lookup that would change the result
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "Meta", typeof(string) }
            };

            // The cache should have been cleared when we set CompatTypeLookup,
            // so this should return string (from compat lookup) not Meta (from cache)
            Assert.AreEqual(typeof(string), parseType("Meta"));
        }

        [Test]
        public void CompatTypeLookupNull()
        {
            // Test that setting CompatTypeLookup to null works properly
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "TestType", typeof(int) }
            };

            Assert.AreEqual(typeof(int), parseType("TestType"));

            // Clear the lookup
            Dec.Config.CompatTypeLookup = null;

            // Should now fail to find the type
            ExpectErrors(() => Assert.IsNull(parseType("TestType")));
        }

        [Test]
        public void CompatTypeLookupWithArraysInDict()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldType[]", typeof(string[]) }, // Direct mapping of array type name
                { "OldBaseType", typeof(float) }   // Base type that can have arrays applied
            };

            // Direct array mapping should work
            Assert.AreEqual(typeof(string[]), parseType("OldType[]"));

            // Base type with applied array should work
            Assert.AreEqual(typeof(float[]), parseType("OldBaseType[]"));
        }

        [Test]
        public void CompatTypeLookupGenericComplex()
        {
            Dec.Config.UsingNamespaces = new string[] { "System.Collections.Generic" };
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldDictionary", typeof(System.Collections.Generic.Dictionary<,>) },
                { "OldList", typeof(System.Collections.Generic.List<>) }
            };

            // Test complex generic combinations
            Assert.AreEqual(typeof(Dictionary<int, string>), parseType("OldDictionary<int, string>"));
            Assert.AreEqual(typeof(Dictionary<List<int>, string>), parseType("OldDictionary<OldList<int>, string>"));
            Assert.AreEqual(typeof(List<Dictionary<string, int>>), parseType("OldList<OldDictionary<string, int>>"));
        }

        [Test]
        public void CompatTypeLookupGenericNested()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.TypeSerialization" };
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldGeneric", typeof(Generic<>) },
                { "OldGeneric2", typeof(Generic2Param<,>) }
            };

            // Test nested generics with compat lookup
            Assert.AreEqual(typeof(Generic<int>), parseType("OldGeneric<int>"));
            Assert.AreEqual(typeof(Generic2Param<string, double>), parseType("OldGeneric2<string, double>"));
            Assert.AreEqual(typeof(Generic<Generic<int>>), parseType("OldGeneric<OldGeneric<int>>"));

            // Test mixing old and new names
            Assert.AreEqual(typeof(Generic<Generic2Param<int, string>>), parseType("OldGeneric<OldGeneric2<int, string>>"));
        }

        [Test]
        public void CompatTypeLookupGenericWithArrays()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldList", typeof(System.Collections.Generic.List<>) }
            };

            // Test generic types with arrays
            Assert.AreEqual(typeof(System.Collections.Generic.List<int>[]), parseType("OldList<int>[]"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<int[]>), parseType("OldList<int[]>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<string>[,]), parseType("OldList<string>[,]"));
        }

        [Test]
        public void CompatTypeLookupGenericPartialMatch()
        {
            Dec.Config.UsingNamespaces = new string[] { "System.Collections.Generic" };
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "System.Collections.Generic.List", typeof(System.Collections.Generic.Queue<>) }
            };

            // The compat lookup should override even when using full qualified names
            Assert.AreEqual(typeof(System.Collections.Generic.Queue<int>), parseType("System.Collections.Generic.List<int>"));

            // And when using namespaces
            Assert.AreEqual(typeof(System.Collections.Generic.Queue<string>), parseType("List<string>"));
        }

        [Test]
        public void CompatTypeLookupGenericWhitespace()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldList", typeof(System.Collections.Generic.List<>) }
            };

            // Test that whitespace handling still works with compat lookup
            Assert.AreEqual(typeof(System.Collections.Generic.List<int>), parseType("OldList< int>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<int>), parseType("OldList<int >"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<int>), parseType("OldList< int >"));
        }

        [Test]
        public void CompatTypeLookupGenericParameters()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldList", typeof(System.Collections.Generic.List<>) },
                { "OldString", typeof(string) },
                { "OldInt", typeof(int) },
                { "OldFloat", typeof(float) }
            };

            // Test that compat lookup works for generic parameters
            Assert.AreEqual(typeof(System.Collections.Generic.List<string>), parseType("OldList<OldString>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<int>), parseType("OldList<OldInt>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<float>), parseType("OldList<OldFloat>"));

            // Test mixing compat and normal types in parameters
            Assert.AreEqual(typeof(System.Collections.Generic.List<string>), parseType("OldList<string>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<string>), parseType("System.Collections.Generic.List<OldString>"));
        }

        [Test]
        public void CompatTypeLookupGenericParametersNested()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldList", typeof(System.Collections.Generic.List<>) },
                { "OldDict", typeof(System.Collections.Generic.Dictionary<,>) },
                { "OldString", typeof(string) },
                { "OldInt", typeof(int) }
            };

            // Test deeply nested compat lookups in generic parameters
            Assert.AreEqual(
                typeof(System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, int>>),
                parseType("OldList<OldDict<OldString, OldInt>>")
            );

            Assert.AreEqual(
                typeof(System.Collections.Generic.Dictionary<System.Collections.Generic.List<string>, int>),
                parseType("OldDict<OldList<OldString>, OldInt>")
            );
        }

        [Test]
        public void CompatTypeLookupGenericParametersWithArrays()
        {
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldList", typeof(System.Collections.Generic.List<>) },
                { "OldString", typeof(string) },
                { "OldInt", typeof(int) }
            };

            // Test compat lookup in generic parameters with arrays
            Assert.AreEqual(typeof(System.Collections.Generic.List<string[]>), parseType("OldList<OldString[]>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<int[,]>), parseType("OldList<OldInt[,]>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<string>[]), parseType("OldList<OldString>[]"));
        }

        [Test]
        public void CompatTypeLookupGenericParametersCustomTypes()
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest" };
            Dec.Config.CompatTypeLookup = new System.Collections.Generic.Dictionary<string, System.Type>
            {
                { "OldList", typeof(System.Collections.Generic.List<>) },
                { "OldMeta", typeof(Meta) },
                { "OldBase", typeof(Base) }
            };

            // Test compat lookup with custom types as generic parameters
            Assert.AreEqual(typeof(System.Collections.Generic.List<Meta>), parseType("OldList<OldMeta>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<Base>), parseType("OldList<OldBase>"));

            // Mix of compat and normal custom types
            Assert.AreEqual(typeof(System.Collections.Generic.List<Meta>), parseType("OldList<Meta>"));
            Assert.AreEqual(typeof(System.Collections.Generic.List<Meta>), parseType("System.Collections.Generic.List<OldMeta>"));
        }
    }

    namespace OverloadedNames
    {
        public class Foo
        {
            public class Solo { }
            public class Generic<U> { }

            public class Overloaded { }
            public class Overloaded<U> { }
            public class Overloaded<U, V> { }
        }
        public class Foo<T>
        {
            public class Solo { }
            public class Generic<U> { }

            public class Overloaded { }
            public class Overloaded<U> { }
            public class Overloaded<U, V> { }
        }
        public class Foo<T, U>
        {
            public class Solo { }
            public class Generic<K> { }

            public class Overloaded { }
            public class Overloaded<K> { }
            public class Overloaded<K, V> { }
        }
    }
}
