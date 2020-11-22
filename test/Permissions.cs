namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class Permissions : Base
    {
        public class PrivateMemberDef : Def.Def
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
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PrivateMemberDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <PrivateMemberDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateMemberDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<PrivateMemberDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class InternalMemberDef : Def.Def
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
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(InternalMemberDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <InternalMemberDef defName=""TestDef"">
                        <value>20</value>
                    </InternalMemberDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<InternalMemberDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class ProtectedMemberDef : Def.Def
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
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ProtectedMemberDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <ProtectedMemberDef defName=""TestDef"">
                        <value>20</value>
                    </ProtectedMemberDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<ProtectedMemberDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class PrivateMemberParentDef : Def.Def
        {
            #pragma warning disable CS0649
            private int value;
            #pragma warning restore CS0649

            public int Value()
            {
                return value;
            }
        }

        public class PrivateMemberChildDef : PrivateMemberParentDef
        {

        }

        [Test]
	    public void PrivateMemberParent([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PrivateMemberChildDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <PrivateMemberChildDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateMemberChildDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<PrivateMemberParentDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }
    }
}
