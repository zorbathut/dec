namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Parser : Base
    {
        [Test]
        public void ConflictingParsers()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();
            ExpectErrors(() => new Def.Parser());
        }

        [Test]
        public void LateAddition()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parser = new Def.Parser();
            parser.Finish();

            ExpectErrors(() => parser.AddString("<Defs />"));
        }

        [Test]
        public void MultiFinish()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parser = new Def.Parser();
            parser.Finish();

            ExpectErrors(() => parser.Finish());
        }

        [Test]
        public void PostFinish()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();
            parserA.Finish();

            ExpectErrors(() => new Def.Parser());
        }

        [Test]
        public void PostClear()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();
            parserA.Finish();

            Def.Database.Clear();

            var parserB = new Def.Parser();
            parserB.Finish();
        }

        [Test]
        public void PartialClear()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters();

            var parserA = new Def.Parser();

            ExpectErrors(() => Def.Database.Clear());
        }
    }
}
