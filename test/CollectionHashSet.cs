namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class CollectionHashSet : Base
    {
        public class HashSetStringDec : Dec.Dec
        {
            public HashSet<string> data;
        }

        [Test]
        public void HashSetString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <Hello />
                            <li>Goodbye</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Hello", "Goodbye" });
        }

        [Test]
        public void HashSetDuplicate([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <li>Prefix</li>
                            <li>Dupe</li>
                            <li>Dupe</li>
                            <li>Suffix</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "Prefix", "Dupe", "Suffix" });
        }

        public class HashSetStringOverrideDec : Dec.Dec
        {
            public HashSet<string> dataA = new HashSet<string> { "a", "b", "c" };
            public HashSet<string> dataB = new HashSet<string> { "d", "e", "f" };
            public HashSet<string> dataC = new HashSet<string> { "g", "h", "i" };
        }

        [Test]
        public void HashSetOverrideString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringOverrideDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <HashSetStringOverrideDec decName=""TestDec"">
                        <dataA>
                            <u />
                        </dataA>
                        <dataB />
                    </HashSetStringOverrideDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<HashSetStringOverrideDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.dataA, new HashSet<string> { "u" });
            Assert.AreEqual(result.dataB, new HashSet<string> { });
            Assert.AreEqual(result.dataC, new HashSet<string> { "g", "h", "i" });
        }

        [Test]
        public void HashSetEmptyString([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HashSetStringDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <HashSetStringDec decName=""TestDec"">
                        <data>
                            <li></li>
                            <li>four</li>
                        </data>
                    </HashSetStringDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<HashSetStringDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.data, new HashSet<string> { "", "four" });
        }
    }
}
