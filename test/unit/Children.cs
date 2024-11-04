using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Children : Base
    {
        public class CCRoot : Dec.Dec
        {
            public CCChild child;
        }

        public class CCChild
        {
            public int value;
            public int initialized = 10;
        }

        [Test]
        public void ChildClass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(CCRoot) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <CCRoot decName=""TestDec"">
                        <child>
                            <value>5</value>
                        </child>
                    </CCRoot>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<CCRoot>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(5, result.child.value);
            Assert.AreEqual(10, result.child.initialized);
        }

        public class CCDRoot : Dec.Dec
        {
            public CCDChild child = new CCDChild() { initialized = 8 };
        }

        public class CCDChild
        {
            public int value;
            public int initialized = 10;
        }

        [Test]
        public void ChildClassDefaults([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(CCDRoot) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <CCDRoot decName=""TestDec"">
                        <child>
                            <value>5</value>
                        </child>
                    </CCDRoot>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<CCDRoot>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(5, result.child.value);
            Assert.AreEqual(8, result.child.initialized);
        }

        public class CSRoot : Dec.Dec
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
        public void ChildStruct([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(CSRoot) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <CSRoot decName=""TestDec"">
                        <child>
                            <value>5</value>
                            <child>
                                <value>8</value>
                            </child>
                        </child>
                    </CSRoot>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<CSRoot>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(5, result.child.value);
            Assert.AreEqual(8, result.child.child.value);
            Assert.AreEqual(0, result.child.valueZero);
        }

        public class ExplicitTypeDec : Dec.Dec
        {
            public ETBase child;
        }

        public class ExplicitTypeDerivedDec : Dec.Dec
        {
            public ETDerived child;
        }

        public class ETBase
        {

        }

        public class ETDerived : ETBase
        {
            public int value;
        }

        public class ETNotDerived
        {

        }

        [Test]
        public void ExplicitType([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Children" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExplicitTypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExplicitTypeDec decName=""TestDec"">
                        <child class=""ETDerived"">
                            <value>5</value>
                        </child>
                    </ExplicitTypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ExplicitTypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.child);
            Assert.AreEqual(typeof(ETDerived), result.child.GetType());
            Assert.AreEqual(5, ( result.child as ETDerived ).value);
        }

        [Test]
        public void ExplicitTypeOverspecify([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Children" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExplicitTypeDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExplicitTypeDec decName=""TestDec"">
                        <child class=""ETBase"">

                        </child>
                    </ExplicitTypeDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ExplicitTypeDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.child);
            Assert.AreEqual(typeof(ETBase), result.child.GetType());
        }

        [Test]
        public void ExplicitTypeBackwards([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Children" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExplicitTypeDerivedDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExplicitTypeDerivedDec decName=""TestDec"">
                        <child class=""ETBase"">

                        </child>
                    </ExplicitTypeDerivedDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ExplicitTypeDerivedDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.child);
            Assert.AreEqual(typeof(ETDerived), result.child.GetType());
        }

        public class ExplicitTypeConflictDec : Dec.Dec
        {
            public ETBase child = new ETDerived();
        }

        public class ETDerivedAlt : ETBase
        {

        }

        [Test]
        public void ExplicitTypeConflict([Values] ParserMode mode)
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.Children" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ExplicitTypeConflictDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ExplicitTypeConflictDec decName=""TestDec"">
                        <child class=""ETDerivedAlt"" />
                    </ExplicitTypeConflictDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ExplicitTypeConflictDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.child);

            // I'm not really sure this is the right outcome - maybe it should be ETDerivedAlt - but it also kind of doesn't matter, this shouldn't happen
            Assert.AreEqual(typeof(ETDerived), result.child.GetType());
        }

        public class ChildPrivate
        {
            private ChildPrivate() { }
        }

        public class ChildParameter
        {
            public ChildParameter(int x) { }
        }

        public class ChildException
        {
            public ChildException() { throw new Exception(); }
        }

        public class ChildValid
        {

        }

        public class ContainerDec : Dec.Dec
        {
            public ChildValid childValid1;
            public ChildPrivate childPrivate;
            public ChildValid childValid2;
            public ChildParameter childParameter;
            public ChildValid childValid3;
            public ChildException childException;
            public ChildValid childValid4;
        }

        [Test]
        public void Private([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ContainerDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ContainerDec decName=""TestDec"">
                        <childValid1 />
                        <childPrivate />
                        <childValid2 />
                        <childParameter />
                        <childValid3 />
                        <childException />
                        <childValid4 />
                    </ContainerDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ContainerDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.childPrivate);
            Assert.IsNull(result.childParameter);
            Assert.IsNull(result.childException);

            Assert.IsNotNull(result.childValid1);
            Assert.IsNotNull(result.childValid2);
            Assert.IsNotNull(result.childValid3);
            Assert.IsNotNull(result.childValid4);
        }

        [Test]
        public void Parameter([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ContainerDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ContainerDec decName=""TestDec"">
                        <childParameter />
                    </ContainerDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ContainerDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.childParameter);
        }

        [Test]
        public void Exception([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ContainerDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ContainerDec decName=""TestDec"">
                        <childException />
                    </ContainerDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ContainerDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.childException);
        }

        public class ObjectHolderDec : Dec.Dec
        {
            public object child;
        }

        [Test]
        public void Object([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ObjectHolderDec), typeof(ETBase) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ObjectHolderDec decName=""ETDec"">
                        <child class=""ETBase"" />
                    </ObjectHolderDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsInstanceOf(typeof(ETBase), Dec.Database<ObjectHolderDec>.Get("ETDec").child);
        }

        [Test]
        public void ObjectUnspecified([Values] ParserMode mode)
        {
            // so it turns out you can just be all "new object" and C# is like "okiedokie you're the boss"
            // wasn't really expecting that

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ObjectHolderDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ObjectHolderDec decName=""TestDec"">
                        <child />
                    </ObjectHolderDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ObjectHolderDec>.Get("TestDec");
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.child);
        }

        [Test]
        public void ObjectInt([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ObjectHolderDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ObjectHolderDec decName=""TestDec"">
                        <child class=""int"">42</child>
                    </ObjectHolderDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(42, Dec.Database<ObjectHolderDec>.Get("TestDec").child);
        }

        public class Int32Dec : Dec.Dec
        {
            public System.Int32 i32; // why would you do this
        }

        [Test]
        public void Int32([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(Int32Dec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <Int32Dec decName=""TestDec"">
                        <i32>42</i32>
                    </Int32Dec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(42, Dec.Database<Int32Dec>.Get("TestDec").i32);
        }
    }
}
