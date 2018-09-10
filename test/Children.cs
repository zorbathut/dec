namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Children : Base
    {
        public class CCRoot : Def.Def
        {
            public CCChild child;
        }

        public class CCChild
        {
            public int value;
            public int initialized = 10;
        }

        [Test]
	    public void ChildClass()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(CCRoot) });
            parser.AddString(@"
                <Defs>
                    <CCRoot defName=""TestDef"">
                        <child>
                            <value>5</value>
                        </child>
                    </CCRoot>
                </Defs>");
            parser.Finish();

            var result = Def.Database<CCRoot>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(5, result.child.value);
            Assert.AreEqual(10, result.child.initialized);
	    }

        public class CCDRoot : Def.Def
        {
            public CCDChild child = new CCDChild() { initialized = 8 };
        }

        public class CCDChild
        {
            public int value;
            public int initialized = 10;
        }

        [Test]
	    public void ChildClassDefaults()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(CCDRoot) });
            parser.AddString(@"
                <Defs>
                    <CCDRoot defName=""TestDef"">
                        <child>
                            <value>5</value>
                        </child>
                    </CCDRoot>
                </Defs>");
            parser.Finish();

            var result = Def.Database<CCDRoot>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(5, result.child.value);
            Assert.AreEqual(8, result.child.initialized);
	    }

        public class CSRoot : Def.Def
        {
            public CSChild child;
        }

        public struct CSChild
        {
            public int value;
            public int valueZero;

            public CSGrandChild child;
        }

        public struct CSGrandChild
        {
            public int value;
        }

        [Test]
	    public void ChildStruct()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(CSRoot) });
            parser.AddString(@"
                <Defs>
                    <CSRoot defName=""TestDef"">
                        <child>
                            <value>5</value>
                            <child>
                                <value>8</value>
                            </child>
                        </child>
                    </CSRoot>
                </Defs>");
            parser.Finish();

            var result = Def.Database<CSRoot>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(5, result.child.value);
            Assert.AreEqual(8, result.child.child.value);
            Assert.AreEqual(0, result.child.valueZero);
	    }
    }
}
