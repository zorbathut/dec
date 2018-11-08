namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    [TestFixture]
    public class Converter : Base
    {
        public class ConverterTestPayload
        {
            public int number = 0;
        }

        public class ConverterDef : Def.Def
        {
            public ConverterTestPayload payload;
        }

        public class ConverterBasicTest : Def.Converter
        {
            public override HashSet<Type> GeneratedTypes()
            {
                return new HashSet<Type>() { typeof(ConverterTestPayload) };
            }

            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                return new ConverterTestPayload() {number = int.Parse(input) };
            }

            public override object FromXml(XElement input, Type type, string inputName)
            {
                return new ConverterTestPayload() {number = int.Parse(input.Nodes().OfType<XElement>().First().Nodes().OfType<XText>().First().Value) };
            }
        }

        [Test]
	    public void BasicFunctionality()
	    {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ typeof(ConverterDef) }, explicitConversionTypes: new Type[]{ typeof(ConverterBasicTest) });
            parser.AddString(@"
                <Defs>
                    <ConverterDef defName=""TestDefA"">
                        <payload>4</payload>
                    </ConverterDef>
                    <ConverterDef defName=""TestDefB"">
                        <payload><cargo>8</cargo></payload>
                    </ConverterDef>
                </Defs>");
            parser.Finish();

            Assert.AreEqual(Def.Database<ConverterDef>.Get("TestDefA").payload.number, 4);
            Assert.AreEqual(Def.Database<ConverterDef>.Get("TestDefB").payload.number, 8);
	    }
    }
}
