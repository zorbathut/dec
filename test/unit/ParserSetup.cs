using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ParserSetup : Base
    {
        private static List<string> setupOrder;
        private static readonly object setupOrderLock = new object();

        private static void RecordSetup(string entry)
        {
            lock (setupOrderLock)
            {
                setupOrder.Add(entry);
            }
        }

        [SetUp]
        public void SetUp()
        {
            setupOrder = new List<string>();
            SharedConv_Payload.Singleton = new SharedConv_Payload();
        }

        // Classes for TestStaticRunsOnce
        public class StaticRun_Dec : Dec.Dec { }
        public static class StaticRun_Holder
        {
            [Dec.Setup]
            internal static void Init(Action<string> reporter)
            {
                RecordSetup("StaticRun_Holder.Init");
            }
        }

        [Test]
        public void TestStaticRunsOnce()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StaticRun_Dec) }, explicitSetupScanTypes = new Type[] { typeof(StaticRun_Holder) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StaticRun_Dec decName=""A"" />
                    <StaticRun_Dec decName=""B"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "StaticRun_Holder.Init" }, setupOrder);
        }

        // Classes for TestInstanceOnDecDecNameOrder
        public class InstOrder_Dec : Dec.Dec
        {
            [Dec.Setup]
            internal void Record(Action<string> reporter)
            {
                RecordSetup(DecName);
            }
        }

        [Test]
        public void TestInstanceOnDecDecNameOrder()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(InstOrder_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <InstOrder_Dec decName=""B"" />
                    <InstOrder_Dec decName=""A"" />
                    <InstOrder_Dec decName=""C"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, setupOrder);
        }

        // Classes for TestNonDecInstance
        public class Collected_Obj
        {
            public string tag;

            [Dec.Setup]
            internal void Record(Action<string> reporter)
            {
                RecordSetup($"obj:{tag}");
            }
        }
        public class Collected_Dec : Dec.Dec
        {
            public Collected_Obj obj;
        }

        [Test]
        public void TestNonDecInstance()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Collected_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Collected_Dec decName=""A"">
                        <obj><tag>alpha</tag></obj>
                    </Collected_Dec>
                    <Collected_Dec decName=""B"">
                        <obj><tag>beta</tag></obj>
                    </Collected_Dec>
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEquivalent(new[] { "obj:alpha", "obj:beta" }, setupOrder);
        }

        // Classes for TestSharedInstanceDedup
        public class SharedConv_Payload
        {
            public static SharedConv_Payload Singleton;

            [Dec.Setup]
            internal void Record(Action<string> reporter)
            {
                RecordSetup("shared");
            }
        }
        public class SharedConv_Converter : Dec.ConverterString<SharedConv_Payload>
        {
            public override SharedConv_Payload Read(string input, Dec.Context context)
            {
                return SharedConv_Payload.Singleton;
            }

            public override string Write(SharedConv_Payload input)
            {
                return "singleton";
            }
        }
        public class SharedConv_Dec : Dec.Dec
        {
            public SharedConv_Payload payload;
        }

        [Test]
        public void TestSharedInstanceDedup()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SharedConv_Dec) }, explicitConverters = new Type[] { typeof(SharedConv_Converter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SharedConv_Dec decName=""A"">
                        <payload>x</payload>
                    </SharedConv_Dec>
                    <SharedConv_Dec decName=""B"">
                        <payload>x</payload>
                    </SharedConv_Dec>
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "shared" }, setupOrder);
        }

        // Classes for TestMethodToMethodAfter
        public class M2M_ADec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(M2M_BDec), "MSecond")]
            internal void MFirst(Action<string> reporter)
            {
                RecordSetup("A.MFirst");
            }
        }
        public class M2M_BDec : Dec.Dec
        {
            [Dec.Setup]
            internal void MSecond(Action<string> reporter)
            {
                RecordSetup("B.MSecond");
            }
        }

        [Test]
        public void TestMethodToMethodAfter()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(M2M_ADec), typeof(M2M_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <M2M_ADec decName=""A"" />
                    <M2M_BDec decName=""B"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "B.MSecond", "A.MFirst" }, setupOrder);
        }

        // Classes for TestMethodToMethodBefore; tiebreak alone would run ADec's function first, the Before edge must invert that
        public class M2MB_ADec : Dec.Dec
        {
            [Dec.Setup]
            internal void MY(Action<string> reporter)
            {
                RecordSetup("A.MY");
            }
        }
        public class M2MB_ZDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupBefore(typeof(M2MB_ADec), "MY")]
            internal void MX(Action<string> reporter)
            {
                RecordSetup("Z.MX");
            }
        }

        [Test]
        public void TestMethodToMethodBefore()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(M2MB_ADec), typeof(M2MB_ZDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <M2MB_ADec decName=""A"" />
                    <M2MB_ZDec decName=""Z"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "Z.MX", "A.MY" }, setupOrder);
        }

        // Classes for TestLegacyNodeAddressing
        public class LegAddr_ADec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(LegAddr_BDec), "PostLoad")]
            internal void M(Action<string> reporter)
            {
                RecordSetup("A.M");
            }
        }
        public class LegAddr_BDec : Dec.Dec
        {
            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                RecordSetup("B.PostLoad");
            }
            #pragma warning restore CS0672
        }

        [Test]
        public void TestLegacyNodeAddressing()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LegAddr_ADec), typeof(LegAddr_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LegAddr_ADec decName=""A"" />
                    <LegAddr_BDec decName=""B"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "B.PostLoad", "A.M" }, setupOrder);
        }

        // Classes for TestBareTypeDepIsWholeStage
        public class Bare_ADec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(Bare_BDec))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("A.M");
            }
        }
        public class Bare_BDec : Dec.Dec
        {
            #pragma warning disable CS0672
            public override void ConfigErrors(Action<string> reporter)
            {
                RecordSetup("B.ConfigErrors");
            }
            #pragma warning restore CS0672

            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                RecordSetup("B.PostLoad");
            }
            #pragma warning restore CS0672

            [Dec.Setup]
            internal void MB(Action<string> reporter)
            {
                RecordSetup("B.MB");
            }
        }

        [Test]
        public void TestBareTypeDepIsWholeStage()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Bare_ADec), typeof(Bare_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Bare_ADec decName=""A"" />
                    <Bare_BDec decName=""B"" />
                </Decs>");
            parser.Finish();

            Assert.AreEqual(4, setupOrder.Count);
            Assert.AreEqual("A.M", setupOrder[3]);
            CollectionAssert.AreEquivalent(new[] { "B.ConfigErrors", "B.PostLoad", "B.MB" }, setupOrder.GetRange(0, 3));
        }

        // Classes for TestStageMarker; the Before constraint lives on the marker class and must apply to both members. The target and the stage-dependent function are deliberately named to sort BEFORE the members in the tiebreak, so this test fails unless the stage constraints actually do work.
        [Dec.SetupBefore(typeof(StageM_AATargetDec))]
        public class StageM_Marker { }
        public class StageM_AATargetDec : Dec.Dec
        {
            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                RecordSetup("Target.PostLoad");
            }
            #pragma warning restore CS0672
        }
        public class StageM_ABLateDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(StageM_Marker))]
            internal void MLate(Action<string> reporter)
            {
                RecordSetup("Late.MLate");
            }
        }
        public class StageM_ADec : Dec.Dec
        {
            [Dec.Setup(Stage = typeof(StageM_Marker))]
            internal void MA(Action<string> reporter)
            {
                RecordSetup("A.MA");
            }
        }
        public class StageM_BDec : Dec.Dec
        {
            [Dec.Setup(Stage = typeof(StageM_Marker))]
            internal void MB(Action<string> reporter)
            {
                RecordSetup("B.MB");
            }
        }

        [Test]
        public void TestStageMarker()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StageM_AATargetDec), typeof(StageM_ADec), typeof(StageM_BDec), typeof(StageM_ABLateDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StageM_AATargetDec decName=""T"" />
                    <StageM_ADec decName=""A"" />
                    <StageM_BDec decName=""B"" />
                    <StageM_ABLateDec decName=""L"" />
                </Decs>");
            parser.Finish();

            int idxA = setupOrder.IndexOf("A.MA");
            int idxB = setupOrder.IndexOf("B.MB");
            int idxTarget = setupOrder.IndexOf("Target.PostLoad");
            int idxLate = setupOrder.IndexOf("Late.MLate");
            Assert.GreaterOrEqual(idxA, 0);
            Assert.GreaterOrEqual(idxB, 0);
            Assert.Less(idxA, idxTarget, "stage member A must precede the target");
            Assert.Less(idxB, idxTarget, "stage member B must precede the target");
            Assert.Less(idxA, idxLate, "stage member A must precede the stage-dependent function");
            Assert.Less(idxB, idxLate, "stage member B must precede the stage-dependent function");
        }

        // Classes for TestBaseStageIncludesDerived
        [Dec.Abstract]
        public abstract class BaseInc_Base : Dec.Dec { }
        public class BaseInc_DDec : BaseInc_Base
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("D.M");
            }
        }
        public class BaseInc_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(BaseInc_Base))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("User.M");
            }
        }

        [Test]
        public void TestBaseStageIncludesDerived()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BaseInc_DDec), typeof(BaseInc_UserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BaseInc_DDec decName=""D"" />
                    <BaseInc_UserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "D.M", "User.M" }, setupOrder);
        }

        // Classes for TestSiblingOrderWithSharedBaseMethod; regression test for false cycles when a shared base declares the setup function
        [Dec.Abstract]
        public abstract class SharedB_Base : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"{GetType().Name}.M");
            }
        }
        [Dec.SetupAfter(typeof(SharedB_D2Dec))]
        public class SharedB_D1Dec : SharedB_Base { }
        public class SharedB_D2Dec : SharedB_Base { }

        [Test]
        public void TestSiblingOrderWithSharedBaseMethod()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SharedB_D1Dec), typeof(SharedB_D2Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SharedB_D1Dec decName=""One"" />
                    <SharedB_D2Dec decName=""Two"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "SharedB_D2Dec.M", "SharedB_D1Dec.M" }, setupOrder);
        }

        // Classes for TestVirtualOverride
        [Dec.Abstract]
        public abstract class VirtOver_Base : Dec.Dec
        {
            [Dec.Setup]
            internal virtual void M(Action<string> reporter)
            {
                RecordSetup("base");
            }
        }
        public class VirtOver_DDec : VirtOver_Base
        {
            internal override void M(Action<string> reporter)
            {
                RecordSetup("derived");
            }
        }

        [Test]
        public void TestVirtualOverride()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(VirtOver_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <VirtOver_DDec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "derived" }, setupOrder);
        }

        // Classes for TestRetaggedOverrideWarns
        [Dec.Abstract]
        public abstract class ReTag_Base : Dec.Dec
        {
            [Dec.Setup]
            internal virtual void M(Action<string> reporter)
            {
                RecordSetup("base");
            }
        }
        public class ReTag_DDec : ReTag_Base
        {
            [Dec.Setup]
            internal override void M(Action<string> reporter)
            {
                RecordSetup("derived");
            }
        }

        [Test]
        public void TestRetaggedOverrideWarns()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReTag_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReTag_DDec decName=""A"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("already a setup function"));

            CollectionAssert.AreEqual(new[] { "derived" }, setupOrder);
        }

        // Classes for TestReporter
        public class Reporter_Dec : Dec.Dec
        {
            public bool touchedBefore = false;
            public bool touchedAfter = false;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                touchedBefore = true;
                reporter("intentional setup gripe");
                touchedAfter = true;
            }
        }

        [Test]
        public void TestReporter()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Reporter_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Reporter_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("intentional setup gripe") && err.Contains("Reporter_Dec"));

            var dec = Dec.Database<Reporter_Dec>.Get("A");
            Assert.IsTrue(dec.touchedBefore);
            Assert.IsTrue(dec.touchedAfter);
        }

        // Classes for TestExceptionIsolation
        public class Except_Dec : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                if (DecName == "B")
                {
                    throw new InvalidOperationException("intentional setup explosion");
                }

                RecordSetup(DecName);
            }

            [Dec.Setup]
            [Dec.SetupAfter(typeof(Except_Dec), "M")]
            internal void MLater(Action<string> reporter)
            {
                RecordSetup($"later:{DecName}");
            }
        }

        [Test]
        public void TestExceptionIsolation()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Except_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Except_Dec decName=""A"" />
                    <Except_Dec decName=""B"" />
                    <Except_Dec decName=""C"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("intentional setup explosion"));

            CollectionAssert.AreEqual(new[] { "A", "C", "later:A", "later:B", "later:C" }, setupOrder);
        }

        // Classes for TestParallel
        public class Par_Dec : Dec.Dec
        {
            [Dec.Setup(Parallel = true)]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"ran:{DecName}");
                reporter($"parerr:{DecName}");
            }
        }

        [Test]
        public void TestParallel()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Par_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Par_Dec decName=""B"" />
                    <Par_Dec decName=""A"" />
                    <Par_Dec decName=""C"" />
                </Decs>");

            // parallel reports arrive from worker threads as they happen, so the thread-static ExpectErrors machinery can't see them; collect with a threadsafe handler instead
            var reported = new List<string>();
            var oldHandler = Dec.Config.ErrorHandler;
            Dec.Config.ErrorHandler = err => { lock (reported) { reported.Add(err); } };
            try
            {
                parser.Finish();
            }
            finally
            {
                Dec.Config.ErrorHandler = oldHandler;
            }

            // execution and report order are both nondeterministic
            CollectionAssert.AreEquivalent(new[] { "ran:A", "ran:B", "ran:C" }, setupOrder);
            Assert.AreEqual(3, reported.Count);
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:A")));
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:B")));
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:C")));
        }

        // Classes for TestParallelThrowingHandler; the default ErrorHandler throws, and that throw must be swallowed after delivery rather than tearing down the Parallel loop and killing sibling instances
        public class ParThrow_Dec : Dec.Dec
        {
            [Dec.Setup(Parallel = true)]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"ran:{DecName}");
                reporter($"parerr:{DecName}");
            }
        }

        [Test]
        public void TestParallelThrowingHandler()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ParThrow_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ParThrow_Dec decName=""A"" />
                    <ParThrow_Dec decName=""B"" />
                    <ParThrow_Dec decName=""C"" />
                </Decs>");

            var reported = new List<string>();
            var oldHandler = Dec.Config.ErrorHandler;
            Dec.Config.ErrorHandler = err => { lock (reported) { reported.Add(err); } throw new ArgumentException(err); };
            try
            {
                // must complete without an escaping exception despite the handler throwing on every report
                parser.Finish();
            }
            finally
            {
                Dec.Config.ErrorHandler = oldHandler;
            }

            CollectionAssert.AreEquivalent(new[] { "ran:A", "ran:B", "ran:C" }, setupOrder);
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:A")));
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:B")));
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:C")));
        }

        // Classes for TestParallelExceptionIsolation
        public class ParEx_Dec : Dec.Dec
        {
            [Dec.Setup(Parallel = true)]
            internal void M(Action<string> reporter)
            {
                if (DecName == "B")
                {
                    throw new InvalidOperationException("intentional parallel explosion");
                }

                RecordSetup($"ran:{DecName}");
            }
        }

        [Test]
        public void TestParallelExceptionIsolation()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ParEx_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ParEx_Dec decName=""A"" />
                    <ParEx_Dec decName=""B"" />
                    <ParEx_Dec decName=""C"" />
                </Decs>");

            var reported = new List<string>();
            var oldHandler = Dec.Config.ErrorHandler;
            Dec.Config.ErrorHandler = err => { lock (reported) { reported.Add(err); } };
            try
            {
                parser.Finish();
            }
            finally
            {
                Dec.Config.ErrorHandler = oldHandler;
            }

            // the exception is reported from the worker but doesn't stop the sibling instances
            CollectionAssert.AreEquivalent(new[] { "ran:A", "ran:C" }, setupOrder);
            Assert.AreEqual(1, reported.Count);
            StringAssert.Contains("intentional parallel explosion", reported[0]);
        }

        // Classes for TestStructDiagnostic
        public struct StructDiag_Instance
        {
            [Dec.Setup]
            internal void M(Action<string> reporter) { }
        }
        public struct StructDiag_Static
        {
            [Dec.Setup]
            internal static void M(Action<string> reporter) { }
        }

        [Test]
        public void TestStructDiagnostic()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitSetupScanTypes = new Type[] { typeof(StructDiag_Instance), typeof(StructDiag_Static) } });

            var errors = new List<string>();
            ExpectErrors(() =>
            {
                var parser = new Dec.Parser();
                parser.Finish();
            }, err => { errors.Add(err); return err.Contains("not supported on structs"); });

            Assert.AreEqual(2, errors.Count);
        }

        // Classes for TestBadSignature
        public class BadSig_Dec : Dec.Dec
        {
            [Dec.Setup]
            internal int M()
            {
                return 0;
            }
        }

        [Test]
        public void TestBadSignature()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BadSig_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BadSig_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("unsupported signature"));
        }

        // Classes for TestNoArgSignatureRejected; the reporter parameter is mandatory, specifically to encourage its use for error reporting
        public class NoArg_Dec : Dec.Dec
        {
            [Dec.Setup]
            internal void M()
            {
                RecordSetup("noarg.M");
            }
        }

        [Test]
        public void TestNoArgSignatureRejected()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NoArg_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NoArg_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("unsupported signature"));

            CollectionAssert.IsEmpty(setupOrder);
        }

        // Classes for TestUnresolvableReference
        public class Unres_ADec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(Unres_BDec), "NoSuchMethod")]
            internal void M(Action<string> reporter)
            {
                RecordSetup("A.M");
            }
        }
        public class Unres_BDec : Dec.Dec { }

        [Test]
        public void TestUnresolvableReference()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Unres_ADec), typeof(Unres_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Unres_ADec decName=""A"" />
                    <Unres_BDec decName=""B"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("no such setup function"));

            // the constraint is dropped but the function still runs
            CollectionAssert.AreEqual(new[] { "A.M" }, setupOrder);
        }

        // Classes for TestAmbiguousReference; `new`-hiding produces two distinct setup functions with the same name
        public class Ambig_Base : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("base.M");
            }
        }
        public class Ambig_DDec : Ambig_Base
        {
            [Dec.Setup]
            internal new void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        public class Ambig_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(Ambig_DDec), "M")]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestAmbiguousReference()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Ambig_DDec), typeof(Ambig_UserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Ambig_DDec decName=""A"" />
                    <Ambig_UserDec decName=""U"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("ambiguous"));

            // both the hidden base function and the hiding function run on the instance
            CollectionAssert.AreEquivalent(new[] { "base.M", "derived.M", "user.MU" }, setupOrder);
        }

        // Classes for TestZeroInstanceDecDep
        public class ZeroInst_TargetDec : Dec.Dec { }
        public class ZeroInst_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ZeroInst_TargetDec))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("user.M");
            }
        }

        [Test]
        public void TestZeroInstanceDecDep()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ZeroInst_TargetDec), typeof(ZeroInst_UserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ZeroInst_UserDec decName=""U"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("no instances"));

            CollectionAssert.AreEqual(new[] { "user.M" }, setupOrder);
        }

        // Classes for TestEmptyStageWarns
        public class EmptyStage_Marker { }
        public class EmptyStage_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(EmptyStage_Marker))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("user.M");
            }
        }

        [Test]
        public void TestEmptyStageWarns()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(EmptyStage_UserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <EmptyStage_UserDec decName=""U"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("no setup functions"));

            CollectionAssert.AreEqual(new[] { "user.M" }, setupOrder);
        }

        // Classes for TestZeroInstanceStageWithMethodsSilent
        public class ZeroPot_Obj
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("obj.M");
            }
        }
        public class ZeroPot_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ZeroPot_Obj))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("user.M");
            }
        }

        [Test]
        public void TestZeroInstanceStageWithMethodsSilent()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ZeroPot_UserDec) }, explicitSetupScanTypes = new Type[] { typeof(ZeroPot_Obj) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ZeroPot_UserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "user.M" }, setupOrder);
        }

        // Classes for TestSelfStageDep
        public class SelfDep_Dec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(SelfDep_Dec))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("self.M");
            }
        }

        [Test]
        public void TestSelfStageDep()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SelfDep_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SelfDep_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("part of"));

            CollectionAssert.AreEqual(new[] { "self.M" }, setupOrder);
        }

        // Classes for TestParallelOnStaticWarns
        public static class ParStatic_Holder
        {
            [Dec.Setup(Parallel = true)]
            internal static void M(Action<string> reporter)
            {
                RecordSetup("static.M");
            }
        }

        [Test]
        public void TestParallelOnStaticWarns()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitSetupScanTypes = new Type[] { typeof(ParStatic_Holder) } });

            var parser = new Dec.Parser();
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("Parallel"));

            CollectionAssert.AreEqual(new[] { "static.M" }, setupOrder);
        }

        // Classes for TestSetupMethodCycle
        public class CycleM_ADec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(CycleM_BDec))]
            internal void MA(Action<string> reporter)
            {
                RecordSetup("A.MA");
            }
        }
        public class CycleM_BDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(CycleM_ADec))]
            internal void MB(Action<string> reporter)
            {
                RecordSetup("B.MB");
            }
        }

        [Test]
        public void TestSetupMethodCycle()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(CycleM_ADec), typeof(CycleM_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <CycleM_ADec decName=""A"" />
                    <CycleM_BDec decName=""B"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("Cycle detected"));

            // both functions still run; the exact recovery order is arbitrary, this pins current behavior rather than a contract
            CollectionAssert.AreEqual(new[] { "B.MB", "A.MA" }, setupOrder);
        }

        // Classes for TestClassLevelDepWithLegacyHooks; the class-level dependency must constrain the depending type's own ConfigErrors/PostLoad nodes, which is the migration path for old SetupDependsOn users
        [Dec.SetupAfter(typeof(LegacyMix_BDec))]
        public class LegacyMix_ADec : Dec.Dec
        {
            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                RecordSetup("A.PostLoad");
            }
            #pragma warning restore CS0672
        }
        public class LegacyMix_BDec : Dec.Dec
        {
            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                RecordSetup("B.PostLoad");
            }
            #pragma warning restore CS0672

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("B.M");
            }
        }

        [Test]
        public void TestClassLevelDepWithLegacyHooks()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LegacyMix_ADec), typeof(LegacyMix_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LegacyMix_ADec decName=""A"" />
                    <LegacyMix_BDec decName=""B"" />
                </Decs>");
            parser.Finish();

            // the dependency waits for B's entire stage, including its [Setup] function, before A's own PostLoad runs
            Assert.AreEqual(3, setupOrder.Count);
            Assert.AreEqual("A.PostLoad", setupOrder[2]);
            CollectionAssert.AreEquivalent(new[] { "B.PostLoad", "B.M" }, setupOrder.GetRange(0, 2));
        }

        // Classes for TestRecorderReadTriggers
        public class RecMode_Obj : Dec.IRecordable
        {
            public int value;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("recorded.M");
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }
        }

        [Test]
        public void TestRecorderReadTriggers([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var obj = new RecMode_Obj { value = 42 };

            // writing alone must not trigger setup
            Dec.Recorder.Write(obj);
            CollectionAssert.IsEmpty(setupOrder);

            var deserialized = DoRecorderRoundTrip(obj, mode);

            Assert.AreEqual(42, deserialized.value);

            // every mode that performs an actual Read/ReadSimple runs setup on the objects it created; Clone and Checksum don't read
            bool reads = mode != RecorderMode.Clone && mode != RecorderMode.Checksum;
            Assert.AreEqual(reads ? 1 : 0, setupOrder.Count);
        }

        // Classes for TestReparseRuns
        public class Reparse_Dec : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup(DecName);
            }
        }

        [Test]
        public void TestReparseRuns([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Reparse_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Reparse_Dec decName=""A"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            // only the Rewritten modes clear the database and reparse, which is what re-runs setup
            bool reparses = mode == ParserMode.RewrittenPretty || mode == ParserMode.RewrittenBare;
            Assert.AreEqual(reparses ? 2 : 1, setupOrder.Count);
        }

        // Classes for TestClassLevelAncestorDepErrors; a class-level dependency on your own ancestor is part of your own stage and must be rejected rather than silently cycling
        public class LegHier_BBaseDec : Dec.Dec
        {
            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                RecordSetup($"{GetType().Name}:{DecName}");
            }
            #pragma warning restore CS0672
        }
        [Dec.SetupAfter(typeof(LegHier_BBaseDec))]
        public class LegHier_ADerivedDec : LegHier_BBaseDec { }

        [Test]
        public void TestClassLevelAncestorDepErrors()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LegHier_BBaseDec), typeof(LegHier_ADerivedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LegHier_BBaseDec decName=""B1"" />
                    <LegHier_ADerivedDec decName=""D1"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("part of"));

            // the dependency is dropped; both hooks still run, in plain tiebreak order (derived sorts first)
            CollectionAssert.AreEqual(new[] { "LegHier_ADerivedDec:D1", "LegHier_BBaseDec:B1" }, setupOrder);
        }

        // Classes for TestSetupOnLegacyOverride
        public class TagLegacy_Dec : Dec.Dec
        {
            [Dec.Setup]
            #pragma warning disable CS0672
            public override void ConfigErrors(Action<string> reporter)
            {
                RecordSetup("cfg");
            }
            #pragma warning restore CS0672
        }

        [Test]
        public void TestSetupOnLegacyOverride()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TagLegacy_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TagLegacy_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("already run automatically"));

            // must run exactly once, through the built-in pass
            CollectionAssert.AreEqual(new[] { "cfg" }, setupOrder);
        }

        // Classes for TestEmptyStageTransitivity; the marker has no members at all, but Before/After constraints routed through it must still order the endpoints. Adversarial tiebreak: A would run first without the constraints.
        public class EmptyTrans_Marker { }
        public class EmptyTrans_ADec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(EmptyTrans_Marker))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("A.M");
            }
        }
        public class EmptyTrans_ZDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupBefore(typeof(EmptyTrans_Marker))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("Z.M");
            }
        }

        [Test]
        public void TestEmptyStageTransitivity()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(EmptyTrans_ADec), typeof(EmptyTrans_ZDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <EmptyTrans_ADec decName=""A"" />
                    <EmptyTrans_ZDec decName=""Z"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("no setup functions"));

            CollectionAssert.AreEqual(new[] { "Z.M", "A.M" }, setupOrder);
        }

        // Classes for TestTransitiveSynthesis; Chain_Mid has no instances but is referenced, and its own dependency on Chain_Far must also be discovered without a spurious empty-stage warning.
        public class Chain_Far
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("far.M");
            }
        }
        public class Chain_Mid
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(Chain_Far))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("mid.M");
            }
        }
        public class Chain_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(Chain_Mid))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("user.M");
            }
        }

        [Test]
        public void TestTransitiveSynthesis()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Chain_UserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Chain_UserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "user.M" }, setupOrder);
        }

        // Classes for TestStaticOverloads; only one signature is valid, so an overload pair degrades to one valid function plus a signature error on the other
        public static class Overload_Holder
        {
            [Dec.Setup]
            internal static void M()
            {
                RecordSetup("m");
            }

            [Dec.Setup]
            internal static void M(Action<string> reporter)
            {
                RecordSetup("m");
            }
        }

        [Test]
        public void TestStaticOverloads()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitSetupScanTypes = new Type[] { typeof(Overload_Holder) } });

            ExpectErrors(() =>
            {
                var parser = new Dec.Parser();
                parser.Finish();
            }, err => err.Contains("unsupported signature"));

            Assert.AreEqual(1, setupOrder.Count);
        }

        // Classes for TestInstanceOverloads
        public class OverloadInst_Dec : Dec.Dec
        {
            [Dec.Setup]
            internal void M()
            {
                RecordSetup("m");
            }

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("m");
            }
        }

        [Test]
        public void TestInstanceOverloads()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(OverloadInst_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <OverloadInst_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("unsupported signature"));

            Assert.AreEqual(1, setupOrder.Count);
        }

        // Classes for TestReadSimpleTriggers
        public class ReadSimple_Obj : Dec.IRecordable
        {
            public int value;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("simple.M");
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }
        }

        [Test]
        public void TestReadSimpleTriggers()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.WriteSimple(new ReadSimple_Obj { value = 7 }, "root");
            CollectionAssert.IsEmpty(setupOrder);

            var deserialized = Dec.Recorder.ReadSimple<ReadSimple_Obj>(serialized, "root");

            Assert.AreEqual(7, deserialized.value);
            CollectionAssert.AreEqual(new[] { "simple.M" }, setupOrder);
        }

        // Classes for TestReadRefsDedup
        public class RefDedup_Child : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("child.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class RefDedup_Root : Dec.IRecordable
        {
            public RefDedup_Child one;
            public RefDedup_Child two;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref one, "one");
                recorder.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void TestReadRefsDedup()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var root = new RefDedup_Root();
            root.one = new RefDedup_Child();
            root.two = root.one;

            var serialized = Dec.Recorder.Write(root);
            var deserialized = Dec.Recorder.Read<RefDedup_Root>(serialized);

            Assert.AreSame(deserialized.one, deserialized.two);
            CollectionAssert.AreEqual(new[] { "child.M" }, setupOrder);
        }

        // Classes for TestReadConverterRecordRef; ConverterRecord ref objects are created outside the normal parse flow, so this exercises the refs sweep specifically
        public class ConvRec_Payload
        {
            public int value;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"convrec.M:{value}");
            }
        }
        public class ConvRec_Converter : Dec.ConverterRecord<ConvRec_Payload>
        {
            public override void Record(ref ConvRec_Payload input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
            }
        }
        public class ConvRec_Root : Dec.IRecordable
        {
            public ConvRec_Payload one;
            public ConvRec_Payload two;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref one, "one");
                recorder.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void TestReadConverterRecordRef()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitConverters = new Type[] { typeof(ConvRec_Converter) } });

            var root = new ConvRec_Root();
            root.one = new ConvRec_Payload { value = 5 };
            root.two = root.one;

            var serialized = Dec.Recorder.Write(root);
            var deserialized = Dec.Recorder.Read<ConvRec_Root>(serialized);

            Assert.AreSame(deserialized.one, deserialized.two);
            Assert.AreEqual(5, deserialized.one.value);
            // populated by the time setup runs, and exactly once despite two pointers
            CollectionAssert.AreEqual(new[] { "convrec.M:5" }, setupOrder);
        }

        // Classes for TestReadConverterFactoryRef
        public class ConvFac_Payload
        {
            public int value;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"convfac.M:{value}");
            }
        }
        public class ConvFac_Converter : Dec.ConverterFactory<ConvFac_Payload>
        {
            public override void Write(ConvFac_Payload input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
            }

            public override ConvFac_Payload Create(Dec.Recorder recorder)
            {
                return new ConvFac_Payload();
            }

            public override void Read(ref ConvFac_Payload input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
            }
        }
        public class ConvFac_Root : Dec.IRecordable
        {
            public ConvFac_Payload one;
            public ConvFac_Payload two;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref one, "one");
                recorder.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void TestReadConverterFactoryRef()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitConverters = new Type[] { typeof(ConvFac_Converter) } });

            var root = new ConvFac_Root();
            root.one = new ConvFac_Payload { value = 9 };
            root.two = root.one;

            var serialized = Dec.Recorder.Write(root);
            var deserialized = Dec.Recorder.Read<ConvFac_Root>(serialized);

            Assert.AreSame(deserialized.one, deserialized.two);
            Assert.AreEqual(9, deserialized.one.value);
            CollectionAssert.AreEqual(new[] { "convfac.M:9" }, setupOrder);
        }

        // Classes for TestReadOrdering; adversarial tiebreak, the After edge must invert the ordinal name order
        public class ReadOrd_AObj : Dec.IRecordable
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ReadOrd_ZObj))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("A.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReadOrd_ZObj : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("Z.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReadOrd_Root : Dec.IRecordable
        {
            public ReadOrd_AObj a;
            public ReadOrd_ZObj z;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref a, "a");
                recorder.Record(ref z, "z");
            }
        }

        [Test]
        public void TestReadOrdering()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReadOrd_Root { a = new ReadOrd_AObj(), z = new ReadOrd_ZObj() });
            Dec.Recorder.Read<ReadOrd_Root>(serialized);

            CollectionAssert.AreEqual(new[] { "Z.M", "A.M" }, setupOrder);
        }

        // Classes for TestReadStage; the marker's class-level Before must apply inside a Read, and the target is named to sort before the member
        [Dec.SetupBefore(typeof(ReadStage_AATargetObj))]
        public class ReadStage_Marker { }
        public class ReadStage_AATargetObj : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("target.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReadStage_MemberObj : Dec.IRecordable
        {
            [Dec.Setup(Stage = typeof(ReadStage_Marker))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("member.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReadStage_Root : Dec.IRecordable
        {
            public ReadStage_AATargetObj target;
            public ReadStage_MemberObj member;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref target, "target");
                recorder.Record(ref member, "member");
            }
        }

        [Test]
        public void TestReadStage()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReadStage_Root { target = new ReadStage_AATargetObj(), member = new ReadStage_MemberObj() });
            Dec.Recorder.Read<ReadStage_Root>(serialized);

            CollectionAssert.AreEqual(new[] { "member.M", "target.M" }, setupOrder);
        }

        // Classes for TestReadAbsentTypeDepSatisfied
        public class ReadAbsent_Target
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("absent.M");
            }
        }
        public class ReadAbsent_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ReadAbsent_Target))]
            [Dec.SetupAfter(typeof(ReadAbsent_Target), "M")]
            internal void M(Action<string> reporter)
            {
                RecordSetup("present.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }

        [Test]
        public void TestReadAbsentTypeDepSatisfied()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReadAbsent_Obj());
            Dec.Recorder.Read<ReadAbsent_Obj>(serialized);

            // no errors, no warnings, and the function ran despite both dependencies referencing a type absent from this read
            CollectionAssert.AreEqual(new[] { "present.M" }, setupOrder);
        }

        // Classes for TestReadDecTypeDepSatisfied
        public class ReadDecDep_Dec : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"dec.M:{DecName}");
            }
        }
        public class ReadDecDep_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ReadDecDep_Dec))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("obj.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }

        [Test]
        public void TestReadDecTypeDepSatisfied()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReadDecDep_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReadDecDep_Dec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "dec.M:A" }, setupOrder);

            var serialized = Dec.Recorder.Write(new ReadDecDep_Obj());
            Dec.Recorder.Read<ReadDecDep_Obj>(serialized);

            // the dec-targeted dependency is silently satisfied; dec setup does not re-run
            CollectionAssert.AreEqual(new[] { "dec.M:A", "obj.M" }, setupOrder);
        }

        // Classes for TestReadParallel
        public class ReadPar_Item : Dec.IRecordable
        {
            public int id;

            [Dec.Setup(Parallel = true)]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"ran:{id}");
                reporter($"parerr:{id}");
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref id, "id");
            }
        }
        public class ReadPar_Root : Dec.IRecordable
        {
            public List<ReadPar_Item> items;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref items, "items");
            }
        }

        [Test]
        public void TestReadParallel()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var root = new ReadPar_Root { items = new List<ReadPar_Item> { new ReadPar_Item { id = 1 }, new ReadPar_Item { id = 2 }, new ReadPar_Item { id = 3 } } };
            var serialized = Dec.Recorder.Write(root);

            var reported = new List<string>();
            var oldHandler = Dec.Config.ErrorHandler;
            Dec.Config.ErrorHandler = err => { lock (reported) { reported.Add(err); } };
            try
            {
                Dec.Recorder.Read<ReadPar_Root>(serialized);
            }
            finally
            {
                Dec.Config.ErrorHandler = oldHandler;
            }

            // all instances ran; reports arrive from workers in no particular order
            CollectionAssert.AreEquivalent(new[] { "ran:1", "ran:2", "ran:3" }, setupOrder);
            Assert.AreEqual(3, reported.Count);
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:1")));
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:2")));
            Assert.IsTrue(reported.Any(r => r.Contains("parerr:3")));
        }

        // Classes for TestReadDecOwnedNotReRun
        public class DecOwned_Comp : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("comp.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class DecOwned_Dec : Dec.Dec
        {
            public DecOwned_Comp comp;

            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                Dec.Database.DecLookupEnable(comp);
            }
            #pragma warning restore CS0672
        }
        public class DecOwned_Holder : Dec.IRecordable
        {
            public DecOwned_Comp comp;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref comp, "comp");
            }
        }

        [Test]
        public void TestReadDecOwnedNotReRun()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DecOwned_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DecOwned_Dec decName=""A"">
                        <comp />
                    </DecOwned_Dec>
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "comp.M" }, setupOrder);

            var dec = Dec.Database<DecOwned_Dec>.Get("A");
            var serialized = Dec.Recorder.Write(new DecOwned_Holder { comp = dec.comp });
            var deserialized = Dec.Recorder.Read<DecOwned_Holder>(serialized);

            // resolved back to the database-owned instance, and setup does not re-run on it
            Assert.AreSame(dec.comp, deserialized.comp);
            CollectionAssert.AreEqual(new[] { "comp.M" }, setupOrder);
        }

        // Classes for TestReadStaticsNotRun
        public static class ReadStatic_Holder
        {
            [Dec.Setup]
            internal static void Init(Action<string> reporter)
            {
                RecordSetup("static.Init");
            }
        }
        public class ReadStatic_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("obj.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }

        [Test]
        public void TestReadStaticsNotRun()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitSetupScanTypes = new Type[] { typeof(ReadStatic_Holder) } });

            var parser = new Dec.Parser();
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "static.Init" }, setupOrder);

            var serialized = Dec.Recorder.Write(new ReadStatic_Obj());
            Dec.Recorder.Read<ReadStatic_Obj>(serialized);

            // instance setup ran, static did not re-run
            CollectionAssert.AreEqual(new[] { "static.Init", "obj.M" }, setupOrder);
        }

        // Classes for TestReadFailurePathSkipsSetup
        public class FailPath_Obj
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("failpath.M");
            }
        }

        [Test]
        public void TestReadFailurePathSkipsSetup()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            // valid refs section, missing <data>: the ref object gets created, but the failed root parse must skip setup entirely
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""DecTest.ParserSetup.FailPath_Obj"" />
                  </refs>
                </Record>";
            ExpectErrors(() => Dec.Recorder.Read<FailPath_Obj>(serialized), err => true);

            CollectionAssert.IsEmpty(setupOrder);
        }

        [Test]
        public void TestReadSimpleFailurePathSkipsSetup()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            ReadSimple_Obj deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.ReadSimple<ReadSimple_Obj>("<<<not xml", "root"), err => true);

            Assert.IsNull(deserialized);
            CollectionAssert.IsEmpty(setupOrder);
        }

        // Classes for TestReadStaticNamedDepSatisfied; a named dependency on a static setup function must be silently satisfied during a Read, not diagnosed as a typo
        public static class StaticDep_Holder
        {
            [Dec.Setup]
            internal static void Init(Action<string> reporter)
            {
                RecordSetup("static.Init");
            }
        }
        public class StaticDep_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(StaticDep_Holder), "Init")]
            internal void M(Action<string> reporter)
            {
                RecordSetup("obj.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }

        [Test]
        public void TestReadStaticNamedDepSatisfied()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitSetupScanTypes = new Type[] { typeof(StaticDep_Holder) } });

            var parser = new Dec.Parser();
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "static.Init" }, setupOrder);

            var serialized = Dec.Recorder.Write(new StaticDep_Obj());
            Dec.Recorder.Read<StaticDep_Obj>(serialized);

            // no errors: the static ran at parse time and the read-time constraint is simply satisfied
            CollectionAssert.AreEqual(new[] { "static.Init", "obj.M" }, setupOrder);
        }

        // Classes for TestReadValueEqualityNotExcluded; a fresh savegame object that value-Equals a parser-created one must still get its setup
        public class ValEq_Obj : Dec.IRecordable
        {
            public int value;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"valeq.M:{value}");
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }

            public override bool Equals(object obj)
            {
                return obj is ValEq_Obj other && other.value == value;
            }

            public override int GetHashCode()
            {
                return value;
            }
        }
        public class ValEq_Dec : Dec.Dec
        {
            public ValEq_Obj obj;
        }

        [Test]
        public void TestReadValueEqualityNotExcluded()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ValEq_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ValEq_Dec decName=""A"">
                        <obj><value>7</value></obj>
                    </ValEq_Dec>
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "valeq.M:7" }, setupOrder);

            // a brand-new object that happens to Equals the parser-created one; database ownership is reference identity, so setup must run
            var serialized = Dec.Recorder.Write(new ValEq_Obj { value = 7 });
            var deserialized = Dec.Recorder.Read<ValEq_Obj>(serialized);

            Assert.AreNotSame(Dec.Database<ValEq_Dec>.Get("A").obj, deserialized);
            CollectionAssert.AreEqual(new[] { "valeq.M:7", "valeq.M:7" }, setupOrder);
        }
    }
}
