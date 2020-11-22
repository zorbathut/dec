namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class DefInheritance : Base
    {
        public class SimpleDef : Def.Def
        {
            public int defaulted;
            public int overridden;
            public int doubleOverridden;

            public class SubObject
            {
                public int defaulted;
                public int overridden;
            }
            public SubObject subObject;

            public List<int> list;
        }

        [Test]
        public void Simple([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Base"" abstract=""true"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDef>
  
                    <SimpleDef defName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<SimpleDef>.Get("Thing");
            Assert.AreEqual(3, result.defaulted);
            Assert.AreEqual(60, result.overridden);
            Assert.AreEqual(12, result.subObject.defaulted);
            Assert.AreEqual(90, result.subObject.overridden);

            Assert.IsNull(Def.Database<SimpleDef>.Get("Base"));
        }

        [Test]
        public void List([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Base"" abstract=""true"">
                        <list>
                          <li>2</li>
                          <li>4</li>
                        </list>
                    </SimpleDef>
  
                    <SimpleDef defName=""Thing"" parent=""Base"">
                        <list>
                          <li>50</li>
                        </list>
                    </SimpleDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<SimpleDef>.Get("Thing");
            Assert.AreEqual(1, result.list.Count);
            Assert.AreEqual(50, result.list[0]);

            Assert.IsNull(Def.Database<SimpleDef>.Get("Base"));
        }

        [Test]
        public void FromNonAbstract([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Base"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDef>
  
                    <SimpleDef defName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var bas = Def.Database<SimpleDef>.Get("Base");
            Assert.AreEqual(3, bas.defaulted);
            Assert.AreEqual(42, bas.overridden);
            Assert.AreEqual(12, bas.subObject.defaulted);
            Assert.AreEqual(80, bas.subObject.overridden);

            var thing = Def.Database<SimpleDef>.Get("Thing");
            Assert.AreEqual(3, thing.defaulted);
            Assert.AreEqual(60, thing.overridden);
            Assert.AreEqual(12, thing.subObject.defaulted);
            Assert.AreEqual(90, thing.subObject.overridden);
        }

        [Test]
        public void MissingConcrete([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Base"" abstract=""true"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(0, Def.Database<SimpleDef>.Count);
        }

        [Test]
        public void MissingBase([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Def.Database<SimpleDef>.Get("Thing");
            Assert.AreEqual(60, result.overridden);
            Assert.AreEqual(90, result.subObject.overridden);
        }

        [Test]
        public void AbstractOverride([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Base"" abstract=""true"" />
                    <SimpleDef defName=""Base"" abstract=""true"" />
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(0, Def.Database<SimpleDef>.Count);
        }

        [Test]
        public void AbstractNonabstractClash([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Before"" abstract=""true"">
                        <overridden>10</overridden>
                     </SimpleDef>
                    <SimpleDef defName=""Before"">
                        <overridden>20</overridden>
                     </SimpleDef>
                    <SimpleDef defName=""After"">
                        <overridden>30</overridden>
                     </SimpleDef>
                    <SimpleDef defName=""After"" abstract=""true"">
                        <overridden>40</overridden>
                     </SimpleDef>
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(20, Def.Database<SimpleDef>.Get("Before").overridden);
            Assert.AreEqual(30, Def.Database<SimpleDef>.Get("After").overridden);
        }

        [Test]
        public void BadAbstractTag([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <SimpleDef defName=""Obj"" abstract=""cheese"">
                        <overridden>10</overridden>
                     </SimpleDef>
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(10, Def.Database<SimpleDef>.Get("Obj").overridden);
        }

        [Test]
        public void DoubleInheritance([Values] bool trunkAbstract, [Values] bool branchAbstract, [Values] bool leafAbstract, [Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDef) } };

            var parser = new Def.Parser();
            parser.AddString($@"
                <Defs>
                    <SimpleDef defName=""Trunk"" {(trunkAbstract ? "abstract=\"true\"" : "")}>
                        <defaulted>1</defaulted>
                        <overridden>2</overridden>
                        <doubleOverridden>3</doubleOverridden>
                     </SimpleDef>
                    <SimpleDef defName=""Branch"" parent=""Trunk"" {(branchAbstract ? "abstract=\"true\"" : "")}>
                        <overridden>20</overridden>
                        <doubleOverridden>30</doubleOverridden>
                     </SimpleDef>
                    <SimpleDef defName=""Leaf"" parent=""Branch"" {(leafAbstract ? "abstract=\"true\"" : "")}>
                        <doubleOverridden>300</doubleOverridden>
                     </SimpleDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            if (trunkAbstract)
            {
                Assert.IsNull(Def.Database<SimpleDef>.Get("Trunk"));
            }
            else
            {
                Assert.AreEqual(1, Def.Database<SimpleDef>.Get("Trunk").defaulted);
                Assert.AreEqual(2, Def.Database<SimpleDef>.Get("Trunk").overridden);
                Assert.AreEqual(3, Def.Database<SimpleDef>.Get("Trunk").doubleOverridden);
            }

            if (branchAbstract)
            {
                Assert.IsNull(Def.Database<SimpleDef>.Get("Branch"));
            }
            else
            {
                Assert.AreEqual(1, Def.Database<SimpleDef>.Get("Branch").defaulted);
                Assert.AreEqual(20, Def.Database<SimpleDef>.Get("Branch").overridden);
                Assert.AreEqual(30, Def.Database<SimpleDef>.Get("Branch").doubleOverridden);
            }

            if (leafAbstract)
            {
                Assert.IsNull(Def.Database<SimpleDef>.Get("Leaf"));
            }
            else
            {
                Assert.AreEqual(1, Def.Database<SimpleDef>.Get("Leaf").defaulted);
                Assert.AreEqual(20, Def.Database<SimpleDef>.Get("Leaf").overridden);
                Assert.AreEqual(300, Def.Database<SimpleDef>.Get("Leaf").doubleOverridden);
            }
        }
    }
}
