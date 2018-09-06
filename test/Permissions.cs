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
            var parser = new Def.Parser(new Type[]{ typeof(PrivateDef) });
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
            internal int value;

            public int Value()
            {
                return value;
            }
        }

        [Test]
	    public void Internal()
	    {
            var parser = new Def.Parser(new Type[]{ typeof(InternalDef) });
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
            var parser = new Def.Parser(new Type[]{ typeof(ProtectedDef) });
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
            var parser = new Def.Parser(new Type[]{ typeof(PrivateChildDef) });
            parser.AddString(@"
                <Defs>
                    <PrivateChildDef defName=""TestDef"">
                        <value>20</value>
                    </PrivateChildDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<PrivateChildDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.Value(), 20);
	    }
    }
}
