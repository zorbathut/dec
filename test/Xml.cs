namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Xml : Base
    {
        [Test]
	    public void DTDParse()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) });
            parser.AddString(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void IncorrectRoot()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) });
            ExpectWarnings(() => parser.AddString(@"
                <NotDefs>
                    <StubDef defName=""TestDef"" />
                </NotDefs>"));
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void MultipleRoot()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) });
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                </Defs>
                <Defs>
                    <StubDef defName=""TestDefB"" />
                </Defs>"));
            parser.Finish();

            // Currently not providing any guarantees on whether these get parsed; I'd actually like for them to get parsed, but doing so is tricky
	    }

        [Test]
	    public void MultiXML()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(StubDef) });
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                </Defs>");
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefB"" />
                </Defs>");
            parser.Finish();

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefA"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefB"));
	    }
    }
}
