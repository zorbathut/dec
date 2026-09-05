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
            ReportNDShare_Payload.Singleton = new ReportNDShare_Payload();
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

        // Classes for TestBareTypeDepExcludesDerivedIntroduced and TestIncludeDerived; a bare-type dependency on the base must not wait on the derived-introduced function, and IncludeDerived must restore exactly that edge. The base is deliberately named to sort after the user so the traversal reaches the user's function before the stage sentinels can pull the derived function in.
        [Dec.Abstract]
        public abstract class DeclOnly_ZBase : Dec.Dec { }
        public class DeclOnly_ZZDec : DeclOnly_ZBase
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        public class DeclOnly_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(DeclOnly_ZBase))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("user.M");
            }
        }

        [Test]
        public void TestBareTypeDepExcludesDerivedIntroduced()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DeclOnly_ZZDec), typeof(DeclOnly_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DeclOnly_ZZDec decName=""D"" />
                    <DeclOnly_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "user.M", "derived.M" }, setupOrder);
        }

        // Classes for TestIncludeDerived; same shape as above, opt-in flips the order
        [Dec.Abstract]
        public abstract class IncDer_ZBase : Dec.Dec { }
        public class IncDer_ZZDec : IncDer_ZBase
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        public class IncDer_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IncDer_ZBase), IncludeDerived = true)]
            internal void M(Action<string> reporter)
            {
                RecordSetup("user.M");
            }
        }

        [Test]
        public void TestIncludeDerived()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IncDer_ZZDec), typeof(IncDer_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IncDer_ZZDec decName=""D"" />
                    <IncDer_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "derived.M", "user.M" }, setupOrder);
        }

        // Classes for TestMethodLevelAncestorDep; a derived-introduced function may depend on its own ancestor's setup, which reaches the inherited function on every instance, siblings included. G's tiebreak would run it first without the edges.
        [Dec.Abstract]
        public abstract class MethAnc_Base : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"{GetType().Name}.M");
            }
        }
        public class MethAnc_ADec : MethAnc_Base
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(MethAnc_Base))]
            internal void G(Action<string> reporter)
            {
                RecordSetup("A.G");
            }
        }
        public class MethAnc_BDec : MethAnc_Base { }

        [Test]
        public void TestMethodLevelAncestorDep()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MethAnc_ADec), typeof(MethAnc_BDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MethAnc_ADec decName=""A1"" />
                    <MethAnc_BDec decName=""B1"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "MethAnc_ADec.M", "MethAnc_BDec.M", "A.G" }, setupOrder);
        }

        // Classes for TestIncludeDerivedAncestorSelfErrors; the opt-in puts the derived-introduced function back inside the ancestor's stage, so the dependency must be rejected again
        [Dec.Abstract]
        public abstract class IncSelf_Base : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("base.M");
            }
        }
        public class IncSelf_DDec : IncSelf_Base
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IncSelf_Base), IncludeDerived = true)]
            internal void G(Action<string> reporter)
            {
                RecordSetup("derived.G");
            }
        }

        [Test]
        public void TestIncludeDerivedAncestorSelfErrors()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IncSelf_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IncSelf_DDec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("part of"));

            // the dependency is dropped; tiebreak runs G first
            CollectionAssert.AreEqual(new[] { "derived.G", "base.M" }, setupOrder);
        }

        // Classes for TestClassLevelIncludeDerived; the class-level opt-in must wait on the derived-introduced function, which the user's tiebreak would otherwise precede
        [Dec.Abstract]
        public abstract class CLInc_ZBase : Dec.Dec { }
        public class CLInc_ZZDec : CLInc_ZBase
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        [Dec.SetupAfter(typeof(CLInc_ZBase), IncludeDerived = true)]
        public class CLInc_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("user.M");
            }
        }

        [Test]
        public void TestClassLevelIncludeDerived()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(CLInc_ZZDec), typeof(CLInc_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <CLInc_ZZDec decName=""D"" />
                    <CLInc_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "derived.M", "user.M" }, setupOrder);
        }

        // Classes for TestNamedTargetHiddenByNew; a base-targeted name must resolve to the base's function even when a derived class new-hides it with a distinct function, rather than reporting ambiguity
        [Dec.Abstract]
        public abstract class NamedHide_ZBase : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("base.M");
            }
        }
        public class NamedHide_ZZDec : NamedHide_ZBase
        {
            [Dec.Setup]
            internal new void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        public class NamedHide_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(NamedHide_ZBase), "M")]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestNamedTargetHiddenByNew()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedHide_ZZDec), typeof(NamedHide_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedHide_ZZDec decName=""D"" />
                    <NamedHide_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEquivalent(new[] { "base.M", "derived.M", "user.MU" }, setupOrder);
            Assert.Less(setupOrder.IndexOf("base.M"), setupOrder.IndexOf("user.MU"));
        }

        // Classes for TestNamedTargetDerivedOnlyErrors; a base-targeted name for a function that exists only on a derived class is a declaration bug, not a resolution
        [Dec.Abstract]
        public abstract class NamedNeg_ZBase : Dec.Dec { }
        public class NamedNeg_ZZDec : NamedNeg_ZBase
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        public class NamedNeg_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(NamedNeg_ZBase), "M")]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestNamedTargetDerivedOnlyErrors()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(NamedNeg_ZZDec), typeof(NamedNeg_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NamedNeg_ZZDec decName=""D"" />
                    <NamedNeg_AUserDec decName=""U"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("no such setup function"));

            CollectionAssert.AreEquivalent(new[] { "derived.M", "user.MU" }, setupOrder);
        }

        // Classes for TestIncludeDerivedWithMemberNameErrors
        public class IncName_ZTargetDec : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("target.M");
            }
        }
        public class IncName_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IncName_ZTargetDec), "M", IncludeDerived = true)]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestIncludeDerivedWithMemberNameErrors()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IncName_ZTargetDec), typeof(IncName_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IncName_ZTargetDec decName=""T"" />
                    <IncName_AUserDec decName=""U"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("IncludeDerived"));

            // the constraint is dropped; tiebreak runs the user function first
            CollectionAssert.AreEqual(new[] { "user.MU", "target.M" }, setupOrder);
        }

        // Classes for TestInterfaceContract; a [Dec.Setup] tag on the interface member makes every implementation a setup function with no attribute on the implementation itself
        public interface IContract_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class Contract_Dec : Dec.Dec, IContract_Iface
        {
            public void M(Action<string> reporter)
            {
                RecordSetup($"impl.M:{DecName}");
            }
        }

        [Test]
        public void TestInterfaceContract()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Contract_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Contract_Dec decName=""A"" />
                    <Contract_Dec decName=""B"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "impl.M:A", "impl.M:B" }, setupOrder);
        }

        // Classes for TestInterfaceContractExplicitImpl
        public interface IExpl_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class Expl_Dec : Dec.Dec, IExpl_Iface
        {
            void IExpl_Iface.M(Action<string> reporter)
            {
                RecordSetup("explicit.M");
            }
        }

        [Test]
        public void TestInterfaceContractExplicitImpl()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Expl_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Expl_Dec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "explicit.M" }, setupOrder);
        }

        // Classes for TestInterfaceContractDefaultImpl; the tagged member's default body runs when the implementor doesn't provide one
        public interface IDim_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter)
            {
                RecordSetup("dim.M");
            }
        }
        public class Dim_Dec : Dec.Dec, IDim_Iface { }

        [Test]
        public void TestInterfaceContractDefaultImpl()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Dim_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Dim_Dec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "dim.M" }, setupOrder);
        }

        // Classes for TestInterfaceContractVirtualOverride; the contract function dispatches to the most-derived body
        public interface IVirtC_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        [Dec.Abstract]
        public abstract class VirtC_Base : Dec.Dec, IVirtC_Iface
        {
            public virtual void M(Action<string> reporter)
            {
                RecordSetup("base");
            }
        }
        public class VirtC_DDec : VirtC_Base
        {
            public override void M(Action<string> reporter)
            {
                RecordSetup("derived");
            }
        }

        [Test]
        public void TestInterfaceContractVirtualOverride()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(VirtC_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <VirtC_DDec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "derived" }, setupOrder);
        }

        // Classes for TestInterfaceBareDepIsContractOnly; the bare dependency waits on the contract but not on the implementor's extra function or its ConfigErrors/PostLoad, all of which the user's tiebreak precedes
        public interface IBareC_Iface
        {
            [Dec.Setup]
            void CM(Action<string> reporter);
        }
        public class BareC_ZDec : Dec.Dec, IBareC_Iface
        {
            public void CM(Action<string> reporter)
            {
                RecordSetup("contract.CM");
            }

            [Dec.Setup]
            internal void Extra(Action<string> reporter)
            {
                RecordSetup("impl.Extra");
            }

            #pragma warning disable CS0672
            public override void PostLoad(Action<string> reporter)
            {
                RecordSetup("impl.PostLoad");
            }
            #pragma warning restore CS0672
        }
        public class BareC_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IBareC_Iface))]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestInterfaceBareDepIsContractOnly()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BareC_ZDec), typeof(BareC_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BareC_ZDec decName=""Z"" />
                    <BareC_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "contract.CM", "user.MU", "impl.Extra", "impl.PostLoad" }, setupOrder);
        }

        // Classes for TestInterfaceIncludeDerived; the opt-in additionally waits on the implementor's extra function
        public interface IIncDIf_Iface
        {
            [Dec.Setup]
            void CM(Action<string> reporter);
        }
        public class IncDIf_ZDec : Dec.Dec, IIncDIf_Iface
        {
            public void CM(Action<string> reporter)
            {
                RecordSetup("contract.CM");
            }

            [Dec.Setup]
            internal void Extra(Action<string> reporter)
            {
                RecordSetup("impl.Extra");
            }
        }
        public class IncDIf_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IIncDIf_Iface), IncludeDerived = true)]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestInterfaceIncludeDerived()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IncDIf_ZDec), typeof(IncDIf_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IncDIf_ZDec decName=""Z"" />
                    <IncDIf_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "contract.CM", "impl.Extra", "user.MU" }, setupOrder);
        }

        // Classes for TestMethodLevelAncestorDepBefore; the Before mirror of the ancestor dependency, which must invert Q's natural tiebreak position after M
        [Dec.Abstract]
        public abstract class MethAncB_ZBase : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup($"{GetType().Name}.M");
            }
        }
        public class MethAncB_ZZDec : MethAncB_ZBase
        {
            [Dec.Setup]
            [Dec.SetupBefore(typeof(MethAncB_ZBase))]
            internal void Q(Action<string> reporter)
            {
                RecordSetup("derived.Q");
            }
        }

        [Test]
        public void TestMethodLevelAncestorDepBefore()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MethAncB_ZZDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MethAncB_ZZDec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "derived.Q", "MethAncB_ZZDec.M" }, setupOrder);
        }

        // Classes for TestStaticDeclaredOnMembership; a static setup function introduced on a derived class is outside the base's own setup but inside its IncludeDerived stage
        public class StatDecl_ZBase { }
        public class StatDecl_ZZHolder : StatDecl_ZBase
        {
            [Dec.Setup]
            internal static void Init(Action<string> reporter)
            {
                RecordSetup("static.Init");
            }
        }
        public class StatDecl_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(StatDecl_ZBase), IncludeDerived = true)]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestStaticDeclaredOnMembership()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StatDecl_AUserDec) }, explicitSetupScanTypes = new Type[] { typeof(StatDecl_ZZHolder) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StatDecl_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "static.Init", "user.MU" }, setupOrder);
        }

        // Classes for TestInterfaceNamedDep; a member-named interface reference resolves to that contract function, and not to the implementor's other functions
        public interface IIfaceNamed
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class IfaceNamed_ZDec : Dec.Dec, IIfaceNamed
        {
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }

            [Dec.Setup]
            internal void Extra(Action<string> reporter)
            {
                RecordSetup("impl.Extra");
            }
        }
        public class IfaceNamed_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IIfaceNamed), "M")]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestInterfaceNamedDep()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IfaceNamed_ZDec), typeof(IfaceNamed_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IfaceNamed_ZDec decName=""Z"" />
                    <IfaceNamed_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "impl.M", "user.MU", "impl.Extra" }, setupOrder);
        }

        // Classes for TestInterfaceImplRetagWarns; the implementation is already a setup function through the contract, so its own tag is redundant and its settings are ignored
        public interface IRetagI_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class RetagI_Dec : Dec.Dec, IRetagI_Iface
        {
            [Dec.Setup]
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }

        [Test]
        public void TestInterfaceImplRetagWarns()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RetagI_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RetagI_Dec decName=""A"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("already a setup function"));

            CollectionAssert.AreEqual(new[] { "impl.M" }, setupOrder);
        }

        // Classes for TestInterfaceRetaggedOverrideWarns; the tagged override of a retagged implementation must not sneak in a second class-identity function
        public interface IReOver_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        [Dec.Abstract]
        public abstract class ReOver_Base : Dec.Dec, IReOver_Iface
        {
            [Dec.Setup]
            public virtual void M(Action<string> reporter)
            {
                RecordSetup("base");
            }
        }
        public class ReOver_DDec : ReOver_Base
        {
            [Dec.Setup]
            public override void M(Action<string> reporter)
            {
                RecordSetup("derived");
            }
        }

        [Test]
        public void TestInterfaceRetaggedOverrideWarns()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReOver_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReOver_DDec decName=""A"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("already a setup function"));

            CollectionAssert.AreEqual(new[] { "derived" }, setupOrder);
        }

        // Classes for TestInterfaceTaggedOverrideOfUntaggedImplWarns; the base's implementation carries no tag, so only the derived override's tag is at fault
        public interface ITagOver_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        [Dec.Abstract]
        public abstract class TagOver_Base : Dec.Dec, ITagOver_Iface
        {
            public virtual void M(Action<string> reporter)
            {
                RecordSetup("base");
            }
        }
        public class TagOver_DDec : TagOver_Base
        {
            [Dec.Setup]
            public override void M(Action<string> reporter)
            {
                RecordSetup("derived");
            }
        }

        [Test]
        public void TestInterfaceTaggedOverrideOfUntaggedImplWarns()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TagOver_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TagOver_DDec decName=""A"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("already a setup function"));

            CollectionAssert.AreEqual(new[] { "derived" }, setupOrder);
        }

        // Classes for TestInterfaceCrossLevelOverlapWarns; the class-tagged function pre-dates the interface, so the class identity wins: one warning at the level introducing the interface, no re-warn below, no double run - and bare deps on the interface don't reach the function while IncludeDerived ones do
        public interface ICross_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        [Dec.Abstract]
        public abstract class Cross_ZBase : Dec.Dec
        {
            [Dec.Setup]
            public virtual void M(Action<string> reporter)
            {
                RecordSetup($"{GetType().Name}.M");
            }
        }
        public class Cross_ZZDec : Cross_ZBase, ICross_Iface { }
        public class Cross_ZZZDec : Cross_ZZDec { }
        public class Cross_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ICross_Iface))]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }
        public class Cross_BUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ICross_Iface), IncludeDerived = true)]
            internal void MW(Action<string> reporter)
            {
                RecordSetup("user.MW");
            }
        }

        [Test]
        public void TestInterfaceCrossLevelOverlapWarns()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Cross_ZZDec), typeof(Cross_ZZZDec), typeof(Cross_AUserDec), typeof(Cross_BUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Cross_ZZDec decName=""Z1"" />
                    <Cross_ZZZDec decName=""Z2"" />
                    <Cross_AUserDec decName=""U1"" />
                    <Cross_BUserDec decName=""U2"" />
                </Decs>");
            int warningCount = 0;
            ExpectWarnings(() => parser.Finish(), wrn => { ++warningCount; return wrn.Contains("already a setup function"); });

            // exactly one warning: the conflict is reported at the level introducing the interface, not re-reported by descendants
            Assert.AreEqual(1, warningCount);
            CollectionAssert.AreEqual(new[] { "user.MU", "Cross_ZZDec.M", "Cross_ZZZDec.M", "user.MW" }, setupOrder);
        }

        // Classes for TestInterfaceGenericContract
        public interface IGen_Iface<T>
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class Gen_ZDec : Dec.Dec, IGen_Iface<int>
        {
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }
        public class Gen_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IGen_Iface<int>))]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestInterfaceGenericContract()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Gen_ZDec), typeof(Gen_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Gen_ZDec decName=""Z"" />
                    <Gen_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "impl.M", "user.MU" }, setupOrder);
        }

        // Classes for TestInterfaceGenericDoubleConstruction; one body satisfying two constructions of a tagged generic interface is two contract functions and runs once per contract
        public interface IGenD_Iface<T>
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class GenD_Dec : Dec.Dec, IGenD_Iface<int>, IGenD_Iface<string>
        {
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }

        [Test]
        public void TestInterfaceGenericDoubleConstruction()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(GenD_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <GenD_Dec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "impl.M", "impl.M" }, setupOrder);
        }

        // Classes for TestInterfaceOwnExtraAfterOwnContract; the implementor's extra function may order after its own interface's contract, since it isn't part of it. The class is named to sort before the interface so the traversal reaches the extra function before the sentinels can pull anything.
        public interface IOwnEx_Iface
        {
            [Dec.Setup]
            void CM(Action<string> reporter);
        }
        public class AOwnEx_Dec : Dec.Dec, IOwnEx_Iface
        {
            public void CM(Action<string> reporter)
            {
                RecordSetup("contract.CM");
            }

            [Dec.Setup]
            [Dec.SetupAfter(typeof(IOwnEx_Iface))]
            internal void AExtra(Action<string> reporter)
            {
                RecordSetup("impl.AExtra");
            }
        }

        [Test]
        public void TestInterfaceOwnExtraAfterOwnContract()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(AOwnEx_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <AOwnEx_Dec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "contract.CM", "impl.AExtra" }, setupOrder);
        }

        // Classes for TestInterfaceImplOrderingAttrsError; ordering attributes on an implementation are dead, the constraint belongs on the interface member
        public interface IImplOrd_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class ImplOrd_Dec : Dec.Dec, IImplOrd_Iface
        {
            [Dec.SetupAfter(typeof(IImplOrd_Iface))]
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }

        [Test]
        public void TestInterfaceImplOrderingAttrsError()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ImplOrd_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ImplOrd_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("declare them on the interface member"));

            CollectionAssert.AreEqual(new[] { "impl.M" }, setupOrder);
        }

        // Classes for TestClassLevelAttrOnMarkerInterface; class-level ordering attributes on an interface must constrain implementors' functions with nothing else referencing the interface - attribute inheritance can't reach them, so both the discovery and the hierarchy own-side binding have to come from the implementors. The target is named to sort before the implementor so the Before edge is what inverts the order.
        [Dec.SetupBefore(typeof(MarkCL_ATargetDec))]
        public interface IMarkCL_Iface { }
        public class MarkCL_ATargetDec : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("target.M");
            }
        }
        public class MarkCL_ZImplDec : Dec.Dec, IMarkCL_Iface
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }

        [Test]
        public void TestClassLevelAttrOnMarkerInterface()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MarkCL_ATargetDec), typeof(MarkCL_ZImplDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MarkCL_ATargetDec decName=""T"" />
                    <MarkCL_ZImplDec decName=""Z"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "impl.M", "target.M" }, setupOrder);
        }

        // Classes for TestClassLevelMarkerInterfaceSelfOverlapErrors; the marker's implementor sits inside the target's IncludeDerived stage, which must be caught as a self-in-stage error rather than surfacing as a raw cycle
        [Dec.SetupBefore(typeof(MarkOv_ZBase), IncludeDerived = true)]
        public interface IMarkOv_Iface { }
        [Dec.Abstract]
        public abstract class MarkOv_ZBase : Dec.Dec { }
        public class MarkOv_DDec : MarkOv_ZBase, IMarkOv_Iface
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("d.M");
            }
        }

        [Test]
        public void TestClassLevelMarkerInterfaceSelfOverlapErrors()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MarkOv_DDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MarkOv_DDec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("part of"));

            CollectionAssert.AreEqual(new[] { "d.M" }, setupOrder);
        }

        // Classes for TestInterfaceLegacyBindErrors; a contract member binding to Dec's built-in hooks would run them twice, so it's rejected at declaration
        public interface ILegacyBind_Iface
        {
            [Dec.Setup]
            void ConfigErrors(Action<string> reporter);
        }
        public class LegacyBind_Dec : Dec.Dec, ILegacyBind_Iface
        {
            #pragma warning disable CS0672
            public override void ConfigErrors(Action<string> reporter)
            {
                RecordSetup("cfg");
            }
            #pragma warning restore CS0672
        }

        [Test]
        public void TestInterfaceLegacyBindErrors()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LegacyBind_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LegacyBind_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("already run automatically"));

            // exactly once, through the built-in pass
            CollectionAssert.AreEqual(new[] { "cfg" }, setupOrder);
        }

        // Classes for TestInterfaceStaticMemberErrors
        public interface IStatIf_Iface
        {
            [Dec.Setup]
            static void Init(Action<string> reporter) { }
        }
        public class StatIf_Dec : Dec.Dec, IStatIf_Iface { }

        [Test]
        public void TestInterfaceStaticMemberErrors()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StatIf_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StatIf_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("static"));

            CollectionAssert.AreEqual(new string[] { }, setupOrder);
        }

        // Classes for TestInterfaceScanDiagnostics; a malformed contract declaration must surface from the parser's scan even when nothing implements the interface
        public interface IScanBad_Iface
        {
            [Dec.Setup]
            void M(int wrong);
        }

        [Test]
        public void TestInterfaceScanDiagnostics()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitSetupScanTypes = new Type[] { typeof(IScanBad_Iface) } });

            // the setup scan runs during parser construction
            ExpectErrors(() =>
            {
                var parser = new Dec.Parser();
                parser.Finish();
            }, err => err.Contains("unsupported signature"));

            CollectionAssert.AreEqual(new string[] { }, setupOrder);
        }

        // Classes for TestInterfaceMemberOrderingAttrs; [Dec.SetupAfter] on the interface member constrains every implementation, and the implementor's tiebreak would run first without it
        public interface IMemOrd_Iface
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(MemOrd_ZTargetDec))]
            void M(Action<string> reporter);
        }
        public class MemOrd_ZTargetDec : Dec.Dec
        {
            [Dec.Setup]
            internal void MT(Action<string> reporter)
            {
                RecordSetup("target.MT");
            }
        }
        public class MemOrd_AImplDec : Dec.Dec, IMemOrd_Iface
        {
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }

        [Test]
        public void TestInterfaceMemberOrderingAttrs()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MemOrd_ZTargetDec), typeof(MemOrd_AImplDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MemOrd_ZTargetDec decName=""T"" />
                    <MemOrd_AImplDec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "target.MT", "impl.M" }, setupOrder);
        }

        // Classes for TestInterfaceInheritedMember; the contract of a derived interface includes members tagged on its base interfaces
        public interface IInhB_Base
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public interface IInhB_Derived : IInhB_Base { }
        public class InhB_ZDec : Dec.Dec, IInhB_Derived
        {
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }
        public class InhB_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IInhB_Derived))]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestInterfaceInheritedMember()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(InhB_ZDec), typeof(InhB_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <InhB_ZDec decName=""Z"" />
                    <InhB_AUserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "impl.M", "user.MU" }, setupOrder);
        }

        // Classes for TestInterfaceBadSignature
        public interface IBadSigI_Iface
        {
            [Dec.Setup]
            void M(int wrong);
        }
        public class BadSigI_Dec : Dec.Dec, IBadSigI_Iface
        {
            public void M(int wrong) { }
        }

        [Test]
        public void TestInterfaceBadSignature()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BadSigI_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <BadSigI_Dec decName=""A"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("unsupported signature"));

            CollectionAssert.AreEqual(new string[] { }, setupOrder);
        }

        // Classes for TestUntaggedInterfaceHint; the interface declares no contract, so the bare dependency is a likely IncludeDerived mistake and the warning must say so
        public interface IHintI_Iface { }
        public class HintI_ZDec : Dec.Dec, IHintI_Iface
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }
        public class HintI_AUserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IHintI_Iface))]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestUntaggedInterfaceHint()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(HintI_ZDec), typeof(HintI_AUserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <HintI_ZDec decName=""Z"" />
                    <HintI_AUserDec decName=""U"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("IncludeDerived"));

            CollectionAssert.AreEquivalent(new[] { "impl.M", "user.MU" }, setupOrder);
        }

        // Classes for TestTwoInterfaceContractsOneMethod; one body satisfying two contracts is two functions and runs once per contract
        public interface ITwoA_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public interface ITwoB_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class Two_Dec : Dec.Dec, ITwoA_Iface, ITwoB_Iface
        {
            public void M(Action<string> reporter)
            {
                RecordSetup("impl.M");
            }
        }

        [Test]
        public void TestTwoInterfaceContractsOneMethod()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Two_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Two_Dec decName=""A"" />
                </Decs>");
            parser.Finish();

            CollectionAssert.AreEqual(new[] { "impl.M", "impl.M" }, setupOrder);
        }

        // Classes for TestClassLevelMarkerOwnSideSilent; the marker's own class-level constraint is enforced on derived owners through attribute inheritance, so the marker's own-setup emptiness must not draw the IncludeDerived hint
        public class OwnSideM_TargetDec : Dec.Dec
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("target.M");
            }
        }
        [Dec.SetupBefore(typeof(OwnSideM_TargetDec))]
        public class OwnSideM_Marker { }
        public class OwnSideM_Derived : OwnSideM_Marker
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        public class OwnSideM_HolderDec : Dec.Dec
        {
            public OwnSideM_Derived obj;
        }
        public class OwnSideM_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(OwnSideM_Marker), IncludeDerived = true)]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestClassLevelMarkerOwnSideSilent()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(OwnSideM_TargetDec), typeof(OwnSideM_HolderDec), typeof(OwnSideM_UserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <OwnSideM_TargetDec decName=""T"" />
                    <OwnSideM_HolderDec decName=""H"">
                        <obj />
                    </OwnSideM_HolderDec>
                    <OwnSideM_UserDec decName=""U"" />
                </Decs>");
            parser.Finish();

            // the inherited class-level Before applies through the derived owner, and the IncludeDerived dependency reaches the derived member
            Assert.Less(setupOrder.IndexOf("derived.M"), setupOrder.IndexOf("target.M"));
            Assert.Less(setupOrder.IndexOf("derived.M"), setupOrder.IndexOf("user.MU"));
        }

        // Classes for TestEmptyDeclaredOnStageHints; the base declares nothing, so the dependency is a likely IncludeDerived mistake and the warning must say so. Non-dec classes are load-bearing here: a dec base always has ConfigErrors/PostLoad in its own setup, so this hint can never fire for dec hierarchies.
        public class Hint_Base { }
        public class Hint_Derived : Hint_Base
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("derived.M");
            }
        }
        public class Hint_HolderDec : Dec.Dec
        {
            public Hint_Derived obj;
        }
        public class Hint_UserDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(Hint_Base))]
            internal void MU(Action<string> reporter)
            {
                RecordSetup("user.MU");
            }
        }

        [Test]
        public void TestEmptyDeclaredOnStageHints()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Hint_HolderDec), typeof(Hint_UserDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Hint_HolderDec decName=""H"">
                        <obj />
                    </Hint_HolderDec>
                    <Hint_UserDec decName=""U"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("IncludeDerived"));

            CollectionAssert.AreEquivalent(new[] { "derived.M", "user.MU" }, setupOrder);
        }

        // Classes for TestEmptyStageMixedVariantTransitivity; the marker is empty, but a declared-on Before and an IncludeDerived After routed through it must still order the endpoints. Adversarial tiebreak: A would run first without the constraints.
        public class MixTrans_Marker { }
        public class MixTrans_ADec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(MixTrans_Marker), IncludeDerived = true)]
            internal void M(Action<string> reporter)
            {
                RecordSetup("A.M");
            }
        }
        public class MixTrans_ZDec : Dec.Dec
        {
            [Dec.Setup]
            [Dec.SetupBefore(typeof(MixTrans_Marker))]
            internal void M(Action<string> reporter)
            {
                RecordSetup("Z.M");
            }
        }

        [Test]
        public void TestEmptyStageMixedVariantTransitivity()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(MixTrans_ADec), typeof(MixTrans_ZDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <MixTrans_ADec decName=""A"" />
                    <MixTrans_ZDec decName=""Z"" />
                </Decs>");
            ExpectWarnings(() => parser.Finish(), wrn => wrn.Contains("no setup functions"));

            CollectionAssert.AreEqual(new[] { "Z.M", "A.M" }, setupOrder);
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
            // the exact dec-side format matters; non-dec reports gained a path prefix and this one deliberately didn't change
            ExpectErrors(() => parser.Finish(), err => err.Contains("intentional setup gripe") && err.Contains("[Reporter_Dec:A]"));

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

            // only the modes that clear the database and reparse re-run setup
            bool reparses = mode == ParserMode.RewrittenPretty || mode == ParserMode.RewrittenBare || mode == ParserMode.Reflection;
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

        // Classes for TestReadIncludeDerived; IncludeDerived must reach a derived-introduced function inside a Read too, and the tiebreak would run A first without the edge
        public class ReadInc_Base { }
        public class ReadInc_ZObj : ReadInc_Base, Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                RecordSetup("Z.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReadInc_AObj : Dec.IRecordable
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(ReadInc_Base), IncludeDerived = true)]
            internal void M(Action<string> reporter)
            {
                RecordSetup("A.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReadInc_Root : Dec.IRecordable
        {
            public ReadInc_AObj a;
            public ReadInc_ZObj z;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref a, "a");
                recorder.Record(ref z, "z");
            }
        }

        [Test]
        public void TestReadIncludeDerived()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReadInc_Root { a = new ReadInc_AObj(), z = new ReadInc_ZObj() });
            Dec.Recorder.Read<ReadInc_Root>(serialized);

            CollectionAssert.AreEqual(new[] { "Z.M", "A.M" }, setupOrder);
        }

        // Classes for TestReadInterfaceContract
        public interface IReadC_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class ReadC_Obj : Dec.IRecordable, IReadC_Iface
        {
            public void M(Action<string> reporter)
            {
                RecordSetup("obj.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }

        [Test]
        public void TestReadInterfaceContract()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReadC_Obj());
            Dec.Recorder.Read<ReadC_Obj>(serialized);

            CollectionAssert.AreEqual(new[] { "obj.M" }, setupOrder);
        }

        // Classes for TestReadInterfaceAbsentDepSatisfied; interface contract dependencies referencing nothing present in this read are satisfied, not diagnosed
        public interface IReadCAbs_Iface
        {
            [Dec.Setup]
            void M(Action<string> reporter);
        }
        public class ReadCAbs_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            [Dec.SetupAfter(typeof(IReadCAbs_Iface))]
            [Dec.SetupAfter(typeof(IReadCAbs_Iface), "M")]
            internal void M(Action<string> reporter)
            {
                RecordSetup("present.M");
            }

            public void Record(Dec.Recorder recorder) { }
        }

        [Test]
        public void TestReadInterfaceAbsentDepSatisfied()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReadCAbs_Obj());
            Dec.Recorder.Read<ReadCAbs_Obj>(serialized);

            CollectionAssert.AreEqual(new[] { "present.M" }, setupOrder);
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

        // ExpectErrors requires *every* error it sees to match its validator, so tests that want to inspect several distinct messages collect them all and assert afterwards.
        private List<string> CollectErrors(Action action)
        {
            var reported = new List<string>();
            ExpectErrors(action, err => { reported.Add(err); return true; });
            return reported;
        }

        // Classes for TestReporterNonDec
        public class ReportND_Obj
        {
            public int id;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("nondec gripe");
            }
        }
        public class ReportND_Dec : Dec.Dec
        {
            public ReportND_Obj obj;
        }

        [Test]
        public void TestReporterNonDec()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReportND_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReportND_Dec decName=""A"">
                        <obj><id>1</id></obj>
                    </ReportND_Dec>
                    <ReportND_Dec decName=""B"">
                        <obj><id>2</id></obj>
                    </ReportND_Dec>
                </Decs>");
            var reported = CollectErrors(() => parser.Finish());

            // the entire point of the feature: two instances of one type produce two distinguishable reports
            Assert.AreEqual(2, reported.Count);
            Assert.IsTrue(reported.Any(r => r.Contains("ReportND_Dec.A.obj")), string.Join(" / ", reported));
            Assert.IsTrue(reported.Any(r => r.Contains("ReportND_Dec.B.obj")), string.Join(" / ", reported));
            Assert.IsTrue(reported.All(r => r.Contains("ReportND_Obj") && r.Contains("nondec gripe")), string.Join(" / ", reported));
        }

        // Classes for TestReporterNonDecIndexed
        public class ReportNDIdx_Obj
        {
            public int id;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("indexed gripe");
            }
        }
        public class ReportNDIdx_Dec : Dec.Dec
        {
            public List<ReportNDIdx_Obj> list;
        }

        [Test]
        public void TestReporterNonDecIndexed()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReportNDIdx_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReportNDIdx_Dec decName=""A"">
                        <list>
                            <li><id>0</id></li>
                            <li><id>1</id></li>
                        </list>
                    </ReportNDIdx_Dec>
                </Decs>");
            var reported = CollectErrors(() => parser.Finish());

            Assert.AreEqual(2, reported.Count);
            Assert.IsTrue(reported.Any(r => r.Contains("ReportNDIdx_Dec.A.list[0]")), string.Join(" / ", reported));
            Assert.IsTrue(reported.Any(r => r.Contains("ReportNDIdx_Dec.A.list[1]")), string.Join(" / ", reported));
        }

        // Classes for TestReporterNonDecUnpathable
        public class ReportNDSet_Obj
        {
            public int id;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("set gripe");
            }
        }
        public class ReportNDSet_Dec : Dec.Dec
        {
            public HashSet<ReportNDSet_Obj> set;
        }

        [Test]
        public void TestReporterNonDecUnpathable()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReportNDSet_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReportNDSet_Dec decName=""A"">
                        <set>
                            <li><id>0</id></li>
                            <li><id>1</id></li>
                        </set>
                    </ReportNDSet_Dec>
                </Decs>");
            var reported = CollectErrors(() => parser.Finish());

            // Known limitation, pinned deliberately: set elements have no individually addressable path, so both instances report at the same place. This is as good as the path system currently gets.
            Assert.AreEqual(2, reported.Count);
            Assert.IsTrue(reported.All(r => r.Contains("ReportNDSet_Dec.A.set[SETELEM]")), string.Join(" / ", reported));
        }

        // Classes for TestReporterNonDecShared
        public class ReportNDShare_Payload
        {
            public static ReportNDShare_Payload Singleton;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("shared gripe");
            }
        }
        public class ReportNDShare_Converter : Dec.ConverterString<ReportNDShare_Payload>
        {
            public override ReportNDShare_Payload Read(string input, Dec.Context context)
            {
                return ReportNDShare_Payload.Singleton;
            }

            public override string Write(ReportNDShare_Payload input)
            {
                return "singleton";
            }
        }
        public class ReportNDShare_Dec : Dec.Dec
        {
            public ReportNDShare_Payload payload;
        }

        [Test]
        public void TestReporterNonDecShared()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReportNDShare_Dec) }, explicitConverters = new Type[] { typeof(ReportNDShare_Converter) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReportNDShare_Dec decName=""A"">
                        <payload>x</payload>
                    </ReportNDShare_Dec>
                    <ReportNDShare_Dec decName=""B"">
                        <payload>x</payload>
                    </ReportNDShare_Dec>
                </Decs>");
            var reported = CollectErrors(() => parser.Finish());

            // One object reached through two decs: setup runs once, and it reports at one of the two places it was found. Which one isn't pinned here - that would be testing dec iteration order - but it must be a real member path, not a fallback.
            Assert.AreEqual(1, reported.Count);
            Assert.IsTrue(reported[0].Contains("ReportNDShare_Dec.A.payload") || reported[0].Contains("ReportNDShare_Dec.B.payload"), reported[0]);
        }

        // Classes for TestReporterNonDecRecorder
        public class ReportNDRec_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("recorder gripe");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReportNDRec_Root : Dec.IRecordable
        {
            public ReportNDRec_Obj member;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref member, "member");
            }
        }

        [Test]
        public void TestReporterNonDecRecorder()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReportNDRec_Root { member = new ReportNDRec_Obj() });
            var reported = CollectErrors(() => Dec.Recorder.Read<ReportNDRec_Root>(serialized));

            Assert.AreEqual(1, reported.Count);
            Assert.IsTrue(reported[0].Contains("RECORD.member"), reported[0]);
        }

        // Classes for TestReporterNonDecRecorderShared; a shared object is parsed through its <Ref> node before the root parse ever reaches a pointer to it, so the useful structural path arrives second
        public class ReportNDShareRec_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("shared recorder gripe");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReportNDShareRec_Root : Dec.IRecordable
        {
            public ReportNDShareRec_Obj one;
            public ReportNDShareRec_Obj two;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref one, "one");
                recorder.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void TestReporterNonDecRecorderShared()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var root = new ReportNDShareRec_Root();
            root.one = new ReportNDShareRec_Obj();
            root.two = root.one;

            var serialized = Dec.Recorder.Write(root);
            var reported = CollectErrors(() => Dec.Recorder.Read<ReportNDShareRec_Root>(serialized));

            Assert.AreEqual(1, reported.Count);
            Assert.IsTrue(reported[0].Contains("RECORD.one"), reported[0]);
            Assert.IsFalse(reported[0].Contains("REF."), reported[0]);
        }

        // Classes for TestReporterNonDecRecorderNestedShared
        public class ReportNDNest_Leaf : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("leaf gripe");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReportNDNest_Mid : Dec.IRecordable
        {
            public ReportNDNest_Leaf a;
            public ReportNDNest_Leaf b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, "a");
                recorder.Shared().Record(ref b, "b");
            }
        }
        public class ReportNDNest_Root : Dec.IRecordable
        {
            public ReportNDNest_Mid one;
            public ReportNDNest_Mid two;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref one, "one");
                recorder.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void TestReporterNonDecRecorderNestedShared()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var root = new ReportNDNest_Root();
            root.one = new ReportNDNest_Mid();
            root.two = root.one;
            root.one.a = new ReportNDNest_Leaf();
            root.one.b = root.one.a;

            var serialized = Dec.Recorder.Write(root);
            var reported = CollectErrors(() => Dec.Recorder.Read<ReportNDNest_Root>(serialized));

            // A shared object reachable only through other shared objects has no path that can re-find it from the root, so it's identified by its own reference. Pinned deliberately: it's unique and greppable in the savegame, but it won't say who pointed at it.
            Assert.AreEqual(1, reported.Count);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(reported[0], @"^REF\.[^.]+ \("), reported[0]);
            Assert.IsTrue(reported[0].Contains("ReportNDNest_Leaf"), reported[0]);
        }

        // Classes for TestReporterNonDecRecorderNestedSharedConverter; structurally identical to the nested-shared case above, but routed through a ConverterRecord, which reaches the setup collection in a different order
        public class ReportNDNestConv_Leaf
        {
            public int value;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("converter leaf gripe");
            }
        }
        public class ReportNDNestConv_Converter : Dec.ConverterRecord<ReportNDNestConv_Leaf>
        {
            public override void Record(ref ReportNDNestConv_Leaf input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
            }
        }
        public class ReportNDNestConv_Mid : Dec.IRecordable
        {
            public ReportNDNestConv_Leaf a;
            public ReportNDNestConv_Leaf b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, "a");
                recorder.Shared().Record(ref b, "b");
            }
        }
        public class ReportNDNestConv_Root : Dec.IRecordable
        {
            public ReportNDNestConv_Mid one;
            public ReportNDNestConv_Mid two;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref one, "one");
                recorder.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void TestReporterNonDecRecorderNestedSharedConverter()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { }, explicitConverters = new Type[] { typeof(ReportNDNestConv_Converter) } });

            var root = new ReportNDNestConv_Root();
            root.one = new ReportNDNestConv_Mid();
            root.two = root.one;
            root.one.a = new ReportNDNestConv_Leaf { value = 5 };
            root.one.b = root.one.a;

            var serialized = Dec.Recorder.Write(root);
            var reported = CollectErrors(() => Dec.Recorder.Read<ReportNDNestConv_Root>(serialized));

            // Same answer as the non-converter case: the object's own reference, not a pointer to it from inside another reference. Which registration arrives first differs between the two, so this pins that the choice is made by rule and not by arrival order.
            Assert.AreEqual(1, reported.Count);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(reported[0], @"^REF\.[^.]+ \("), reported[0]);
            Assert.IsTrue(reported[0].Contains("ReportNDNestConv_Leaf"), reported[0]);
        }

        // Classes for TestReporterNonDecDictionaryValue
        public class ReportNDDict_Obj
        {
            public int id;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("dict gripe");
            }
        }
        public class ReportNDDict_Dec : Dec.Dec
        {
            public Dictionary<string, ReportNDDict_Obj> dict;
        }

        [Test]
        public void TestReporterNonDecDictionaryValue()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReportNDDict_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReportNDDict_Dec decName=""A"">
                        <dict>
                            <alpha><id>0</id></alpha>
                            <beta><id>1</id></beta>
                        </dict>
                    </ReportNDDict_Dec>
                </Decs>");
            var reported = CollectErrors(() => parser.Finish());

            Assert.AreEqual(2, reported.Count);
            Assert.IsTrue(reported.Any(r => r.Contains("ReportNDDict_Dec.A.dict[alpha]")), string.Join(" / ", reported));
            Assert.IsTrue(reported.Any(r => r.Contains("ReportNDDict_Dec.A.dict[beta]")), string.Join(" / ", reported));
        }

        // Classes for TestReporterNonDecReadSimple
        public class ReportNDSimple_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                reporter("simple gripe");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReportNDSimple_Root : Dec.IRecordable
        {
            public ReportNDSimple_Obj member;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref member, "member");
            }
        }

        [Test]
        public void TestReporterNonDecReadSimple()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.WriteSimple(new ReportNDSimple_Root { member = new ReportNDSimple_Obj() }, "root");
            var reported = CollectErrors(() => Dec.Recorder.ReadSimple<ReportNDSimple_Root>(serialized, "root"));

            // ReadSimple roots at the caller's tag name instead of RECORD
            Assert.AreEqual(1, reported.Count);
            Assert.IsTrue(reported[0].Contains("root.member"), reported[0]);
        }

        // Classes for TestReporterNonDecParallel
        public class ReportNDPar_Obj
        {
            public int id;

            [Dec.Setup(Parallel = true)]
            internal void M(Action<string> reporter)
            {
                reporter("parallel gripe");
            }
        }
        public class ReportNDPar_Dec : Dec.Dec
        {
            public ReportNDPar_Obj obj;
        }

        [Test]
        public void TestReporterNonDecParallel()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReportNDPar_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReportNDPar_Dec decName=""A"">
                        <obj><id>1</id></obj>
                    </ReportNDPar_Dec>
                    <ReportNDPar_Dec decName=""B"">
                        <obj><id>2</id></obj>
                    </ReportNDPar_Dec>
                </Decs>");

            // parallel reports arrive from worker threads, which the thread-static ExpectErrors machinery can't see
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

            Assert.AreEqual(2, reported.Count);
            Assert.IsTrue(reported.Any(r => r.Contains("ReportNDPar_Dec.A.obj")), string.Join(" / ", reported));
            Assert.IsTrue(reported.Any(r => r.Contains("ReportNDPar_Dec.B.obj")), string.Join(" / ", reported));
        }

        // Classes for TestReporterNonDecException
        public class ReportNDEx_Obj
        {
            public int id;

            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                throw new InvalidOperationException("intentional nondec explosion");
            }
        }
        public class ReportNDEx_Dec : Dec.Dec
        {
            public ReportNDEx_Obj obj;
        }

        [Test]
        public void TestReporterNonDecException()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ReportNDEx_Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ReportNDEx_Dec decName=""A"">
                        <obj><id>1</id></obj>
                    </ReportNDEx_Dec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("intentional nondec explosion") && err.Contains("ReportNDEx_Dec.A.obj"));
        }

        // Classes for TestReporterNonDecRecorderException
        public class ReportNDRecEx_Obj : Dec.IRecordable
        {
            [Dec.Setup]
            internal void M(Action<string> reporter)
            {
                throw new InvalidOperationException("intentional recorder nondec explosion");
            }

            public void Record(Dec.Recorder recorder) { }
        }
        public class ReportNDRecEx_Root : Dec.IRecordable
        {
            public ReportNDRecEx_Obj member;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref member, "member");
            }
        }

        [Test]
        public void TestReporterNonDecRecorderException()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { } });

            var serialized = Dec.Recorder.Write(new ReportNDRecEx_Root { member = new ReportNDRecEx_Obj() });
            ExpectErrors(() => Dec.Recorder.Read<ReportNDRecEx_Root>(serialized), err => err.Contains("intentional recorder nondec explosion") && err.Contains("RECORD.member"));
        }
    }
}
