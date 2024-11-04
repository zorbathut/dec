using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class References : Base
    {
        public class RefTargetDec : Dec.Dec
        {

        }

        public class RefSourceDec : Dec.Dec
        {
            public RefTargetDec target;
        }

        public class RefCircularDec : Dec.Dec
        {
            public RefCircularDec target;
        }

        [Test]
        public void Basic([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefTargetDec decName=""Target"" />
                    <RefSourceDec decName=""Source"">
                        <target>Target</target>
                    </RefSourceDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var target = Dec.Database<RefTargetDec>.Get("Target");
            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
        }

        [Test]
        public void Reversed([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefSourceDec decName=""Source"">
                        <target>Target</target>
                    </RefSourceDec>
                    <RefTargetDec decName=""Target"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var target = Dec.Database<RefTargetDec>.Get("Target");
            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
        }

        [Test]
        public void Multistring([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefSourceDec decName=""Source"">
                        <target>Target</target>
                    </RefSourceDec>
                </Decs>");
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefTargetDec decName=""Target"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var target = Dec.Database<RefTargetDec>.Get("Target");
            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
        }

        [Test]
        public void Refdec([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefSourceDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefSourceDec decName=""Source"">
                        Source
                    </RefSourceDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(source);
        }

        [Test]
        public void Circular([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefCircularDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefCircularDec decName=""Alpha"">
                        <target>Beta</target>
                    </RefCircularDec>
                    <RefCircularDec decName=""Beta"">
                        <target>Alpha</target>
                    </RefCircularDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var alpha = Dec.Database<RefCircularDec>.Get("Alpha");
            var beta = Dec.Database<RefCircularDec>.Get("Beta");
            Assert.IsNotNull(alpha);
            Assert.IsNotNull(beta);

            Assert.AreEqual(alpha.target, beta);
            Assert.AreEqual(beta.target, alpha);
        }

        [Test]
        public void CircularTight([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefCircularDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefCircularDec decName=""TestDec"">
                        <target>TestDec</target>
                    </RefCircularDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<RefCircularDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.target, result);
        }

        [Test]
        public void NullRef([Values] ParserMode mode)
        {
            // This is a little wonky; we have to test it by duplicating a tag, which is technically an error
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefCircularDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefCircularDec decName=""TestDec"">
                        <target>TestDec</target>
                        <target></target>
                    </RefCircularDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<RefCircularDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
        }

        [Test]
        public void FailedLookup([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefSourceDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefSourceDec decName=""TestDec"">
                        <target>MissingDec</target>
                    </RefSourceDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<RefSourceDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
        }

        public class BareDecDec : Dec.Dec
        {
            public Dec.Dec target;
        }

        [Test]
        public void BareDec([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(BareDecDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BareDecDec decName=""TestDec"">
                        <target>TestDec</target>
                    </BareDecDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<BareDecDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
        }

        public class RecordableDecDec : Dec.Dec, Dec.IRecordable
        {
            public RecordableDecDec link;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref link, "link");
            }
        }

        [Test]
        public void RecordableDec([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RecordableDecDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RecordableDecDec decName=""TestADec"">
                        <link>TestADec</link>
                    </RecordableDecDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<RecordableDecDec>.Get("TestADec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.link, result);
        }
    }
}
