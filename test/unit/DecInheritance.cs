using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DecTest
{
    [TestFixture]
    public class DecInheritance : Base
    {
        public class SimpleDec : Dec.Dec
        {
            public int defaulted;
            public int overridden;
            public int doubleOverridden;

            public class SubObject
            {
                public int defaulted;
                public int overridden;
            }
            public SubObject subObject;

            public List<int> list;
        }

        [Test]
        public void Simple([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDec>

                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(3, result.defaulted);
            Assert.AreEqual(60, result.overridden);
            Assert.AreEqual(12, result.subObject.defaulted);
            Assert.AreEqual(90, result.subObject.overridden);

            Assert.IsNull(Dec.Database<SimpleDec>.Get("Base"));
        }

        [Test]
        public void List([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"">
                        <list>
                          <li>2</li>
                          <li>4</li>
                        </list>
                    </SimpleDec>

                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <list>
                          <li>50</li>
                        </list>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(1, result.list.Count);
            Assert.AreEqual(50, result.list[0]);

            Assert.IsNull(Dec.Database<SimpleDec>.Get("Base"));
        }

        [Test]
        public void FromNonAbstract([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDec>

                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var bas = Dec.Database<SimpleDec>.Get("Base");
            Assert.AreEqual(3, bas.defaulted);
            Assert.AreEqual(42, bas.overridden);
            Assert.AreEqual(12, bas.subObject.defaulted);
            Assert.AreEqual(80, bas.subObject.overridden);

            var thing = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(3, thing.defaulted);
            Assert.AreEqual(60, thing.overridden);
            Assert.AreEqual(12, thing.subObject.defaulted);
            Assert.AreEqual(90, thing.subObject.overridden);
        }

        [Test]
        public void MissingConcrete([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(0, Dec.Database<SimpleDec>.Count);
        }

        [Test]
        public void MissingBase([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(60, result.overridden);
            Assert.AreEqual(90, result.subObject.overridden);
        }

        [Test]
        public void AbstractOverride([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"" />
                    <SimpleDec decName=""Base"" abstract=""true"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.AreEqual(0, Dec.Database<SimpleDec>.Count);
        }

        [Test]
        public void AbstractNonabstractClash([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Before"" abstract=""true"">
                        <overridden>10</overridden>
                     </SimpleDec>
                    <SimpleDec decName=""Before"">
                        <overridden>20</overridden>
                     </SimpleDec>
                    <SimpleDec decName=""After"">
                        <overridden>30</overridden>
                     </SimpleDec>
                    <SimpleDec decName=""After"" abstract=""true"">
                        <overridden>40</overridden>
                     </SimpleDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.AreEqual(20, Dec.Database<SimpleDec>.Get("Before").overridden);
            Assert.AreEqual(30, Dec.Database<SimpleDec>.Get("After").overridden);
        }

        [Test]
        public void BadAbstractTag([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Obj"" abstract=""cheese"">
                        <overridden>10</overridden>
                     </SimpleDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.AreEqual(10, Dec.Database<SimpleDec>.Get("Obj").overridden);
        }

        [Test]
        public void DoubleInheritance([Values] bool trunkAbstract, [Values] bool branchAbstract, [Values] bool leafAbstract, [Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <SimpleDec decName=""Trunk"" {(trunkAbstract ? "abstract=\"true\"" : "")}>
                        <defaulted>1</defaulted>
                        <overridden>2</overridden>
                        <doubleOverridden>3</doubleOverridden>
                     </SimpleDec>
                    <SimpleDec decName=""Branch"" parent=""Trunk"" {(branchAbstract ? "abstract=\"true\"" : "")}>
                        <overridden>20</overridden>
                        <doubleOverridden>30</doubleOverridden>
                     </SimpleDec>
                    <SimpleDec decName=""Leaf"" parent=""Branch"" {(leafAbstract ? "abstract=\"true\"" : "")}>
                        <doubleOverridden>300</doubleOverridden>
                     </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            if (trunkAbstract)
            {
                Assert.IsNull(Dec.Database<SimpleDec>.Get("Trunk"));
            }
            else
            {
                Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("Trunk").defaulted);
                Assert.AreEqual(2, Dec.Database<SimpleDec>.Get("Trunk").overridden);
                Assert.AreEqual(3, Dec.Database<SimpleDec>.Get("Trunk").doubleOverridden);
            }

            if (branchAbstract)
            {
                Assert.IsNull(Dec.Database<SimpleDec>.Get("Branch"));
            }
            else
            {
                Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("Branch").defaulted);
                Assert.AreEqual(20, Dec.Database<SimpleDec>.Get("Branch").overridden);
                Assert.AreEqual(30, Dec.Database<SimpleDec>.Get("Branch").doubleOverridden);
            }

            if (leafAbstract)
            {
                Assert.IsNull(Dec.Database<SimpleDec>.Get("Leaf"));
            }
            else
            {
                Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("Leaf").defaulted);
                Assert.AreEqual(20, Dec.Database<SimpleDec>.Get("Leaf").overridden);
                Assert.AreEqual(300, Dec.Database<SimpleDec>.Get("Leaf").doubleOverridden);
            }
        }
    }
}
