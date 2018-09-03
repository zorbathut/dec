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
            var parser = new Def.Parser();
            parser.ParseFromString(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>",
                new Type[]{ typeof(StubDef) });

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void IncorrectRoot()
	    {
            var parser = new Def.Parser();
            ExpectWarnings(() => parser.ParseFromString(@"
                <NotDefs>
                    <StubDef defName=""TestDef"" />
                </NotDefs>",
                new Type[]{ typeof(StubDef) }));

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void MultipleRoot()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                </Defs>
                <Defs>
                    <StubDef defName=""TestDefB"" />
                </Defs>",
                new Type[]{ typeof(StubDef) }));

            // Currently not providing any guarantees on whether these get parsed; I'd actually like for them to get parsed, but doing so is tricky
	    }
    }
}
