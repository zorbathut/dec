namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class Record : Base
    {
        public class PrimitivesRecordable : Def.IRecordable
        {
            public int intValue;
            public float floatValue;
            public bool boolValue;
            public string stringValue;

            public void Record(Def.Recorder record)
            {
                record.Record(ref intValue, "intValue");
                record.Record(ref floatValue, "floatValue");
                record.Record(ref boolValue, "boolValue");
                record.Record(ref stringValue, "stringValue");
            }
        }

        [Test]
	    public void Primitives()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ });
            parser.AddString(@"
                <Defs>
                </Defs>");
            parser.Finish();

            var primitives = new PrimitivesRecordable();
            primitives.intValue = 42;
            primitives.floatValue = 0.1234f;
            primitives.boolValue = true;
            primitives.stringValue = "<This is a test string value with some XML-sensitive characters.>";

            string serialized = Def.Recorder.Write(primitives, pretty: true);
            var deserialized = Def.Recorder.Read<PrimitivesRecordable>(serialized);

            Assert.AreEqual(primitives.intValue, deserialized.intValue);
            Assert.AreEqual(primitives.floatValue, deserialized.floatValue);
            Assert.AreEqual(primitives.boolValue, deserialized.boolValue);
            Assert.AreEqual(primitives.stringValue, deserialized.stringValue);
        }
    }
}
