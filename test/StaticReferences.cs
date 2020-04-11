namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class StaticReferences : Base
    {
        [Def.StaticReferences]
        public static class StaticReferenceDefs
        {
            static StaticReferenceDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDefA;
            public static StubDef TestDefB;
        }

        [Test]
	    public void StaticReference([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ typeof(StaticReferenceDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var resultA = Def.Database<StubDef>.Get("TestDefA");
            var resultB = Def.Database<StubDef>.Get("TestDefB");
            Assert.IsNotNull(resultA);
            Assert.IsNotNull(resultB);

            Assert.AreEqual(StaticReferenceDefs.TestDefA, resultA);
            Assert.AreEqual(StaticReferenceDefs.TestDefB, resultB);
	    }

        public class StubDerivedDef : StubDef
        {

        }

        [Def.StaticReferences]
        public static class PreciseClassDefs
        {
            static PreciseClassDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDerivedDef TestDef;
        }

        [Test]
	    public void PreciseClass([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDerivedDef) }, explicitStaticRefs: new Type[]{ typeof(PreciseClassDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDerivedDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(PreciseClassDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public static class SuperClassDefs
        {
            static SuperClassDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void SuperClass([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDerivedDef) }, explicitStaticRefs: new Type[]{ typeof(SuperClassDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDerivedDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(SuperClassDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public static class SubClassDefs
        {
            static SubClassDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDerivedDef TestDef;
        }

        [Test]
	    public void SubClass([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ typeof(SubClassDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, expectErrors: true);

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNull(SubClassDefs.TestDef);
	    }

        public static class NoAttributeDefs
        {
            static NoAttributeDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void NoAttribute([Values] BehaviorMode mode)
	    {
            Def.Parser parser = null;
            ExpectErrors(() => parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ typeof(NoAttributeDefs) }));
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode, expectErrors: true);

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoAttributeDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public static class NoInitializerDefs
        {
            public static StubDef TestDef;
        }

        [Test]
	    public void NoInitializer([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ typeof(NoInitializerDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, expectErrors: true);

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoInitializerDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public class NoStaticDefs
        {
            static NoStaticDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void NoStatic([Values] BehaviorMode mode)
	    {
            Def.Parser parser = null;
            ExpectErrors(() => parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ typeof(NoStaticDefs) }));
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode, expectErrors: true);

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoStaticDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public static class MissingDefs
        {
            static MissingDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void Missing([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ typeof(MissingDefs) });
            parser.AddString(@"
                <Defs>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, expectErrors: true);
        }

        [Def.StaticReferences]
        public static class EarlyTouchDefs
        {
            static EarlyTouchDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void EarlyTouch()
	    {
            ExpectErrors(() => EarlyTouchDefs.TestDef = null);
	    }

        [Def.StaticReferences]
        public static class LateTouchDefs
        {
            static LateTouchDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void LateTouch()
	    {
            // We avoid BehaviorMode here because the StaticReference error event can happen at most once, which means that we can't run the test twice without the second (and later) tests failing.
            // This also means that test success would depend on test order run, which, no.
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) });
            parser.AddString(@"
                <Defs>
                </Defs>");
            parser.Finish();

            ExpectErrors(() => LateTouchDefs.TestDef = null);
	    }

        [Def.StaticReferences]
        public static class UnexpectedTouchDefs
        {
            static UnexpectedTouchDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

        public class UnexpectedTouchDef : StubDef
        {
            public UnexpectedTouchDef()
            {
                UnexpectedTouchDefs.TestDef = null;
            }
        }

        [Test]
	    public void UnexpectedTouch()
	    {
            // We avoid BehaviorMode here because the StaticReference error event can happen at most once, which means that we can't run the test twice without the second (and later) tests failing.
            // This also means that test success would depend on test order run, which, no.
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(UnexpectedTouchDef) });
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <UnexpectedTouchDef defName=""TestDef"" />
                </Defs>"));
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Def.StaticReferences]
        public static class ConstructorTouchDefs
        {
            static ConstructorTouchDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static ConstructorTouchDef TestDef;
        }

        public class ConstructorTouchDef : StubDef
        {
            public ConstructorTouchDef member;

            public ConstructorTouchDef()
            {
                member = ConstructorTouchDefs.TestDef;
            }
        }

        [Test]
	    public void ConstructorTouch()
	    {
            // We avoid BehaviorMode here because the StaticReference error event can happen at most once, which means that we can't run the test twice without the second (and later) tests failing.
            // This also means that test success would depend on test order run, which, no.
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(ConstructorTouchDef) }, explicitStaticRefs: new Type[]{ typeof(ConstructorTouchDefs) });
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <ConstructorTouchDef defName=""TestDef"" />
                </Defs>"));
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Def.StaticReferences]
        public static class InternalDefs
        {
            static InternalDefs() { Def.StaticReferencesAttribute.Initialized(); }

            #pragma warning disable CS0649
            internal static StubDef TestDef;
            #pragma warning restore CS0649
        }

        [Test]
	    public void Internal([Values] BehaviorMode mode)
	    {
            var parser = CreateParserForBehavior(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ typeof(InternalDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(InternalDefs.TestDef);
	    }
    }
}
