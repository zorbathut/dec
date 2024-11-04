using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class CollectionQueue : Base
    {
        public class SimpleDec : Dec.Dec
        {
            public Queue<int> queue;
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
                        <queue>
                            <li>4</li>
                            <li>12</li>
                            <li>20</li>
                        </queue>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new Queue<int>(new int[] { 4, 12, 20 }), result.queue);
        }

        [Test]
        public void Recordable([Values] RecorderMode mode)
        {
            // first make sure this is inputting in the order that's expected
            Assert.AreEqual(1, new Queue<int>(new int[] { 1, 2, 3, 4 }).Dequeue());

            var element = new Queue<int>(new int[] { 4, 12, 20, -10 });

            // we are also making sure this prints them in the expected order
            var deserialized = DoRecorderRoundTrip(element, mode, testSerializedResult: ser => Assert.IsTrue(ser.IndexOf("<li>4</li>") < ser.IndexOf("<li>12</li>")));

            Assert.AreEqual(element, deserialized);
        }
    }
}
