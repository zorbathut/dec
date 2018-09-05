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
            private int value;

            public int Value()
            {
                return value;
            }
        }

        [Test]
	    public void Private()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <PrivateDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateDef>
                </Defs>",
                new Type[]{ typeof(PrivateDef) });

            var result = Def.Database<PrivateDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class InternalDef : Def.Def
        {
            internal int value;

            public int Value()
            {
                return value;
            }
        }

        [Test]
	    public void Internal()
	    {
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <InternalDef defName=""TestDef"">
                        <value>20</value>
                    </InternalDef>
                </Defs>",
                new Type[]{ typeof(InternalDef) });

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
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <ProtectedDef defName=""TestDef"">
                        <value>20</value>
                    </ProtectedDef>
                </Defs>",
                new Type[]{ typeof(ProtectedDef) });

            var result = Def.Database<ProtectedDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }

        public class PrivateParentDef : Def.Def
        {
            private int value;

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
            var parser = new Def.Parser();
            parser.ParseFromString(@"
                <Defs>
                    <PrivateChildDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateChildDef>
                </Defs>",
                new Type[]{ typeof(PrivateChildDef) });

            var result = Def.Database<PrivateChildDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }
    }
}
