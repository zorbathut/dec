namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Xml : Base
    {
        [Test]
	    public void DTDParse([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void IncorrectRoot([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            ExpectWarnings(() => parser.AddString(@"
                <NotDefs>
                    <StubDef defName=""TestDef"" />
                </NotDefs>"));
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void MultipleRoot([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                </Defs>
                <Defs>
                    <StubDef defName=""TestDefB"" />
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);

            // Currently not providing any guarantees on whether these get parsed; I'd actually like for them to get parsed, but doing so is tricky
        }

        [Test]
	    public void MultiXML([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                </Defs>");
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefB"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefA"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefB"));
	    }

        [Test]
        public void ProvidedFilenameForXml([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"test.xml"));
            parser.Finish();

            DoBehavior(mode);
        }

        [Test]
        public void ProperStringName([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                </Defs>", "TestStringName"), str => str.StartsWith("TestStringName"));
            parser.Finish();

            DoBehavior(mode);
        }

        [Test]
        public void Garbage([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"�SimpleDef defName=""Hello""><value>3</value></SimpleDef>"));
            parser.Finish();

            DoBehavior(mode);
        }
    }
}
