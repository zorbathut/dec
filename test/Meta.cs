namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Meta : Base
    {
	    [Test]
	    public void Clear()
	    {
            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));

            var parser = new Def.Parser();
            parser.ParseFromString(
                @"
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>
                ", new Type[]{ typeof(StubDef) });

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));

            parser.ParseFromString(
                @"
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>
                ", new Type[]{ typeof(StubDef) });

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));
	    }
    }
}
