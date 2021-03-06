namespace DecTest
{
    using NUnit.Framework;
    using System;

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
        public void Basic([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefTargetDec decName=""Target"" />
                    <RefSourceDec decName=""Source"">
                        <target>Target</target>
                    </RefSourceDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var target = Dec.Database<RefTargetDec>.Get("Target");
            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
        }

        [Test]
        public void Reversed([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefSourceDec decName=""Source"">
                        <target>Target</target>
                    </RefSourceDec>
                    <RefTargetDec decName=""Target"" />
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var target = Dec.Database<RefTargetDec>.Get("Target");
            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
        }

        [Test]
        public void Multistring([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefSourceDec decName=""Source"">
                        <target>Target</target>
                    </RefSourceDec>
                </Decs>");
            parser.AddString(@"
                <Decs>
                    <RefTargetDec decName=""Target"" />
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var target = Dec.Database<RefTargetDec>.Get("Target");
            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
        }

        [Test]
        public void Refdec([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefSourceDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefSourceDec decName=""Source"">
                        Source
                    </RefSourceDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var source = Dec.Database<RefSourceDec>.Get("Source");
            Assert.IsNotNull(source);
        }

        [Test]
        public void Circular([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefCircularDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefCircularDec decName=""Alpha"">
                        <target>Beta</target>
                    </RefCircularDec>
                    <RefCircularDec decName=""Beta"">
                        <target>Alpha</target>
                    </RefCircularDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var alpha = Dec.Database<RefCircularDec>.Get("Alpha");
            var beta = Dec.Database<RefCircularDec>.Get("Beta");
            Assert.IsNotNull(alpha);
            Assert.IsNotNull(beta);

            Assert.AreEqual(alpha.target, beta);
            Assert.AreEqual(beta.target, alpha);
        }

        [Test]
        public void CircularTight([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefCircularDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefCircularDec decName=""TestDec"">
                        <target>TestDec</target>
                    </RefCircularDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<RefCircularDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.target, result);
        }

        [Test]
        public void NullRef([Values] BehaviorMode mode)
        {
            // This is a little wonky; we have to test it by duplicating a tag, which is technically an error
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefCircularDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefCircularDec decName=""TestDec"">
                        <target>TestDec</target>
                        <target></target>
                    </RefCircularDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<RefCircularDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
        }

        [Test]
        public void FailedLookup([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefSourceDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <RefSourceDec decName=""TestDec"">
                        <target>MissingDec</target>
                    </RefSourceDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Dec.Database<RefSourceDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
        }

        public class BareDecDec : Dec.Dec
        {
            public Dec.Dec target;
        }

        [Test]
        public void BareDec([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(BareDecDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <BareDecDec decName=""TestDec"">
                        <target>TestDec</target>
                    </BareDecDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            var result = Dec.Database<BareDecDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
        }
    }
}
