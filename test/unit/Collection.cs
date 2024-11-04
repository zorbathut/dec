using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Collection : Base
    {
        public class NestedDec : Dec.Dec
        {
            public int[][] data;
        }

        [Test]
        public void Nested([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(NestedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NestedDec decName=""TestDec"">
                        <data>
                            <li>
                                <li>8</li>
                                <li>16</li>
                            </li>
                            <li>
                                <li>9</li>
                                <li>81</li>
                            </li>
                        </data>
                    </NestedDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<NestedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { new[] { 8, 16 }, new[] { 9, 81 } });
        }
    }
}
