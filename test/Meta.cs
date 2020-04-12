namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Meta : Base
    {
        [Def.StaticReferences]
        public static class StubDefs
        {
            static StubDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDef;
        }

	    [Test]
	    public void Clear()
	    {
            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));
            // we don't test StubDefs.TestDef here because if we do, we'll kick off the detection

            {
                Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) }, explicitStaticRefs = new Type[]{ typeof(StubDefs) } };
            var parser = new Def.Parser();
                parser.AddString(@"
                    <Defs>
                        <StubDef defName=""TestDef"" />
                    </Defs>");
                parser.Finish();
            }

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
            Assert.AreEqual(StubDefs.TestDef, Def.Database<StubDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));
            Assert.IsNull(StubDefs.TestDef);

            {
                Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) }, explicitStaticRefs = new Type[]{ typeof(StubDefs) } };
            var parser = new Def.Parser();
                parser.AddString(@"
                    <Defs>
                        <StubDef defName=""TestDef"" />
                    </Defs>");
                parser.Finish();
            }

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
            Assert.AreEqual(StubDefs.TestDef, Def.Database<StubDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));
            Assert.IsNull(StubDefs.TestDef);
	    }

        public class RefTargetDef : Def.Def
        {

        }

        public class RefSourceDef : Def.Def
        {
            public RefTargetDef target;
        }

        [Test]
	    public void ClearRef()
	    {
            // Had a bug where Def.Database.Clear() wasn't properly clearing the lookup table used for references
            // This double-checks it

            {
                Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDef), typeof(RefSourceDef) } };
            var parser = new Def.Parser();
                parser.AddString(@"
                    <Defs>
                        <RefTargetDef defName=""Target"" />
                        <RefSourceDef defName=""Source"">
                            <target>Target</target>
                        </RefSourceDef>
                    </Defs>");
                parser.Finish();
            }

            {
                var target = Def.Database<RefTargetDef>.Get("Target");
                var source = Def.Database<RefSourceDef>.Get("Source");
                Assert.IsNotNull(target);
                Assert.IsNotNull(source);

                Assert.AreEqual(source.target, target);
            }

            Def.Database.Clear();

            {
                Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDef), typeof(RefSourceDef) } };
            var parser = new Def.Parser();
                parser.AddString(@"
                    <Defs>
                        <RefSourceDef defName=""Source"">
                            <target>Target</target>
                        </RefSourceDef>
                    </Defs>");
                ExpectErrors(() => parser.Finish());
            }

            {
                var target = Def.Database<RefTargetDef>.Get("Target");
                var source = Def.Database<RefSourceDef>.Get("Source");
                Assert.IsNull(target);
                Assert.IsNotNull(source);

                Assert.IsNull(source.target);
            }
	    }
    }
}
