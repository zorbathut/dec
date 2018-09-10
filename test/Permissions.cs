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
	    public void Private()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(PrivateDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <PrivateDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateDef>
                </Defs>");
            parser.Finish();

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
	    public void Internal()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(InternalDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <InternalDef defName=""TestDef"">
                        <value>20</value>
                    </InternalDef>
                </Defs>");
            parser.Finish();

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
	    public void Protected()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(ProtectedDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <ProtectedDef defName=""TestDef"">
                        <value>20</value>
                    </ProtectedDef>
                </Defs>");
            parser.Finish();

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
	    public void PrivateParent()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(PrivateChildDef) }, explicitStaticRefs: new Type[]{ });
            parser.AddString(@"
                <Defs>
                    <PrivateChildDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateChildDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<PrivateParentDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }
    }
}
