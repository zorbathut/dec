namespace DecTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Xml : Base
    {
        [Test]
        public void DTDParse([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                <Decs>
                    <StubDec decName=""TestDec"">
                    </StubDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
        }

        [Test]
        public void IncorrectRoot([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } };

            var parser = new Dec.Parser();
            ExpectWarnings(() => parser.AddString(@"
                <NotDecs>
                    <StubDec decName=""TestDec"" />
                </NotDecs>"));
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
        }

        [Test]
        public void MultipleRoot([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } };

            var parser = new Dec.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                </Decs>
                <Decs>
                    <StubDec decName=""TestDecB"" />
                </Decs>"));
            parser.Finish();

            DoBehavior(mode);

            // Currently not providing any guarantees on whether these get parsed; I'd actually like for them to get parsed, but doing so is tricky
        }

        [Test]
        public void MultiXml([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                </Decs>");
            parser.AddString(@"
                <Decs>
                    <StubDec decName=""TestDecB"" />
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDecA"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDecB"));
        }

        [Test]
        public void ProvidedFilenameForXml([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) } };

            var parser = new Dec.Parser();
            ExpectErrors(() => parser.AddString(@"test.xml"));
            parser.Finish();

            DoBehavior(mode);
        }

        [Test]
        public void ProperStringName([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                </Decs>", "TestStringName"), str => str.StartsWith("TestStringName"));
            parser.Finish();

            DoBehavior(mode);
        }

        [Test]
        public void Garbage([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            ExpectErrors(() => parser.AddString(@"�SimpleDec decName=""Hello""><value>3</value></SimpleDec>"));
            parser.Finish();

            DoBehavior(mode);
        }
    }
}
