using System;
using NUnit.Framework;


namespace DecTest
{
    [TestFixture]
    public class CollectionArray : Base
    {
        public class ArrayDec : Dec.Dec
        {
            public int[] dataEmpty = null;
            public int[] dataProvided = new int[] { 10, 20 };
        }

        [Test]
        public void Basic([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 7, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 7, 6 });
        }

        [Test]
        public void AsStringError([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>nope</dataEmpty>
                        <dataProvided>nope</dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            // error should default to existing data
            Assert.IsNull(result.dataEmpty);
            Assert.AreEqual(result.dataProvided, new[] { 10, 20 });
        }

        [Test]
        public void Zero([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty></dataEmpty>
                        <dataProvided></dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new int[] { });
            Assert.AreEqual(result.dataProvided, new int[] { });
        }

        [Test]
        public void Null([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty null=""true""></dataEmpty>
                        <dataProvided null=""true""></dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.dataEmpty);
            Assert.IsNull(result.dataProvided);
        }

        [Test]
        public void ElementMisparse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>dog</li>
                            <li>6</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>dog</li>
                            <li>6</li>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 0, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 0, 6 });
        }

        [Test]
        public void BadTags([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <horse>10</horse>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>10</li>
                            <li>9</li>
                            <rhino>8</rhino>
                            <li>7</li>
                            <li>6</li>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 7, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 7, 6 });
        }

        [Test]
        public void MultiDimensional([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var arr = new int[2,2,3] {
                { { 1, 2, 3 }, { 4, 5, 6 } },
                { { 7, 8, 9 }, { 10, 11, 12 } }
            };

            var deserialized = DoRecorderRoundTrip(arr, mode);

            Assert.AreEqual(arr, deserialized);
        }

        class MultidimDec : Dec.Dec
        {
            public int[,,] data = new int[1,1,1] { { { 10 } } };
        }

        [Test]
        public void MultiInsufficient([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MultidimDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MultidimDec decName=""TestDec"">
                            <data>
                                <li>
                                    <li>
                                        <li>1</li>
                                        <li>2</li>
                                        <li>3</li>
                                    </li>
                                    <li>
                                        <li>4</li>
                                        <li>5</li>
                                        <li>6</li>
                                    </li>
                                </li>
                                <li>
                                    <li>
                                        <li>7</li>
                                        <li>8</li>
                                    </li>
                                    <li>
                                        <li>10</li>
                                        <li>11</li>
                                        <li>12</li>
                                    </li>
                                </li>
                            </data>
                    </MultidimDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<MultidimDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new int[2,2,3] {
                { { 1, 2, 3 }, { 4, 5, 6 } },
                { { 7, 8, 0 }, { 10, 11, 12 } }
            }, result.data);
        }

        [Test]
        public void MultiExcessive([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MultidimDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MultidimDec decName=""TestDec"">
                            <data>
                                <li>
                                    <li>
                                        <li>1</li>
                                        <li>2</li>
                                        <li>3</li>
                                    </li>
                                    <li>
                                        <li>4</li>
                                        <li>5</li>
                                        <li>6</li>
                                    </li>
                                </li>
                                <li>
                                    <li>
                                        <li>7</li>
                                        <li>8</li>
                                        <li>9</li>
                                        <li>42</li>
                                    </li>
                                    <li>
                                        <li>10</li>
                                        <li>11</li>
                                        <li>12</li>
                                    </li>
                                </li>
                            </data>
                    </MultidimDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<MultidimDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new int[2,2,3] {
                { { 1, 2, 3 }, { 4, 5, 6 } },
                { { 7, 8, 9 }, { 10, 11, 12 } }
            }, result.data);
        }

        [Test]
        public void MultiAppend([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MultidimDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MultidimDec decName=""TestDec"">
                            <data mode=""append"">
                                <li>
                                    <li>
                                        <li>100</li>
                                    </li>
                                </li>
                                <li>
                                    <li>
                                        <li>1000</li>
                                    </li>
                                </li>
                            </data>
                    </MultidimDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<MultidimDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new int[3,1,1] {
                { { 10 } },
                { { 100 } },
                { { 1000 } }
            }, result.data);
        }
    }
}
