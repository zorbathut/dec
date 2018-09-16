namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Defs : Base
    {
        [Test]
	    public void TrivialParse()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void TrivialEmptyParse()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }
        
        [Test]
	    public void NonDefType()
	    {
            Def.Parser parser = null;
            ExpectErrors(() => parser = new Def.Parser(explicitTypes: new Type[]{ typeof(bool), typeof(StubDef) }, explicitStaticRefs: new Type[]{ }));
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void MissingDefType()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ });
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <NonexistentDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>"));
            parser.Finish();

            Assert.IsNull(Def.Database<StubDef>.Get("TestDefA"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefB"));
	    }

        [Test]
	    public void MissingDefName()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ });
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <StubDef />
                </Defs>"));
            parser.Finish();
	    }

        [Test]
	    public void InvalidDefName()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ });
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <StubDef defName=""1NumberPrefix"" />
                    <StubDef defName=""Contains Spaces"" />
                    <StubDef defName=""HasPunctuation!"" />
                </Defs>"));
            parser.Finish();

            Assert.IsNull(Def.Database<StubDef>.Get("1NumberPrefix"));
            Assert.IsNull(Def.Database<StubDef>.Get("Contains Spaces"));
            Assert.IsNull(Def.Database<StubDef>.Get("HasPunctuation!"));
	    }

        public class IntDef : Def.Def
        {
            public int value = 4;
        }

        [Test]
	    public void DuplicateField()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(IntDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>3</value>
                        <value>6</value>
                    </IntDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(6, result.value);
	    }

        [Test]
	    public void DuplicateDef()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(IntDef) }, explicitStaticRefs: new Type[]{ });
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>10</value>
                    </IntDef>
                    <IntDef defName=""TestDef"">
                        <value>20</value>
                    </IntDef>
                </Defs>"));
            parser.Finish();

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(20, result.value);
	    }

        public class DeepParentDef : Def.Def
        {
            public int value = 4;
        }

        public class DeepChildDef : DeepParentDef
        {
            
        }

        [Test]
	    public void HierarchyDeepField()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(DeepChildDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <DeepChildDef defName=""TestDef"">
                        <value>12</value>
                    </DeepChildDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<DeepParentDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
	    }

        public class DupeParentDef : Def.Def
        {
            public int value = 4;
        }

        public class DupeChildDef : DupeParentDef
        {
            new public int value = 8;
        }

        [Test]
	    public void HierarchyDuplicateField()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(DupeChildDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <DupeChildDef defName=""TestDef"">
                        <value>12</value>
                    </DupeChildDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = (DupeChildDef)Def.Database<DupeParentDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
            Assert.AreEqual(4, ((DupeParentDef)result).value);
	    }

        [Test]
	    public void ExtraAttribute()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" invalidAttribute=""hello"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        public class StubBetaDef : Def.Def
        {

        }

        public class StubChildDef : StubDef
        {

        }

        [Test]
	    public void Index()
        {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef), typeof(StubBetaDef), typeof(StubChildDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <StubChildDef defName=""TestDefA"" />
                    <StubBetaDef defName=""TestDefB"" />
                    <StubDef defName=""TestDefC"" />
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefA"));
            Assert.IsNotNull(Def.Database<StubBetaDef>.Get("TestDefB"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefC"));

            Assert.AreEqual(Def.Database<StubDef>.Get("TestDefA").index, 0);
            Assert.AreEqual(Def.Database<StubBetaDef>.Get("TestDefB").index, 0);
            Assert.AreEqual(Def.Database<StubDef>.Get("TestDefC").index, 1);

            Assert.AreEqual(Def.Database<StubDef>.Count, 2);
            Assert.AreEqual(Def.Database<StubBetaDef>.Count, 1);

            Assert.AreEqual(Def.Database.Count, 3);

            Assert.AreEqual(Def.Database.List.Count(), 3);
        }

        [Test]
	    public void DebugPrint()
        {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(StubDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));

            Assert.AreEqual(Def.Database<StubDef>.Get("TestDef").ToString(), "TestDef");
        }

        public class ErrorDef : Def.Def
        {
            public override IEnumerable<string> ConfigErrors()
            {
                foreach (var err in base.ConfigErrors())
                {
                    yield return err;
                }

                yield return "I am never valid";
            }
        }

        [Test]
	    public void ConfigErrors()
        {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(ErrorDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <ErrorDef defName=""TestDef"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            Assert.IsNotNull(Def.Database<ErrorDef>.Get("TestDef"));
        }

        public class PostLoadDef : Def.Def
        {
            public bool initted = false;

            public override void PostLoad()
            {
                initted = true;
            }
        }

        [Test]
	    public void PostLoad()
        {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(PostLoadDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <PostLoadDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<PostLoadDef>.Get("TestDef"));
            Assert.IsTrue(Def.Database<PostLoadDef>.Get("TestDef").initted);
        }
    }
}
