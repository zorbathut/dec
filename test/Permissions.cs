namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class Permissions : Base
    {
        private class PrivateDec : Dec.Dec
        {
            public int value;
        }

        [Test]
        public void Private([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PrivateDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <PrivateDec decName=""TestDec"">
                        <value>20</value>
                    </PrivateDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<PrivateDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value, 20);
        }

        internal class InternalDec : Dec.Dec
        {
            public int value;
        }

        [Test]
        public void Internal([ValuesExcept(BehaviorMode.Validation)] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(InternalDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <InternalDec decName=""TestDec"">
                        <value>20</value>
                    </InternalDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<InternalDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.value, 20);
        }

        public class PrivateMemberDec : Dec.Dec
        {
            #pragma warning disable CS0649
            private int value;
            #pragma warning restore CS0649

            public int Value()
            {
                return value;
            }
        }

        [Test]
	    public void PrivateMember([Values] BehaviorMode mode)
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PrivateMemberDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <PrivateMemberDec decName=""TestDec"">
                        <value>20</value>
                    </PrivateMemberDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<PrivateMemberDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class InternalMemberDec : Dec.Dec
        {
            #pragma warning disable CS0649
            internal int value;
            #pragma warning restore CS0649

            public int Value()
            {
                return value;
            }
        }

        [Test]
	    public void InternalMember([Values] BehaviorMode mode)
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(InternalMemberDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <InternalMemberDec decName=""TestDec"">
                        <value>20</value>
                    </InternalMemberDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<InternalMemberDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class ProtectedMemberDec : Dec.Dec
        {
            protected int value;

            public int Value()
            {
                return value;
            }
        }

        [Test]
	    public void ProtectedMember([Values] BehaviorMode mode)
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ProtectedMemberDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ProtectedMemberDec decName=""TestDec"">
                        <value>20</value>
                    </ProtectedMemberDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<ProtectedMemberDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class PrivateMemberParentDec : Dec.Dec
        {
            #pragma warning disable CS0649
            private int value;
            #pragma warning restore CS0649

            public int Value()
            {
                return value;
            }
        }

        public class PrivateMemberChildDec : PrivateMemberParentDec
        {

        }

        [Test]
	    public void PrivateMemberParent([Values] BehaviorMode mode)
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PrivateMemberChildDec) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <PrivateMemberChildDec decName=""TestDec"">
                        <value>20</value>
                    </PrivateMemberChildDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Dec.Database<PrivateMemberParentDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }
    }
}
