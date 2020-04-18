namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class Permissions : Base
    {
        public class PrivateDef : Def.Def
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
	    public void Private([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PrivateDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <PrivateDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<PrivateDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class InternalDef : Def.Def
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
	    public void Internal([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(InternalDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <InternalDef defName=""TestDef"">
                        <value>20</value>
                    </InternalDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<InternalDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class ProtectedDef : Def.Def
        {
            protected int value;

            public int Value()
            {
                return value;
            }
        }

        [Test]
	    public void Protected([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ProtectedDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <ProtectedDef defName=""TestDef"">
                        <value>20</value>
                    </ProtectedDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<ProtectedDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class PrivateParentDef : Def.Def
        {
            #pragma warning disable CS0649
            private int value;
            #pragma warning restore CS0649

            public int Value()
            {
                return value;
            }
        }

        public class PrivateChildDef : PrivateParentDef
        {

        }

        [Test]
	    public void PrivateParent([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PrivateChildDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <PrivateChildDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateChildDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<PrivateParentDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }
    }
}
