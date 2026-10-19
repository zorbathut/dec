using System;
using System.Collections.Generic;
using NUnit.Framework;

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
            ExpectErrors(() => parser.Finish(), err => err.Contains("Too many indices"));

            DoParserTests(mode, rewrite_expectParseErrors: true, errorValidator: err => err.Contains("Too many indices"));

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

        public class PatchedIndexCarrier
        {
            [Dec.Index] public int index;
            public int x;
            public int y;
            public int z;
        }

        public class PatchedIndexDec : Dec.Dec
        {
            public PatchedIndexCarrier obj;
            public Dictionary<string, PatchedIndexCarrier> dict;
        }

        [Test]
        public void PatchedIndexOnce([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PatchedIndexDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PatchedIndexDec decName=""Parent"" abstract=""true"">
                        <obj><x>1</x></obj>
                    </PatchedIndexDec>
                    <PatchedIndexDec decName=""TestDec"" parent=""Parent"">
                        <obj><y>2</y></obj>
                    </PatchedIndexDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PatchedIndexDec decName=""TestDec"" mode=""patch"">
                        <obj><z>3</z></obj>
                    </PatchedIndexDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var obj = Dec.Database<PatchedIndexDec>.Get("TestDec").obj;
            Assert.AreEqual(1, obj.x);
            Assert.AreEqual(2, obj.y);
            Assert.AreEqual(3, obj.z);
            Assert.AreEqual(1, Dec.Index<PatchedIndexCarrier>.Count);
            Assert.AreEqual(0, obj.index);
            Assert.AreSame(obj, Dec.Index<PatchedIndexCarrier>.Get(0));
        }

        [Test]
        public void DictValuePatchIndexOnce([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PatchedIndexDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PatchedIndexDec decName=""Parent"" abstract=""true"">
                        <dict><a><x>1</x></a></dict>
                    </PatchedIndexDec>
                    <PatchedIndexDec decName=""TestDec"" parent=""Parent"">
                        <dict mode=""patch""><a><y>2</y></a></dict>
                    </PatchedIndexDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var value = Dec.Database<PatchedIndexDec>.Get("TestDec").dict["a"];
            Assert.AreEqual(1, value.x);
            Assert.AreEqual(2, value.y);
            Assert.AreEqual(1, Dec.Index<PatchedIndexCarrier>.Count);
            Assert.AreSame(value, Dec.Index<PatchedIndexCarrier>.Get(0));
        }

        public class NestedReplaceIndexHolder
        {
            public List<ParentReplaceIndexCarrier> list;
        }

        public class NestedReplaceIndexDec : Dec.Dec
        {
            public NestedReplaceIndexHolder holder;
        }

        [Test]
        public void NestedReplaceIndex([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NestedReplaceIndexDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NestedReplaceIndexDec decName=""Parent"" abstract=""true"">
                        <holder><list><li /><li /></list></holder>
                    </NestedReplaceIndexDec>

                    <NestedReplaceIndexDec decName=""TestDec"" parent=""Parent"">
                        <holder><list><li /></list></holder>
                    </NestedReplaceIndexDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Index<ParentReplaceIndexCarrier>.Count);
        }

        public class DictReplaceIndexDec : Dec.Dec
        {
            public Dictionary<string, List<ParentReplaceIndexCarrier>> dict;
        }

        [Test]
        public void DictValueReplaceIndex([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DictReplaceIndexDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DictReplaceIndexDec decName=""Parent"" abstract=""true"">
                        <dict><a><li /><li /></a></dict>
                    </DictReplaceIndexDec>

                    <DictReplaceIndexDec decName=""TestDec"" parent=""Parent"">
                        <dict mode=""patch""><a><li /></a></dict>
                    </DictReplaceIndexDec>
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
