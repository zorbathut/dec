namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class References : Base
    {
        public class RefTargetDef : Def.Def
        {

        }

        public class RefSourceDef : Def.Def
        {
            public RefTargetDef target;
        }

        public class RefCircularDef : Def.Def
        {
            public RefCircularDef target;
        }

        [Test]
	    public void Basic()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefTargetDef), typeof(RefSourceDef) });
            parser.AddString(@"
                <Defs>
                    <RefTargetDef defName=""Target"" />
                    <RefSourceDef defName=""Source"">
                        <target>Target</target>
                    </RefSourceDef>
                </Defs>");
            parser.Finish();

            var target = Def.Database<RefTargetDef>.Get("Target");
            var source = Def.Database<RefSourceDef>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
	    }

        [Test]
	    public void Reversed()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefTargetDef), typeof(RefSourceDef) });
            parser.AddString(@"
                <Defs>
                    <RefSourceDef defName=""Source"">
                        <target>Target</target>
                    </RefSourceDef>
                    <RefTargetDef defName=""Target"" />
                </Defs>");
            parser.Finish();

            var target = Def.Database<RefTargetDef>.Get("Target");
            var source = Def.Database<RefSourceDef>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
	    }

        [Test]
	    public void Multistring()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefTargetDef), typeof(RefSourceDef) });
            parser.AddString(@"
                <Defs>
                    <RefSourceDef defName=""Source"">
                        <target>Target</target>
                    </RefSourceDef>
                </Defs>");
            parser.AddString(@"
                <Defs>
                    <RefTargetDef defName=""Target"" />
                </Defs>");
            parser.Finish();

            var target = Def.Database<RefTargetDef>.Get("Target");
            var source = Def.Database<RefSourceDef>.Get("Source");
            Assert.IsNotNull(target);
            Assert.IsNotNull(source);

            Assert.AreEqual(source.target, target);
	    }

        [Test]
	    public void Refdef()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefSourceDef) });
            parser.AddString(@"
                <Defs>
                    <RefSourceDef defName=""Source"">
                        Source
                    </RefSourceDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var source = Def.Database<RefSourceDef>.Get("Source");
            Assert.IsNotNull(source);
	    }

        [Test]
	    public void Circular()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefCircularDef) });
            parser.AddString(@"
                <Defs>
                    <RefCircularDef defName=""Alpha"">
                        <target>Beta</target>
                    </RefCircularDef>
                    <RefCircularDef defName=""Beta"">
                        <target>Alpha</target>
                    </RefCircularDef>
                </Defs>");
            parser.Finish();

            var alpha = Def.Database<RefCircularDef>.Get("Alpha");
            var beta = Def.Database<RefCircularDef>.Get("Beta");
            Assert.IsNotNull(alpha);
            Assert.IsNotNull(beta);

            Assert.AreEqual(alpha.target, beta);
            Assert.AreEqual(beta.target, alpha);
	    }

        [Test]
	    public void CircularTight()
	    {
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefCircularDef) });
            parser.AddString(@"
                <Defs>
                    <RefCircularDef defName=""TestDef"">
                        <target>TestDef</target>
                    </RefCircularDef>
                </Defs>");
            parser.Finish();

            var result = Def.Database<RefCircularDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(result.target, result);
	    }

        [Test]
	    public void NullRef()
	    {
            // This is a little wonky; we have to test it by duplicating a tag, which is technically an error
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefCircularDef) });
            parser.AddString(@"
                <Defs>
                    <RefCircularDef defName=""TestDef"">
                        <target>TestDef</target>
                        <target></target>
                    </RefCircularDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<RefCircularDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
	    }

        [Test]
	    public void FailedLookup()
	    {
            // This is a little wonky; we have to test it by duplicating a tag, which is technically an error
            var parser = new Def.Parser(explicitTypes: new Type[]{ typeof(RefSourceDef) });
            parser.AddString(@"
                <Defs>
                    <RefSourceDef defName=""TestDef"">
                        <target>MissingDef</target>
                    </RefSourceDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            var result = Def.Database<RefSourceDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.IsNull(result.target);
	    }
    }
}
