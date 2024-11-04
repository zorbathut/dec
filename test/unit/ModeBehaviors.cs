using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ModeBehaviors : Base
    {
        // This parallels Dec.Serialization.ParseMode, but I really don't want to expose that one, so we just do it again.
        public enum ParseModesToTest
        {
            Default,
            Replace,
            //ReplaceOrCreate, // NYI
            Patch,
            //PatchOrCreate, // NYI
            //Create, //NYI
            Append,
            //Delete, //NYI
            //ReplaceIfExists, //NYI
            //PatchIfExists, //NYI
            //DeleteIfExists, //NYI

            Invalid,
        }

        static string GenerateParseModeTag(ParseModesToTest mode)
        {
            if (mode == ParseModesToTest.Default)
            {
                return "";
            }
            else
            {
                return $"mode='{mode.ToString().ToLower()}'";
            }
        }

        public class DecDec : Dec.Dec
        {
            public int value = 4;
        }

        [Test]
        public void DecMode([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DecDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DecDec decName=""TestDec"" {GenerateParseModeTag(parseMode)}>
                        <value>6</value>
                    </DecDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(6, Dec.Database<DecDec>.Get("TestDec").value);
        }

        public class Convertable
        {
            public int value = 4;
            public int sideValue = 15;
        }

        public class ConvertableConverter : Dec.ConverterRecord<Convertable>
        {
            public override void Record(ref Convertable input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
                recorder.Record(ref input.sideValue, "sideValue");
            }
        }

        public class ConverterDec : Dec.Dec
        {
            public Convertable empty;
            public Convertable filled = new Convertable { value = 5, sideValue = 20 };
        }

        [Test]
        public void ConverterEmpty([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConverterDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ConverterDec decName=""TestDec"">
                        <empty {GenerateParseModeTag(parseMode)}><value>60</value></empty>
                    </ConverterDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(60, Dec.Database<ConverterDec>.Get("TestDec").empty.value);
            Assert.AreEqual(15, Dec.Database<ConverterDec>.Get("TestDec").empty.sideValue);

            Assert.AreEqual(5, Dec.Database<ConverterDec>.Get("TestDec").filled.value);
            Assert.AreEqual(20, Dec.Database<ConverterDec>.Get("TestDec").filled.sideValue);
        }

        [Test]
        public void ConverterFilled([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConverterDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ConverterDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}><value>60</value></filled>
                    </ConverterDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<ConverterDec>.Get("TestDec").empty);

            Assert.AreEqual(60, Dec.Database<ConverterDec>.Get("TestDec").filled.value);
            Assert.AreEqual(20, Dec.Database<ConverterDec>.Get("TestDec").filled.sideValue);
        }

        public class Recordable : Dec.IRecordable
        {
            public int value = 4;
            public int sideValue = 15;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
                recorder.Record(ref sideValue, "sideValue");
            }
        }

        public class RecorderDec : Dec.Dec
        {
            public Recordable empty;
            public Recordable filled = new Recordable { value = 5, sideValue = 20 };
        }

        [Test]
        public void RecorderEmpty([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RecorderDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <RecorderDec decName=""TestDec"">
                        <empty {GenerateParseModeTag(parseMode)}><value>60</value></empty>
                    </RecorderDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(60, Dec.Database<RecorderDec>.Get("TestDec").empty.value);
            Assert.AreEqual(15, Dec.Database<RecorderDec>.Get("TestDec").empty.sideValue);

            Assert.AreEqual(5, Dec.Database<RecorderDec>.Get("TestDec").filled.value);
            Assert.AreEqual(20, Dec.Database<RecorderDec>.Get("TestDec").filled.sideValue);
        }

        [Test]
        public void RecorderFilled([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RecorderDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <RecorderDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}><value>60</value></filled>
                    </RecorderDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<RecorderDec>.Get("TestDec").empty);

            Assert.AreEqual(60, Dec.Database<RecorderDec>.Get("TestDec").filled.value);
            Assert.AreEqual(20, Dec.Database<RecorderDec>.Get("TestDec").filled.sideValue);
        }

        public class PrimitiveDec : Dec.Dec
        {
            public int value = 4;
        }

        [Test]
        public void Primitive([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PrimitiveDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <PrimitiveDec decName=""TestDec"">
                        <value {GenerateParseModeTag(parseMode)}>60</value>
                    </PrimitiveDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(60, Dec.Database<PrimitiveDec>.Get("TestDec").value);
        }

        public class ListDec : Dec.Dec
        {
            public List<int> empty;
            public List<int> filled = new List<int> { 1, 2, 3, 4 };
        }

        [Test]
        public void ListEmpty([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ListDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ListDec decName=""TestDec"">
                        <empty {GenerateParseModeTag(parseMode)}>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </empty>
                    </ListDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(new List<int> { 5, 6, 7 }, Dec.Database<ListDec>.Get("TestDec").empty);
            Assert.AreEqual(new List<int> { 1, 2, 3, 4 }, Dec.Database<ListDec>.Get("TestDec").filled);
        }

        [Test]
        public void ListFilled([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ListDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ListDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </filled>
                    </ListDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<ListDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Append)
            {
                Assert.AreEqual(new List<int> { 1, 2, 3, 4, 5, 6, 7 }, Dec.Database<ListDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new List<int> { 5, 6, 7 }, Dec.Database<ListDec>.Get("TestDec").filled);
            }
        }

        public class ArrayDec : Dec.Dec
        {
            public int[] empty;
            public int[] filled = new int[] { 1, 2, 3, 4 };
        }

        [Test]
        public void ArrayEmpty([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <empty {GenerateParseModeTag(parseMode)}>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </empty>
                    </ArrayDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(new int[] { 5, 6, 7 }, Dec.Database<ArrayDec>.Get("TestDec").empty);
            Assert.AreEqual(new int[] { 1, 2, 3, 4 }, Dec.Database<ArrayDec>.Get("TestDec").filled);
        }

        [Test]
        public void ArrayFilled([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ArrayDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </filled>
                    </ArrayDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<ArrayDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Append)
            {
                Assert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6, 7 }, Dec.Database<ArrayDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new int[] { 5, 6, 7 }, Dec.Database<ArrayDec>.Get("TestDec").filled);
            }
        }

        public class DictionaryDec : Dec.Dec
        {
            public Dictionary<string, int> empty;
            public Dictionary<string, int> filled = new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 } };
        }

        [Test]
        public void DictionaryEmpty([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryDec decName=""TestDec"">
                        <empty {GenerateParseModeTag(parseMode)}>
                            <four>4</four>
                            <five>5</five>
                        </empty>
                    </DictionaryDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Patch || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(new Dictionary<string, int> { { "four", 4 }, { "five", 5 } }, Dec.Database<DictionaryDec>.Get("TestDec").empty);
            Assert.AreEqual(new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 } }, Dec.Database<DictionaryDec>.Get("TestDec").filled);
        }

        [Test]
        public void DictionaryFilled([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <four>4</four>
                            <five>5</five>
                        </filled>
                    </DictionaryDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Patch || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<DictionaryDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Patch || parseMode == ParseModesToTest.Append)
            {
                Assert.AreEqual(new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 } }, Dec.Database<DictionaryDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new Dictionary<string, int> { { "four", 4 }, { "five", 5 } }, Dec.Database<DictionaryDec>.Get("TestDec").filled);
            }
        }

        [Test]
        public void DictionaryReplace([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <three>-3</three>
                            <four>4</four>
                            <five>5</five>
                        </filled>
                    </DictionaryDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<DictionaryDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Patch || parseMode == ParseModesToTest.Append)
            {
                Assert.AreEqual(new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", -3 }, { "four", 4 }, { "five", 5 } }, Dec.Database<DictionaryDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new Dictionary<string, int> { { "three", -3 }, { "four", 4 }, { "five", 5 } }, Dec.Database<DictionaryDec>.Get("TestDec").filled);
            }
        }

        [Test]
        public void DictionaryDoubleReplace([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <three>-3</three>
                            <three>-33</three>
                            <four>4</four>
                            <five>5</five>
                        </filled>
                    </DictionaryDec>
                </Decs>");

            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<DictionaryDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Patch || parseMode == ParseModesToTest.Append)
            {
                Assert.AreEqual(new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", -33 }, { "four", 4 }, { "five", 5 } }, Dec.Database<DictionaryDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new Dictionary<string, int> { { "three", -33 }, { "four", 4 }, { "five", 5 } }, Dec.Database<DictionaryDec>.Get("TestDec").filled);
            }
        }

        public class HashSetDec : Dec.Dec
        {
            public HashSet<int> empty;
            public HashSet<int> filled = new HashSet<int> { 1, 2, 3, 4 };
        }

        [Test]
        public void HashSetEmpty([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <HashSetDec decName=""TestDec"">
                        <empty {GenerateParseModeTag(parseMode)}>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </empty>
                    </HashSetDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Patch || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(new HashSet<int> { 5, 6, 7 }, Dec.Database<HashSetDec>.Get("TestDec").empty);
            Assert.AreEqual(new HashSet<int> { 1, 2, 3, 4 }, Dec.Database<HashSetDec>.Get("TestDec").filled);
        }

        [Test]
        public void HashSetFilled([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <HashSetDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </filled>
                    </HashSetDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Patch || parseMode == ParseModesToTest.Append)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<HashSetDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Append || parseMode == ParseModesToTest.Patch)
            {
                Assert.AreEqual(new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 }, Dec.Database<HashSetDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new HashSet<int> { 5, 6, 7 }, Dec.Database<HashSetDec>.Get("TestDec").filled);
            }
        }

        [Test]
        public void HashSetReplace([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <HashSetDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <li>4</li>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </filled>
                    </HashSetDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<HashSetDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Append || parseMode == ParseModesToTest.Patch)
            {
                Assert.AreEqual(new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 }, Dec.Database<HashSetDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new HashSet<int> { 4, 5, 6, 7 }, Dec.Database<HashSetDec>.Get("TestDec").filled);
            }
        }

        [Test]
        public void HashSetDoubleReplace([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <HashSetDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}>
                            <li>4</li>
                            <li>4</li>
                            <li>5</li>
                            <li>6</li>
                            <li>7</li>
                        </filled>
                    </HashSetDec>
                </Decs>");

            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<HashSetDec>.Get("TestDec").empty);
            if (parseMode == ParseModesToTest.Append || parseMode == ParseModesToTest.Patch)
            {
                Assert.AreEqual(new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 }, Dec.Database<HashSetDec>.Get("TestDec").filled);
            }
            else
            {
                Assert.AreEqual(new HashSet<int> { 4, 5, 6, 7 }, Dec.Database<HashSetDec>.Get("TestDec").filled);
            }
        }

        public class TupleDec : Dec.Dec
        {
            public (int, int) value = (42, 99);
        }

        [Test]
        public void Tuple([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TupleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TupleDec decName=""TestDec"">
                        <value {GenerateParseModeTag(parseMode)}>
                            <li>11</li>
                            <li>12</li>
                        </value>
                    </TupleDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Replace)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual((11, 12), Dec.Database<TupleDec>.Get("TestDec").value);
        }

        public class Composite
        {
            public int value = 4;
            public int sideValue = 15;
        }

        public class CompositeDec : Dec.Dec
        {
            public Composite empty;
            public Composite filled = new Composite { value = 5, sideValue = 20 };
        }

        [Test]
        public void CompositeEmpty([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(CompositeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <CompositeDec decName=""TestDec"">
                        <empty {GenerateParseModeTag(parseMode)}><value>60</value></empty>
                    </CompositeDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.AreEqual(60, Dec.Database<CompositeDec>.Get("TestDec").empty.value);
            Assert.AreEqual(15, Dec.Database<CompositeDec>.Get("TestDec").empty.sideValue);

            Assert.AreEqual(5, Dec.Database<CompositeDec>.Get("TestDec").filled.value);
            Assert.AreEqual(20, Dec.Database<CompositeDec>.Get("TestDec").filled.sideValue);
        }

        [Test]
        public void CompositeFilled([Values] ParserMode mode, [Values] ParseModesToTest parseMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(CompositeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <CompositeDec decName=""TestDec"">
                        <filled {GenerateParseModeTag(parseMode)}><value>60</value></filled>
                    </CompositeDec>
                </Decs>");

            if (parseMode == ParseModesToTest.Default || parseMode == ParseModesToTest.Patch)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<CompositeDec>.Get("TestDec").empty);

            Assert.AreEqual(60, Dec.Database<CompositeDec>.Get("TestDec").filled.value);
            Assert.AreEqual(20, Dec.Database<CompositeDec>.Get("TestDec").filled.sideValue);
        }


        public interface IComponent { }
        public class ComponentConcrete : IComponent {  }

        public class EntityDec : Dec.Dec
        {
            public List<IComponent> components;
        }

        [Test]
        public void ComponentAppend([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(EntityDec), typeof(ComponentConcrete) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <EntityDec decName=""BaseEntity"" abstract=""true"">
                        <components>
                            <li class=""ComponentConcrete"" />
                        </components>
                    </EntityDec>

                    <EntityDec decName=""ConcreteEntity"" parent=""BaseEntity"">
                        <components mode=""append"">
                            <li class=""ComponentConcrete"" />
                        </components>
                    </EntityDec>

                    <EntityDec decName=""ConcreteEntityBeta"" parent=""BaseEntity"">
                        <components mode=""append"">
                            <li class=""ComponentConcrete"" />
                        </components>
                    </EntityDec>
                </Decs>");

            parser.Finish();

            Assert.AreEqual(2, Dec.Database<EntityDec>.Get("ConcreteEntity").components.Count);
        }

        public class PairOfThings
        {
            public int a = -1;
            public int b = -2;
        }

        public class DictPairDec : Dec.Dec
        {
            public Dictionary<string, PairOfThings> pairs;
        }

        public enum DictValuePatchModes
        {
            Default,
            patch,
        }
        [Test]
        public void DictValuePatch([Values] ParserMode mode, [Values] DictValuePatchModes patchModes)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictPairDec) } });

            string patchString = patchModes == DictValuePatchModes.Default ? "" : $@" mode=""{patchModes}""";

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictPairDec decName=""Base"" abstract=""true"">
                        <pairs>
                            <original>
                                <a>1</a>
                                <b>2</b>
                            </original>
                        </pairs>
                    </DictPairDec>

                    <DictPairDec decName=""DerivedInline"" parent=""Base"">
                        <pairs mode=""patch"">
                            <original {patchString}>
                                <b>3</b>
                            </original>
                        </pairs>
                    </DictPairDec>

                    <DictPairDec decName=""DerivedLi"" parent=""Base"">
                        <pairs mode=""patch"">
                            <li>
                                <key>original</key>
                                <value {patchString}>
                                    <b>4</b>
                                </value>
                            </li>
                        </pairs>
                    </DictPairDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Database<DictPairDec>.Get("DerivedInline").pairs["original"].a);
            Assert.AreEqual(1, Dec.Database<DictPairDec>.Get("DerivedLi").pairs["original"].a);

            Assert.AreEqual(3, Dec.Database<DictPairDec>.Get("DerivedInline").pairs["original"].b);
            Assert.AreEqual(4, Dec.Database<DictPairDec>.Get("DerivedLi").pairs["original"].b);
        }

        public class DictionaryItem
        {
            public int a;
            public int b;
        }

        public class DictionaryHolderDec : Dec.Dec, Dec.IRecordable
        {
            public Dictionary<string, DictionaryItem> dict;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Read)
                {
                    var dict = new Dictionary<string, DictionaryItem>();
                    recorder.Record(ref dict, "dict");
                    this.dict = dict;
                }
                else
                {
                    recorder.Record(ref dict, "dict");
                }
            }
        }

        [Test] [Ignore("Hard to fix and I currently don't need it")]
        public void RecordableInheritance([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictionaryHolderDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <DictionaryHolderDec abstract=""true"" decName=""Base"">
                        <dict mode=""patch"">
                            <original mode=""patch"">
                                <a>1</a>
                            </original>
                            <new mode=""patch"">
                                <b>2</b>
                            </new>
                        </dict>
                    </DictionaryHolderDec>

                    <DictionaryHolderDec parent=""Base"" decName=""Derived"">
                        <dict mode=""patch"">
                            <new mode=""patch"">
                                <a>3</a>
                            </new>
                        </dict>
                    </DictionaryHolderDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Database<DictionaryHolderDec>.Get("Derived").dict["original"].a);
            Assert.AreEqual(2, Dec.Database<DictionaryHolderDec>.Get("Derived").dict["new"].b);
            Assert.AreEqual(3, Dec.Database<DictionaryHolderDec>.Get("Derived").dict["new"].a);
        }
    }
}
