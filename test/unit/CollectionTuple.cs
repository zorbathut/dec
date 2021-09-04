namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class CollectionTuple : Base
    {
        public class SimpleDec : Dec.Dec
        {
            public Tuple<int, string> t_is;
            public Tuple<string, int> t_si;
            public Tuple<List<int>, Dictionary<string, int>> t_ld;

            public Tuple<int> t_1;
            public Tuple<int, int> t_2;
            public Tuple<int, int, int> t_3;
            public Tuple<int, int, int, int> t_4;
            public Tuple<int, int, int, int, int, int, int> t_7;
        }

        [Test]
        public void Simple([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(SimpleDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <SimpleDec decName=""TestDec"">
                        <t_is>
                            <li>4</li>
                            <li>cows</li>
                        </t_is>
                        <t_si>
                            <li>horses</li>
                            <li>8</li>
                        </t_si>
                        <t_ld>
                            <li>
                                <li>1</li>
                                <li>2</li>
                                <li>3</li>
                            </li>
                            <li>
                                <sheep>9</sheep>
                                <goats>99</goats>
                            </li>
                        </t_ld>

                        <t_1>
                            <li>11</li>
                        </t_1>
                        <t_2>
                            <li>21</li>
                            <li>22</li>
                        </t_2>
                        <t_3>
                            <li>31</li>
                            <li>32</li>
                            <li>33</li>
                        </t_3>
                        <t_4>
                            <li>41</li>
                            <li>42</li>
                            <li>43</li>
                            <li>44</li>
                        </t_4>
                        <t_7>
                            <li>51</li>
                            <li>52</li>
                            <li>53</li>
                            <li>54</li>
                            <li>55</li>
                            <li>56</li>
                            <li>57</li>
                        </t_7>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<SimpleDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(4, "cows"), result.t_is);
            Assert.AreEqual(Tuple.Create("horses", 8), result.t_si);
            Assert.AreEqual(Tuple.Create(new List<int>() { 1, 2, 3 }, new Dictionary<string, int> { { "sheep", 9 }, { "goats", 99 } }), result.t_ld);

            Assert.AreEqual(Tuple.Create(11), result.t_1);
            Assert.AreEqual(Tuple.Create(21, 22), result.t_2);
            Assert.AreEqual(Tuple.Create(31, 32, 33), result.t_3);
            Assert.AreEqual(Tuple.Create(41, 42, 43, 44), result.t_4);
            Assert.AreEqual(Tuple.Create(51, 52, 53, 54, 55, 56, 57), result.t_7);
        }

        public class ValueDec : Dec.Dec
        {
            public (int, string) t_is;
            public (string, int) t_si;
            public (List<int>, Dictionary<string, int>) t_ld;

            public ValueTuple<int> t_1; // syntactic sugar doesn't work for this one, but it's still valid
            public (int, int) t_2;
            public (int, int, int) t_3;
            public (int, int, int, int) t_4;
            public (int, int, int, int, int, int, int) t_7;
        }

        [Test]
        public void Value([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ValueDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ValueDec decName=""TestDec"">
                        <t_is>
                            <li>4</li>
                            <li>cows</li>
                        </t_is>
                        <t_si>
                            <li>horses</li>
                            <li>8</li>
                        </t_si>
                        <t_ld>
                            <li>
                                <li>1</li>
                                <li>2</li>
                                <li>3</li>
                            </li>
                            <li>
                                <sheep>9</sheep>
                                <goats>99</goats>
                            </li>
                        </t_ld>

                        <t_1>
                            <li>11</li>
                        </t_1>
                        <t_2>
                            <li>21</li>
                            <li>22</li>
                        </t_2>
                        <t_3>
                            <li>31</li>
                            <li>32</li>
                            <li>33</li>
                        </t_3>
                        <t_4>
                            <li>41</li>
                            <li>42</li>
                            <li>43</li>
                            <li>44</li>
                        </t_4>
                        <t_7>
                            <li>51</li>
                            <li>52</li>
                            <li>53</li>
                            <li>54</li>
                            <li>55</li>
                            <li>56</li>
                            <li>57</li>
                        </t_7>
                    </ValueDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ValueDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(ValueTuple.Create(4, "cows"), result.t_is);
            Assert.AreEqual(ValueTuple.Create("horses", 8), result.t_si);
            Assert.AreEqual(ValueTuple.Create(new List<int>() { 1, 2, 3 }, new Dictionary<string, int> { { "sheep", 9 }, { "goats", 99 } }), result.t_ld);

            Assert.AreEqual(ValueTuple.Create(11), result.t_1);
            Assert.AreEqual(ValueTuple.Create(21, 22), result.t_2);
            Assert.AreEqual(ValueTuple.Create(31, 32, 33), result.t_3);
            Assert.AreEqual(ValueTuple.Create(41, 42, 43, 44), result.t_4);
            Assert.AreEqual(ValueTuple.Create(51, 52, 53, 54, 55, 56, 57), result.t_7);
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
        public void TooFew([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(QuadDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<QuadDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(1, 2, 3, 0), result.tuple);
        }

        [Test]
        public void TooMany([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(QuadDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<QuadDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(1, 2, 3, 4), result.tuple);
        }

        [Test]
        public void Misnamed([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(QuadDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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

            DoBehavior(mode);

            var result = Dec.Database<QuadDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(Tuple.Create(1, 2, 3, 4), result.tuple);
        }
    }
}
