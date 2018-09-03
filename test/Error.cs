namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Error : Base
    {
        [Test]
        public void WarningTest()
        {
            // This doesn't happen normally, but does in our test framework
            Assert.Throws(typeof(ArgumentException), () => Def.Config.WarningHandler("Test"));

            ExpectWarnings(() => Def.Config.WarningHandler("Test"));

            // Make sure things are deinited properly
            Assert.Throws(typeof(ArgumentException), () => Def.Config.WarningHandler("Test"));
        }

        [Test]
        public void ErrorTest()
        {
            Assert.Throws(typeof(ArgumentException), () => Def.Config.ErrorHandler("Test"));

            ExpectErrors(() => Def.Config.ErrorHandler("Test"));

            // Make sure things are deinited properly
            Assert.Throws(typeof(ArgumentException), () => Def.Config.ErrorHandler("Test"));
        }

        [Test]
	    public void IncorrectRootTest()
	    {
            var parser = new Def.Parser();
            ExpectWarnings(() => parser.ParseFromString(
                @"
                <NotDefs>
                    <StubDef defName=""TestDef"">
                        
                    </StubDef>
                </NotDefs>
                ", new Type[]{ typeof(StubDef) }));

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void MultipleRootTest()
	    {
            var parser = new Def.Parser();
            ExpectErrors(() => parser.ParseFromString(
                @"
                <Defs>
                    <StubDef defName=""TestDefA"">
                        
                    </StubDef>
                </Defs>
                <Defs>
                    <StubDef defName=""TestDefB"">
                        
                    </StubDef>
                </Defs>
                ", new Type[]{ typeof(StubDef) }));

            // Currently not providing any guarantees on whether these get parsed; I'd actually like for them to get parsed, but doing so is tricky
	    }
    }
}
