namespace DecTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Module : Base
    {
        public class TwoIntsDec : Dec.Dec
        {
            public int a = -1;
            public int b = -2;
        }

        [Test]
        public void DeAbstract([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""AbsA"" abstract=""true"">
                        <a>42</a>
                        <b>100</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""AbsB"" abstract=""true"">
                        <a>-42</a>
                        <b>-100</b>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""AbsA"" mode=""patch"" abstract=""false"" />
                    <TwoIntsDec decName=""ConcA"" parent=""AbsA"" />
                    <TwoIntsDec decName=""AbsB"" mode=""patch"" />
                    <TwoIntsDec decName=""AAB"" parent=""AbsB"" abstract=""true"" />
                    <TwoIntsDec decName=""ConcAAB"" parent=""AAB"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("AbsA"));
            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("ConcA"));
            Assert.IsNull(Dec.Database<TwoIntsDec>.Get("AbsB"));
            Assert.IsNull(Dec.Database<TwoIntsDec>.Get("AAB"));
            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("ConcAAB"));

            Assert.AreEqual(42, Dec.Database<TwoIntsDec>.Get("AbsA").a);
            Assert.AreEqual(42, Dec.Database<TwoIntsDec>.Get("ConcA").a);
            Assert.AreEqual(100, Dec.Database<TwoIntsDec>.Get("AbsA").b);
            Assert.AreEqual(100, Dec.Database<TwoIntsDec>.Get("ConcA").b);

            Assert.AreEqual(-42, Dec.Database<TwoIntsDec>.Get("ConcAAB").a);
            Assert.AreEqual(-100, Dec.Database<TwoIntsDec>.Get("ConcAAB").b);
        }

        [Test]
        public void InheritanceInsertion([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""Abstract"" abstract=""true"">
                        <a>42</a>
                        <b>100</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""Concrete"" parent=""Abstract"" />
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""Abstract"" mode=""patch"">
                        <a>-42</a>
                    </TwoIntsDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<TwoIntsDec>.Get("Abstract"));
            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("Concrete"));

            Assert.AreEqual(-42, Dec.Database<TwoIntsDec>.Get("Concrete").a);
            Assert.AreEqual(100, Dec.Database<TwoIntsDec>.Get("Concrete").b);
        }

        [Test]
        public void DuplicateModule([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""First"" />
                </Decs>");
            ExpectErrors(() => parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""Second"" />
                </Decs>"));
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("First"));
            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("Second"));
        }

        public enum Vis
        {
            Hidden,
            Create,
        }
        private string StrFromVis(Vis vis)
        {
            switch (vis)
            {
                case Vis.Hidden:
                    return "";

                case Vis.Create:
                    return "mode=\"create\"";

                default:
                    Assert.Fail();
                    return "";
            }
        }

        [Test]
        public void ModeCreatePass([Values] ParserMode mode, [Values] Vis vis)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""BaseOnly"" />
                    <TwoIntsDec decName=""BaseAbstract"" abstract=""true"" />
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""ModOnly"" {StrFromVis(vis)} />
                    <TwoIntsDec decName=""ModAbstract"" abstract=""true"" {StrFromVis(vis)} />
                    <TwoIntsDec decName=""ModFromBase"" parent=""BaseAbstract"" {StrFromVis(vis)} />
                    <TwoIntsDec decName=""ModFromMod"" parent=""ModAbstract"" {StrFromVis(vis)} />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("BaseOnly"));
            Assert.IsNull(Dec.Database<TwoIntsDec>.Get("BaseAbstract"));

            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("ModOnly"));
            Assert.IsNull(Dec.Database<TwoIntsDec>.Get("ModAbstract"));
            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("ModFromBase"));
            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("ModFromMod"));
        }

        [Test]
        public void ModeCreateFailBase([Values] ParserMode mode, [Values] Vis vis)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""BaseOnly"">
                        <a>42</a>
                        <b>100</b>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""BaseOnly"" {StrFromVis(vis)}>
                        <a>-42</a>
                    </TwoIntsDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("BaseOnly"));

            Assert.AreEqual(-42, Dec.Database<TwoIntsDec>.Get("BaseOnly").a);
            Assert.AreEqual(100, Dec.Database<TwoIntsDec>.Get("BaseOnly").b);
        }

        [Test]
        public void ModeCreateFailAbstract([Values] ParserMode mode, [Values] Vis vis)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""BaseAbstract"" abstract=""true"">
                        <a>42</a>
                        <b>100</b>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""BaseAbstract"" {StrFromVis(vis)}>
                        <a>-42</a>
                    </TwoIntsDec>

                    <TwoIntsDec decName=""ModFromBase"" parent=""BaseAbstract"" {StrFromVis(vis)} />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<TwoIntsDec>.Get("BaseAbstract"));
            Assert.IsNotNull(Dec.Database<TwoIntsDec>.Get("ModFromBase"));

            Assert.AreEqual(-42, Dec.Database<TwoIntsDec>.Get("ModFromBase").a);
            Assert.AreEqual(100, Dec.Database<TwoIntsDec>.Get("ModFromBase").b);
        }

        [Test]
        public void ModeReplacePass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""BaseOnly"">
                        <a>1</a>
                        <b>2</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" abstract=""true"">
                        <a>3</a>
                        <b>4</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteUntouched"" parent=""BaseAbstract"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteTouched"" parent=""BaseAbstract"">
                        <b>5</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" parent=""BaseAbstract"">
                        <b>6</b>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""BaseOnly"" mode=""replace"">
                        <a>7</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" mode=""replace"" abstract=""true"">
                        <a>8</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" mode=""replace"">
                    </TwoIntsDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec baseOnly = Dec.Database<TwoIntsDec>.Get("BaseOnly");
            Assert.IsNotNull(baseOnly);
            Assert.AreEqual(7, baseOnly.a);
            Assert.AreEqual(-2, baseOnly.b);

            TwoIntsDec baseAbstract = Dec.Database<TwoIntsDec>.Get("BaseAbstract");
            Assert.IsNull(baseAbstract);

            TwoIntsDec baseConcreteUntouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteUntouched");
            Assert.IsNotNull(baseConcreteUntouched);
            Assert.AreEqual(8, baseConcreteUntouched.a);
            Assert.AreEqual(-2, baseConcreteUntouched.b);

            TwoIntsDec baseConcreteTouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteTouched");
            Assert.IsNotNull(baseConcreteTouched);
            Assert.AreEqual(8, baseConcreteTouched.a);
            Assert.AreEqual(5, baseConcreteTouched.b);

            TwoIntsDec baseConcreteReplaceable = Dec.Database<TwoIntsDec>.Get("BaseConcreteReplaceable");
            Assert.IsNotNull(baseConcreteReplaceable);
            Assert.AreEqual(-1, baseConcreteReplaceable.a);
            Assert.AreEqual(-2, baseConcreteReplaceable.b);
        }

        [Test]
        public void ModeReplaceMissing([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""Forged"" mode=""replace"">
                        <a>1</a>
                        <b>2</b>
                    </TwoIntsDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            TwoIntsDec baseOnly = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNotNull(baseOnly);
            Assert.AreEqual(1, baseOnly.a);
            Assert.AreEqual(2, baseOnly.b);
        }
    }
}
