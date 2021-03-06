namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class CollectionList : Base
    {
        public class ListDec : Dec.Dec
        {
            public List<int> data;
        }

        [Test]
        public void Basic([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ListDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ListDec decName=""TestDec"">
                        <data>
                            <li>10</li>
                            <li>9</li>
                            <li>8</li>
                            <li>7</li>
                            <li>6</li>
                        </data>
                    </ListDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ListDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new[] { 10, 9, 8, 7, 6 });
        }

        public class ListOverrideDec : Dec.Dec
        {
            public List<int> dataA = new List<int> { 3, 4, 5 };
            public List<int> dataB = new List<int> { 6, 7, 8 };
            public List<int> dataC = new List<int> { 9, 10, 11 };
        }

        [Test]
        public void Override([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ListOverrideDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ListOverrideDec decName=""TestDec"">
                        <dataA>
                            <li>2020</li>
                        </dataA>
                        <dataB />
                    </ListOverrideDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ListOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new[] { 2020 });
            Assert.AreEqual(result.dataB, new int[0] );
            Assert.AreEqual(result.dataC, new[] { 9, 10, 11 });
        }
    }
}
