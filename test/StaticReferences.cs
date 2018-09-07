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
            static StaticReferenceDefs() { Def.StaticReferences.Initialized(); }

            public static StubDef TestDefA;
            public static StubDef TestDefB;
        }

        [Test]
	    public void StaticReference()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDef) }, new Type[]{ typeof(StaticReferenceDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>");
            parser.Finish();

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
            static PreciseClassDefs() { Def.StaticReferences.Initialized(); }

            public static StubDerivedDef TestDef;
        }

        [Test]
	    public void PreciseClass()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDerivedDef) }, new Type[]{ typeof(PreciseClassDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDerivedDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(PreciseClassDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public static class SuperClassDefs
        {
            static SuperClassDefs() { Def.StaticReferences.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void SuperClass()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDerivedDef) }, new Type[]{ typeof(SuperClassDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDerivedDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(SuperClassDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public static class SubClassDefs
        {
            static SubClassDefs() { Def.StaticReferences.Initialized(); }

            public static StubDerivedDef TestDef;
        }

        [Test]
	    public void SubClass()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDef) }, new Type[]{ typeof(SubClassDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNull(SubClassDefs.TestDef);
	    }

        public static class NoAttributeDefs
        {
            static NoAttributeDefs() { Def.StaticReferences.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void NoAttribute()
	    {
            Def.Parser parser = null;
            ExpectErrors(() => parser = new Def.Parser(new Type[]{ typeof(StubDef) }, new Type[]{ typeof(NoAttributeDefs) }));
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

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
	    public void NoInitializer()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDef) }, new Type[]{ typeof(NoInitializerDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoInitializerDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public class NoStaticDefs
        {
            static NoStaticDefs() { Def.StaticReferences.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void NoStatic()
	    {
            Def.Parser parser = null;
            ExpectErrors(() => parser = new Def.Parser(new Type[]{ typeof(StubDef) }, new Type[]{ typeof(NoStaticDefs) }));

            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            var result = Def.Database<StubDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(NoStaticDefs.TestDef, result);
	    }

        [Def.StaticReferences]
        public static class MissingDefs
        {
            static MissingDefs() { Def.StaticReferences.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void Missing()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDef) }, new Type[]{ typeof(MissingDefs) });
            parser.AddString(@"
                <Defs>
                </Defs>");
            ExpectErrors(() => parser.Finish());
	    }

        [Def.StaticReferences]
        public static class EarlyTouchDefs
        {
            static EarlyTouchDefs() { Def.StaticReferences.Initialized(); }

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
            static LateTouchDefs() { Def.StaticReferences.Initialized(); }

            public static StubDef TestDef;
        }

        [Test]
	    public void LateTouch()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDef) });
            parser.AddString(@"
                <Defs>
                </Defs>");
            parser.Finish();

            ExpectErrors(() => LateTouchDefs.TestDef = null);
	    }

        [Def.StaticReferences]
        public static class UnexpectedTouchDefs
        {
            static UnexpectedTouchDefs() { Def.StaticReferences.Initialized(); }

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
            var parser = new Def.Parser(new Type[]{ typeof(UnexpectedTouchDef) });
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
            static ConstructorTouchDefs() { Def.StaticReferences.Initialized(); }

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
            var parser = new Def.Parser(new Type[]{ typeof(ConstructorTouchDef) }, new Type[]{ typeof(ConstructorTouchDefs) });
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
            static InternalDefs() { Def.StaticReferences.Initialized(); }

            internal static StubDef TestDef;
        }

        [Test]
	    public void Internal()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(StubDef) }, new Type[]{ typeof(InternalDefs) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(InternalDefs.TestDef);
	    }
    }
}
