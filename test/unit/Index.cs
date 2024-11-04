using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DecTest
{
    [TestFixture]
    public class Index : Base
    {
        public class IndexBaseDec : Dec.Dec
        {
            [Dec.Index] public int index;
        }

        public class IndexDerivedDec : IndexBaseDec
        {
            [Dec.Index] public new int index;
        }

        public class IndexLeafDec : StubDec
        {
            [Dec.Index] public int index;
        }

        [Test]
        public void IndexBaseList([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndexBaseDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IndexBaseDec decName=""TestDecA"" />
                    <IndexBaseDec decName=""TestDecB"" />
                    <IndexBaseDec decName=""TestDecC"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecA"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecA").index));
            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecB"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecB").index));
            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecC"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecC").index));

            Assert.AreEqual(3, Dec.Index<IndexBaseDec>.Count);
        }

        [Test]
        public void IndexDerivedList([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndexBaseDec), typeof(IndexDerivedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IndexDerivedDec decName=""TestDecA"" />
                    <IndexBaseDec decName=""TestDecB"" />
                    <IndexDerivedDec decName=""TestDecC"" />
                    <IndexBaseDec decName=""TestDecD"" />
                    <IndexDerivedDec decName=""TestDecE"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecA"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecA").index));
            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecB"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecB").index));
            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecC"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecC").index));
            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecD"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecD").index));
            Assert.AreSame(Dec.Database<IndexBaseDec>.Get("TestDecE"), Dec.Index<IndexBaseDec>.Get(Dec.Database<IndexBaseDec>.Get("TestDecE").index));

            Assert.AreSame(Dec.Database<IndexDerivedDec>.Get("TestDecA"), Dec.Index<IndexDerivedDec>.Get(Dec.Database<IndexDerivedDec>.Get("TestDecA").index));
            Assert.AreSame(Dec.Database<IndexDerivedDec>.Get("TestDecC"), Dec.Index<IndexDerivedDec>.Get(Dec.Database<IndexDerivedDec>.Get("TestDecC").index));
            Assert.AreSame(Dec.Database<IndexDerivedDec>.Get("TestDecE"), Dec.Index<IndexDerivedDec>.Get(Dec.Database<IndexDerivedDec>.Get("TestDecE").index));

            Assert.AreEqual(5, Dec.Index<IndexBaseDec>.Count);
            Assert.AreEqual(3, Dec.Index<IndexDerivedDec>.Count);
        }

        [Test]
        public void IndexLeafList([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndexLeafDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IndexLeafDec decName=""TestDecA"" />
                    <IndexLeafDec decName=""TestDecB"" />
                    <IndexLeafDec decName=""TestDecC"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreSame(Dec.Database<IndexLeafDec>.Get("TestDecA"), Dec.Index<IndexLeafDec>.Get(Dec.Database<IndexLeafDec>.Get("TestDecA").index));
            Assert.AreSame(Dec.Database<IndexLeafDec>.Get("TestDecB"), Dec.Index<IndexLeafDec>.Get(Dec.Database<IndexLeafDec>.Get("TestDecB").index));
            Assert.AreSame(Dec.Database<IndexLeafDec>.Get("TestDecC"), Dec.Index<IndexLeafDec>.Get(Dec.Database<IndexLeafDec>.Get("TestDecC").index));

            Assert.AreEqual(3, Dec.Index<IndexLeafDec>.Count);
        }

        public class IndependentIndexClass
        {
            [Dec.Index] public int index;
        }

        public struct IndependentIndexStruct
        {
            [Dec.Index] public int index;

            public int value;
        }

        public class IndependentIndexDec : Dec.Dec
        {
            public IndependentIndexClass iicPrefilled = new IndependentIndexClass();
            public IndependentIndexClass iicUnfilled;
            public IndependentIndexStruct iis = new IndependentIndexStruct();
        }

        [Test]
        public void IndependentIndex([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndependentIndexDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IndependentIndexDec decName=""TestDecA"">
                        <iicPrefilled />
                        <iicUnfilled />
                        <iis><value>7</value></iis>
                    </IndependentIndexDec>
                    <IndependentIndexDec decName=""TestDecB"">
                        <iicPrefilled />
                        <iis><value>8</value></iis>
                    </IndependentIndexDec>
                    <IndependentIndexDec decName=""TestDecC"">
                        <iicPrefilled />
                        <iicUnfilled />
                        <iis><value>9</value></iis>
                    </IndependentIndexDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            // At the moment, the expected behavior is that classes which are explicitly mentioned get indices, classes which default to null and aren't mentioned don't get indices.
            // Classes which default to objects and aren't explicitly mentioned are in a gray area where we currently don't guarantee anything.
            // This needs to be fixed, but I'm not sure how to do it; I've got a few ideas but nothing I really like.

            // Structs, also, are required to be explicitly created.

            // *addendum*: I know how to fix this, it's just going to be a pain.

            Assert.AreSame(Dec.Database<IndependentIndexDec>.Get("TestDecA").iicPrefilled, Dec.Index<IndependentIndexClass>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecA").iicPrefilled.index));
            Assert.AreSame(Dec.Database<IndependentIndexDec>.Get("TestDecB").iicPrefilled, Dec.Index<IndependentIndexClass>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecB").iicPrefilled.index));
            Assert.AreSame(Dec.Database<IndependentIndexDec>.Get("TestDecC").iicPrefilled, Dec.Index<IndependentIndexClass>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecC").iicPrefilled.index));

            Assert.AreSame(Dec.Database<IndependentIndexDec>.Get("TestDecA").iicUnfilled, Dec.Index<IndependentIndexClass>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecA").iicUnfilled.index));
            Assert.AreSame(Dec.Database<IndependentIndexDec>.Get("TestDecC").iicUnfilled, Dec.Index<IndependentIndexClass>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecC").iicUnfilled.index));

            Assert.AreEqual(Dec.Database<IndependentIndexDec>.Get("TestDecA").iis, Dec.Index<IndependentIndexStruct>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecA").iis.index));
            Assert.AreEqual(Dec.Database<IndependentIndexDec>.Get("TestDecB").iis, Dec.Index<IndependentIndexStruct>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecB").iis.index));
            Assert.AreEqual(Dec.Database<IndependentIndexDec>.Get("TestDecC").iis, Dec.Index<IndependentIndexStruct>.Get(Dec.Database<IndependentIndexDec>.Get("TestDecC").iis.index));

            Assert.AreEqual(5, Dec.Index<IndependentIndexClass>.Count);
            Assert.AreEqual(3, Dec.Index<IndependentIndexStruct>.Count);
        }

        public class ExcessiveIndicesDec : Dec.Dec
        {
            [Dec.Index] public int indexA;
            [Dec.Index] public int indexB;
        }

        [Test]
        public void ExcessiveIndices([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExcessiveIndicesDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExcessiveIndicesDec decName=""TestDecA"" />
                    <ExcessiveIndicesDec decName=""TestDecB"" />
                    <ExcessiveIndicesDec decName=""TestDecC"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            // It's guaranteed that either indexA or indexB has some behavior that causes 0, 1, and 2 to be distributed among TestDec.
            // This is a massive over-constraint of allowable behavior; loosen it up if it fails somewhere.
            Assert.AreEqual(0, Dec.Database<ExcessiveIndicesDec>.Get("TestDecA").indexB);
            Assert.AreEqual(1, Dec.Database<ExcessiveIndicesDec>.Get("TestDecB").indexB);
            Assert.AreEqual(2, Dec.Database<ExcessiveIndicesDec>.Get("TestDecC").indexB);
        }

        public class ParentReplaceIndexCarrier
        {
            [Dec.Index] public int index;
        }

        public class ParentReplaceIndexDec : Dec.Dec
        {
            public List<ParentReplaceIndexCarrier> list;
        }

        [Test]
        [Ignore("Currently broken :(")]
        public void ParentReplaceIndex([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ParentReplaceIndexDec), typeof(ParentReplaceIndexCarrier) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ParentReplaceIndexDec decName=""Parent"" abstract=""true"">
                        <list>
                            <li />
                            <li />
                        </list>
                    </ParentReplaceIndexDec>

                    <ParentReplaceIndexDec decName=""TestDec"" parent=""Parent"">
                        <list mode=""replace"">
                            <li />
                        </list>
                    </ParentReplaceIndexDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Index<ParentReplaceIndexCarrier>.Count);
        }

        [Test]
        public void Multifile([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IndexBaseDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IndexBaseDec decName=""TestDecA"" />
                    <IndexBaseDec decName=""TestDecB"" />
                    <IndexBaseDec decName=""TestDecC"" />
                </Decs>");
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IndexBaseDec decName=""TestDecD"" />
                    <IndexBaseDec decName=""TestDecE"" />
                    <IndexBaseDec decName=""TestDecF"" />
                </Decs>");
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IndexBaseDec decName=""TestDecG"" />
                    <IndexBaseDec decName=""TestDecH"" />
                    <IndexBaseDec decName=""TestDecI"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var indices = new HashSet<int>();
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecA").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecB").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecC").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecD").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecE").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecF").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecG").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecH").index);
            indices.Add(Dec.Database<IndexBaseDec>.Get("TestDecI").index);

            Assert.AreEqual(9, indices.Count);
            Assert.AreEqual(9, Dec.Index<IndexBaseDec>.Count);
        }
    }
}
