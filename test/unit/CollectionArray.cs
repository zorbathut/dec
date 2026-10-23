using System;
using System.Linq;
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
        public void Basic([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
        public void WhitespaceOnly([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>
                        </dataEmpty>
                        <dataProvided>
                        </dataProvided>
                    </ArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new int[0], result.dataEmpty);
            Assert.AreEqual(new int[0], result.dataProvided);
        }

        public class IntArrayRecordable : Dec.IRecordable
        {
            public int[] data;
            public int[] data2;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref data, "data");
                record.Shared().Record(ref data2, "data2");
            }
        }

        [Test]
        public void RecorderRefWhitespaceOnly()
        {
            var original = new IntArrayRecordable();
            original.data = new int[0];
            original.data2 = original.data;

            // Indent the shared array's reference element by hand, as if the save had been edited.
            var doc = System.Xml.Linq.XDocument.Parse(Dec.Recorder.Write(original));
            var reference = doc.Root.Element("refs").Elements().Single();
            Assert.IsFalse(reference.HasElements);
            reference.Value = "\n            ";

            var deserialized = Dec.Recorder.Read<IntArrayRecordable>(doc.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));

            Assert.AreEqual(new int[0], deserialized.data);
            Assert.AreSame(deserialized.data, deserialized.data2);
        }

        [Test]
        public void AsStringError([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <dataEmpty>nope</dataEmpty>
                        <dataProvided>nope</dataProvided>
                    </ArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("Text detected"));

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            // error should default to existing data
            Assert.IsNull(result.dataEmpty);
            Assert.AreEqual(result.dataProvided, new[] { 10, 20 });
        }

        [Test]
        public void Zero([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
        public void Null([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
        public void ElementMisparse([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
            ExpectErrors(() => parser.Finish(), err => err.Contains("is not a valid value for Int32"));

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 0, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 0, 6 });
        }

        [Test]
        public void BadTags([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
            ExpectErrors(() => parser.Finish(), err => err.Contains("Tag should be <li>"));

            DoParserTests(mode);

            var result = Dec.Database<ArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataEmpty, new[] { 10, 9, 8, 7, 6 });
            Assert.AreEqual(result.dataProvided, new[] { 10, 9, 8, 7, 6 });
        }

        [Test]
        public void MultiDimensional([ValuesExcept(RecorderMode.Validation)] RecorderMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
        public void MultiInsufficient([ValuesExcept(ParserMode.Validation)] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
            ExpectErrors(() => parser.Finish(), err => err.Contains("padding with default values"));

            DoParserTests(mode);

            var result = Dec.Database<MultidimDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new int[2,2,3] {
                { { 1, 2, 3 }, { 4, 5, 6 } },
                { { 7, 8, 0 }, { 10, 11, 12 } }
            }, result.data);
        }

        [Test]
        public void MultiExcessive([ValuesExcept(ParserMode.Validation)] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
            ExpectErrors(() => parser.Finish(), err => err.Contains("truncating"));

            DoParserTests(mode);

            var result = Dec.Database<MultidimDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new int[2,2,3] {
                { { 1, 2, 3 }, { 4, 5, 6 } },
                { { 7, 8, 9 }, { 10, 11, 12 } }
            }, result.data);
        }

        [Test]
        public void MultiAppend([ValuesExcept(ParserMode.Validation)] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
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
            ExpectErrors(() => parser.Finish(), err => err.Contains("padding with default values"));

            DoParserTests(mode);

            var result = Dec.Database<MultidimDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new int[3,1,1] {
                { { 10 } },
                { { 100 } },
                { { 1000 } }
            }, result.data);
        }

        public class ObjectArrayDec : Dec.Dec
        {
            public object[] data = null;
        }

        [Test]
        public void ObjectArrayNonExistentClassTag([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackEmit = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ObjectArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ObjectArrayDec decName=""TestDec"">
                        <data class=""NonExistentClass[]"">
                            <li>42</li>
                            <li>100</li>
                        </data>
                    </ObjectArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("Couldn't find type named") || err.Contains("cannot be assigned to expected type") || err.Contains("Text detected"));

            DoParserTests(mode);

            var result = Dec.Database<ObjectArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data.GetType(), typeof(object[]));
        }
    }
}
