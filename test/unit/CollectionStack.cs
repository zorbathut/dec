using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DecTest
{
    [TestFixture]
    public class CollectionStack : Base
    {
        public class SimpleDec : Dec.Dec
        {
            public Stack<int> stack;
        }

        [Test]
        public void Simple([Values] ParserMode mode)
        {
            // These aren't really that useful in decs, but whatever, I'd rather support it than not support it.

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""TestDec"">
                        <stack>
                            <li>4</li>
                            <li>12</li>
                            <li>20</li>
                        </stack>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new Stack<int>(new int[] { 4, 12, 20 }), result.stack);
        }

        [Test]
        public void Recordable([Values] RecorderMode mode)
        {
            // first make sure this is inputting in the order that's expected
            Assert.AreEqual(4, new Stack<int>(new int[] { 1, 2, 3, 4 }).Pop());

            var element = new Stack<int>(new int[] { 4, 12, 20, -10 });

            // we are also making sure this prints them in the expected order
            var deserialized = DoRecorderRoundTrip(element, mode, testSerializedResult: ser => Assert.IsTrue(ser.IndexOf("<li>4</li>") < ser.IndexOf("<li>12</li>")));

            Assert.AreEqual(element, deserialized);
        }
    }
}
