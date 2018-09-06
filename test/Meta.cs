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

            {
                var parser = new Def.Parser(new Type[]{ typeof(StubDef) });
                parser.AddString(@"
                    <Defs>
                        <StubDef defName=""TestDef"" />
                    </Defs>");
                parser.Finish();
            }

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));

            {
                var parser = new Def.Parser(new Type[]{ typeof(StubDef) });
                parser.AddString(@"
                    <Defs>
                        <StubDef defName=""TestDef"" />
                    </Defs>");
                parser.Finish();
            }

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<StubDef>.Get("TestDef"));
	    }
    }
}
