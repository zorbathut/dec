using NUnit.Framework;
using System;

namespace DecTest
{
    [TestFixture]
    public class Abstract : Base
    {
        [Dec.Abstract]
        public abstract class AbstractRootDec : Dec.Dec
        {

        }

        public class ChildADec : AbstractRootDec
        {

        }

        public class ChildBDec : AbstractRootDec
        {

        }

        [Test]
        public void DatabaseSplit([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ChildADec), typeof(ChildBDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ChildADec decName=""TestDec"" />
                    <ChildBDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<ChildADec>.Get("TestDec"));
            Assert.IsNotNull(Dec.Database<ChildBDec>.Get("TestDec"));

            Assert.AreNotSame(Dec.Database<ChildADec>.Get("TestDec"), Dec.Database<ChildBDec>.Get("TestDec"));
        }

        [Test]
        public void NoAbstract()    // can't do multitype here because the error is thrown during a static constructor
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ChildADec), typeof(ChildBDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.Finish();

            DoParserTests(ParserMode.Bare);

            ExpectErrors(() => Dec.Database<AbstractRootDec>.Get("TestDec"));
        }

        [Dec.Abstract]
        public class SemiAbstractRootDec : Dec.Dec
        {

        }

        [Test]
        public void SemiAbstract()    // can't do multitype here because the error is thrown during a static constructor
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(SemiAbstractRootDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.Finish();

            DoParserTests(ParserMode.Bare);

            ExpectErrors(() => Dec.Database<SemiAbstractRootDec>.Get("TestDec"));
        }

        public class NotAbstractDec : Dec.Dec
        {

        }

        [Dec.Abstract]
        public abstract class NotAbstractInheritedDec : NotAbstractDec
        {

        }

        public class ConfusingHierarchyDec : NotAbstractInheritedDec
        {

        }

        [Test]
        public void ConfusingHierarchy()    // can't do multitype here because the error is thrown during a static constructor
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ConfusingHierarchyDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.Finish();

            DoParserTests(ParserMode.Bare);

            ExpectErrors(() => Dec.Database<ConfusingHierarchyDec>.Get("TestDec"));
        }
    }
}
