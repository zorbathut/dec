using NUnit.Framework;
using System;

namespace DecTest
{
    [TestFixture]
    public class StaticReferences : Base
    {
        [Dec.StaticReferences]
        public static class StaticReferenceDecs
        {
            static StaticReferenceDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDecA;
            public static StubDec TestDecB;
        }

        [Test]
        public void StaticReference([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) }, explicitStaticRefs = new Type[]{ typeof(StaticReferenceDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                    <StubDec decName=""TestDecB"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var resultA = Dec.Database<StubDec>.Get("TestDecA");
            var resultB = Dec.Database<StubDec>.Get("TestDecB");
            Assert.IsNotNull(resultA);
            Assert.IsNotNull(resultB);

            Assert.AreEqual(StaticReferenceDecs.TestDecA, resultA);
            Assert.AreEqual(StaticReferenceDecs.TestDecB, resultB);
        }

        public class StubDerivedDec : StubDec
        {

        }

        [Dec.StaticReferences]
        public static class PreciseClassDecs
        {
            static PreciseClassDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDerivedDec TestDec;
        }

        [Test]
        public void PreciseClass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDerivedDec) }, explicitStaticRefs = new Type[]{ typeof(PreciseClassDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDerivedDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<StubDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(PreciseClassDecs.TestDec, result);
        }

        [Dec.StaticReferences]
        public static class SuperClassDecs
        {
            static SuperClassDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        [Test]
        public void SuperClass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDerivedDec) }, explicitStaticRefs = new Type[]{ typeof(SuperClassDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDerivedDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<StubDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(SuperClassDecs.TestDec, result);
        }

        [Dec.StaticReferences]
        public static class SubClassDecs
        {
            static SubClassDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDerivedDec TestDec;
        }

        [Test]
        public void SubClass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) }, explicitStaticRefs = new Type[]{ typeof(SubClassDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            var result = Dec.Database<StubDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(SubClassDecs.TestDec);
        }

        public static class NoAttributeDecs
        {
            static NoAttributeDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        [Test]
        public void NoAttribute([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) }, explicitStaticRefs = new Type[] { typeof(NoAttributeDecs) } });

            Dec.Parser parser = null;
            ExpectErrors(() => parser = new Dec.Parser());
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode, rewrite_expectParseErrors: true);

            var result = Dec.Database<StubDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoAttributeDecs.TestDec, result);
        }

        [Dec.StaticReferences]
        public static class NoInitializerDecs
        {
            public static StubDec TestDec;
        }

        [Test]
        public void NoInitializer([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) }, explicitStaticRefs = new Type[]{ typeof(NoInitializerDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            var result = Dec.Database<StubDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoInitializerDecs.TestDec, result);
        }

        [Dec.StaticReferences]
        public class NoStaticDecs
        {
            static NoStaticDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        [Test]
        public void NoStatic([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) }, explicitStaticRefs = new Type[] { typeof(NoStaticDecs) } });

            Dec.Parser parser = null;
            ExpectErrors(() => parser = new Dec.Parser());
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode, rewrite_expectParseErrors: true);

            var result = Dec.Database<StubDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoStaticDecs.TestDec, result);
        }

        [Dec.StaticReferences]
        public static class MissingDecs
        {
            static MissingDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        [Test]
        public void Missing([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) }, explicitStaticRefs = new Type[]{ typeof(MissingDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);
        }

        [Dec.StaticReferences]
        public static class EarlyTouchDecs
        {
            static EarlyTouchDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        [Test]
        public void EarlyTouch()
        {
            ExpectErrors(() => EarlyTouchDecs.TestDec = null);
        }

        [Dec.StaticReferences]
        public static class LateTouchDecs
        {
            static LateTouchDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        [Test]
        public void LateTouch()
        {
            // We avoid ParserMode here because the StaticReference error event can happen at most once, which means that we can't run the test twice without the second (and later) tests failing.
            // This also means that test success would depend on test order run, which, no.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.Finish();

            ExpectErrors(() => LateTouchDecs.TestDec = null);
        }

        [Dec.StaticReferences]
        public static class UnexpectedTouchDecs
        {
            static UnexpectedTouchDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        public class UnexpectedTouchDec : StubDec
        {
            public UnexpectedTouchDec()
            {
                UnexpectedTouchDecs.TestDec = null;
            }
        }

        [Test]
        public void UnexpectedTouch()
        {
            // We avoid ParserMode here because the StaticReference error event can happen at most once, which means that we can't run the test twice without the second (and later) tests failing.
            // This also means that test success would depend on test order run, which, no.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(UnexpectedTouchDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <UnexpectedTouchDec decName=""TestDec"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
        }

        [Dec.StaticReferences]
        public static class ConstructorTouchDecs
        {
            static ConstructorTouchDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ConstructorTouchDec TestDec;
        }

        public class ConstructorTouchDec : StubDec
        {
            public ConstructorTouchDec member;

            public ConstructorTouchDec()
            {
                member = ConstructorTouchDecs.TestDec;
            }
        }

        [Test]
        public void ConstructorTouch()
        {
            // We avoid ParserMode here because the StaticReference error event can happen at most once, which means that we can't run the test twice without the second (and later) tests failing.
            // This also means that test success would depend on test order run, which, no.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConstructorTouchDec) }, explicitStaticRefs = new Type[]{ typeof(ConstructorTouchDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ConstructorTouchDec decName=""TestDec"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
        }

        [Dec.StaticReferences]
        public static class InternalDecs
        {
            static InternalDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            #pragma warning disable CS0649
            internal static StubDec TestDec;
            #pragma warning restore CS0649
        }

        [Test]
        public void Internal([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) }, explicitStaticRefs = new Type[]{ typeof(InternalDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(InternalDecs.TestDec);
        }

        [Dec.StaticReferences]
        public static class EmptyDecs
        {
            static EmptyDecs() { Dec.StaticReferencesAttribute.Initialized(); }
        }

        [Test]
        public void EmptyRefs()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitStaticRefs = new Type[] { typeof(EmptyDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.Finish();
        }
    }
}
