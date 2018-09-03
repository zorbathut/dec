namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Meta : Base
    {
        public class ClearTestDef : Def.Def
        {

        }

	    [Test]
	    public void ClearTest()
	    {
            Assert.IsNull(Def.Database<ClearTestDef>.Get("TestDef"));

            var parser = new Def.Parser();
            parser.ParseFromString(
                @"
                <Defs>
                    <ClearTestDef defName=""TestDef"">
                    </ClearTestDef>
                </Defs>
                ", new Type[]{ typeof(ClearTestDef) });

            Assert.IsNotNull(Def.Database<ClearTestDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<ClearTestDef>.Get("TestDef"));

            parser.ParseFromString(
                @"
                <Defs>
                    <ClearTestDef defName=""TestDef"">
                    </ClearTestDef>
                </Defs>
                ", new Type[]{ typeof(ClearTestDef) });

            Assert.IsNotNull(Def.Database<ClearTestDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.IsNull(Def.Database<ClearTestDef>.Get("TestDef"));
	    }
    }
}
