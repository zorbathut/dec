using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DecTest
{
    [TestFixture]
    public class CollectionTuple : Base
    {
        public class SimpleDec : Dec.Dec
        {
            public Tuple<int, string> tupleIs;
            public Tuple<string, int> tupleSi;
            public Tuple<List<int>, Dictionary<string, int>> tupleLd;

            public Tuple<int> tuple1;
            public Tuple<int, int> tuple2;
            public Tuple<int, int, int> tuple3;
            public Tuple<int, int, int, int> tuple4;
            public Tuple<int, int, int, int, int, int, int> tuple7;
        }

        [Test]
        public void Simple([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""TestDec"">
                        <tupleIs>
                            <li>4</li>
                            <li>cows</li>
                        </tupleIs>
                        <tupleSi>
                            <li>horses</li>
                            <li>8</li>
                        </tupleSi>
                        <tupleLd>
                            <li>
                                <li>1</li>
                                <li>2</li>
                                <li>3</li>
                            </li>
                            <li>
                                <sheep>9</sheep>
                                <goats>99</goats>
                            </li>
                        </tupleLd>

                        <tuple1>
                            <li>11</li>
                        </tuple1>
                        <tuple2>
                            <li>21</li>
                            <li>22</li>
                        </tuple2>
                        <tuple3>
                            <li>31</li>
                            <li>32</li>
                            <li>33</li>
                        </tuple3>
                        <tuple4>
                            <li>41</li>
                            <li>42</li>
                            <li>43</li>
                            <li>44</li>
                        </tuple4>
                        <tuple7>
                            <li>51</li>
                            <li>52</li>
                            <li>53</li>
                            <li>54</li>
                            <li>55</li>
                            <li>56</li>
                            <li>57</li>
                        </tuple7>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(4, "cows"), result.tupleIs);
            Assert.AreEqual(Tuple.Create("horses", 8), result.tupleSi);
            Assert.AreEqual(Tuple.Create(new List<int>() { 1, 2, 3 }, new Dictionary<string, int> { { "sheep", 9 }, { "goats", 99 } }), result.tupleLd);

            Assert.AreEqual(Tuple.Create(11), result.tuple1);
            Assert.AreEqual(Tuple.Create(21, 22), result.tuple2);
            Assert.AreEqual(Tuple.Create(31, 32, 33), result.tuple3);
            Assert.AreEqual(Tuple.Create(41, 42, 43, 44), result.tuple4);
            Assert.AreEqual(Tuple.Create(51, 52, 53, 54, 55, 56, 57), result.tuple7);
        }

        public class ValueDec : Dec.Dec
        {
            public (int, string) tupleIs;
            public (string, int) tupleSi;
            public (List<int>, Dictionary<string, int>) tupleLd;

            public ValueTuple<int> tuple1; // syntactic sugar doesn't work for this one, but it's still valid
            public (int, int) tuple2;
            public (int, int, int) tuple3;
            public (int, int, int, int) tuple4;
            public (int, int, int, int, int, int, int) tuple7;
        }

        [Test]
        public void Value([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ValueDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ValueDec decName=""TestDec"">
                        <tupleIs>
                            <li>4</li>
                            <li>cows</li>
                        </tupleIs>
                        <tupleSi>
                            <li>horses</li>
                            <li>8</li>
                        </tupleSi>
                        <tupleLd>
                            <li>
                                <li>1</li>
                                <li>2</li>
                                <li>3</li>
                            </li>
                            <li>
                                <sheep>9</sheep>
                                <goats>99</goats>
                            </li>
                        </tupleLd>

                        <tuple1>
                            <li>11</li>
                        </tuple1>
                        <tuple2>
                            <li>21</li>
                            <li>22</li>
                        </tuple2>
                        <tuple3>
                            <li>31</li>
                            <li>32</li>
                            <li>33</li>
                        </tuple3>
                        <tuple4>
                            <li>41</li>
                            <li>42</li>
                            <li>43</li>
                            <li>44</li>
                        </tuple4>
                        <tuple7>
                            <li>51</li>
                            <li>52</li>
                            <li>53</li>
                            <li>54</li>
                            <li>55</li>
                            <li>56</li>
                            <li>57</li>
                        </tuple7>
                    </ValueDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ValueDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(ValueTuple.Create(4, "cows"), result.tupleIs);
            Assert.AreEqual(ValueTuple.Create("horses", 8), result.tupleSi);
            Assert.AreEqual(ValueTuple.Create(new List<int>() { 1, 2, 3 }, new Dictionary<string, int> { { "sheep", 9 }, { "goats", 99 } }), result.tupleLd);

            Assert.AreEqual(ValueTuple.Create(11), result.tuple1);
            Assert.AreEqual(ValueTuple.Create(21, 22), result.tuple2);
            Assert.AreEqual(ValueTuple.Create(31, 32, 33), result.tuple3);
            Assert.AreEqual(ValueTuple.Create(41, 42, 43, 44), result.tuple4);
            Assert.AreEqual(ValueTuple.Create(51, 52, 53, 54, 55, 56, 57), result.tuple7);
        }

        public class RecordableType : Dec.IRecordable
        {
            public Tuple<int, string> tuple;
            public (int, string) value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref tuple, "tuple");
                recorder.Record(ref value, "value");
            }
        }

        [Test]
        public void Recordable([Values] RecorderMode mode)
        {
            var element = new RecordableType();
            element.tuple = Tuple.Create(3, "elk");
            element.value = (4, "wolves");

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.AreEqual(element.tuple, deserialized.tuple);
            Assert.AreEqual(element.value, deserialized.value);
        }

        public class QuadDec : Dec.Dec
        {
            public Tuple<int, int, int, int> tuple;
        }

        [Test]
        public void TooFew([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(QuadDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <QuadDec decName=""TestDec"">
                        <tuple>
                            <li>1</li>
                            <li>2</li>
                            <li>3</li>
                        </tuple>
                    </QuadDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<QuadDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(1, 2, 3, 0), result.tuple);
        }

        [Test]
        public void TooMany([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(QuadDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <QuadDec decName=""TestDec"">
                        <tuple>
                            <li>1</li>
                            <li>2</li>
                            <li>3</li>
                            <li>4</li>
                            <li>5</li>
                        </tuple>
                    </QuadDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<QuadDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(1, 2, 3, 4), result.tuple);
        }

        [Test]
        public void Misnamed([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(QuadDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <QuadDec decName=""TestDec"">
                        <tuple>
                            <hello>1</hello>
                            <i>2</i>
                            <am>3</am>
                            <groot>4</groot>
                        </tuple>
                    </QuadDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, xmlValidator: xml => xml.Contains("li") && !xml.Contains("groot"));

            var result = Dec.Database<QuadDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(0, 0, 0, 0), result.tuple);
        }

        [Test]
        public void ForcedNames([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(QuadDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <QuadDec decName=""TestDec"">
                        <tuple>
                            <Item1>1</Item1>
                            <Item2>2</Item2>
                            <Item3>3</Item3>
                            <Item4>4</Item4>
                        </tuple>
                    </QuadDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<QuadDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(1, 2, 3, 4), result.tuple);
        }

        public class NamedDec : Dec.Dec
        {
            public (int klaatu, int barada, int nikto) kbn;
        }

        [Test]
        public void Named([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedDec decName=""TestDec"">
                        <kbn>
                            <klaatu>61</klaatu>
                            <barada>62</barada>
                            <nikto>63</nikto>
                        </kbn>
                    </NamedDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode, xmlValidator: xml => xml.Contains("klaatu") && xml.Contains("barada") && xml.Contains("nikto"));

            var result = Dec.Database<NamedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(61, result.kbn.klaatu);
            Assert.AreEqual(62, result.kbn.barada);
            Assert.AreEqual(63, result.kbn.nikto);
        }

        [Test]
        public void NamedReordered([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedDec decName=""TestDec"">
                        <kbn>
                            <klaatu>61</klaatu>
                            <nikto>63</nikto>
                            <barada>62</barada>
                        </kbn>
                    </NamedDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<NamedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(61, result.kbn.klaatu);
            Assert.AreEqual(62, result.kbn.barada);
            Assert.AreEqual(63, result.kbn.nikto);
        }

        [Test]
        public void NamedLi([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedDec decName=""TestDec"">
                        <kbn>
                            <li>61</li>
                            <li>62</li>
                            <li>63</li>
                        </kbn>
                    </NamedDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<NamedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(61, result.kbn.klaatu);
            Assert.AreEqual(62, result.kbn.barada);
            Assert.AreEqual(63, result.kbn.nikto);
        }

        [Test]
        public void NamedPartial([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedDec decName=""TestDec"">
                        <kbn>
                            <klaatu>61</klaatu>
                            <nikto>63</nikto>
                        </kbn>
                    </NamedDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<NamedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(61, result.kbn.klaatu);
            Assert.AreEqual(0, result.kbn.barada);
            Assert.AreEqual(63, result.kbn.nikto);
        }

        [Test]
        public void Duplicate([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedDec decName=""TestDec"">
                        <kbn>
                            <klaatu>61</klaatu>
                            <barada>62</barada>
                            <barada>64</barada>
                            <nikto>63</nikto>
                        </kbn>
                    </NamedDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, xmlValidator: xml => xml.Contains("klaatu") && xml.Contains("barada") && xml.Contains("nikto"));

            var result = Dec.Database<NamedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(61, result.kbn.klaatu);
            Assert.AreEqual(64, result.kbn.barada);
            Assert.AreEqual(63, result.kbn.nikto);
        }

        public class NamedAsLiDec : Dec.Dec
        {
            public (int klaatu, int li, int nikto) kbn;
        }

        [Test]
        public void NamedAsLiList([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedAsLiDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedAsLiDec decName=""TestDec"">
                        <kbn>
                            <li>61</li>
                            <li>62</li>
                            <li>63</li>
                        </kbn>
                    </NamedAsLiDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<NamedAsLiDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(61, result.kbn.klaatu);
            Assert.AreEqual(62, result.kbn.li);
            Assert.AreEqual(63, result.kbn.nikto);
        }

        [Test]
        public void NamedAsLiNames([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedAsLiDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedAsLiDec decName=""TestDec"">
                        <kbn>
                            <klaatu>61</klaatu>
                            <li>62</li>
                            <nikto>63</nikto>
                        </kbn>
                    </NamedAsLiDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<NamedAsLiDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(61, result.kbn.klaatu);
            Assert.AreEqual(62, result.kbn.li);
            Assert.AreEqual(63, result.kbn.nikto);
        }

        [Test]
        public void NamedAsLiSingle([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedAsLiDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedAsLiDec decName=""TestDec"">
                        <kbn>
                            <li>61</li>
                        </kbn>
                    </NamedAsLiDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<NamedAsLiDec>.Get("TestDec");
            Assert.IsNotNull(result);

            // I don't even care what the result is, this is your own fault you horrible person
        }
    }
}
