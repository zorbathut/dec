namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Meta
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            Def.Database.Clear();
        }

        public class ClearTestDef : Def.Def
        {

        }

	    [Test]
	    public void ClearTest()
	    {
            Assert.AreEqual(null, Def.Database<ClearTestDef>.Get("TestDef"));

            var parser = new Def.Parser();
            parser.ParseFromString(
                @"
                <ClearTestDef defName=""TestDef"">
                </ClearTestDef>
                ", new Type[]{ typeof(ClearTestDef) });

            Assert.AreNotEqual(null, Def.Database<ClearTestDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.AreEqual(null, Def.Database<ClearTestDef>.Get("TestDef"));

            parser.ParseFromString(
                @"
                <ClearTestDef defName=""TestDef"">
                </ClearTestDef>
                ", new Type[]{ typeof(ClearTestDef) });

            Assert.AreNotEqual(null, Def.Database<ClearTestDef>.Get("TestDef"));

            Def.Database.Clear();

            Assert.AreEqual(null, Def.Database<ClearTestDef>.Get("TestDef"));
	    }
    }
}
