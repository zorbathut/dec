using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ModuleModes : Base
    {
        public class TwoIntsDec : Dec.Dec
        {
            public int a = -1;
            public int b = -2;
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

            TwoIntsDec forged = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNotNull(forged);
            Assert.AreEqual(1, forged.a);
            Assert.AreEqual(2, forged.b);
        }

        [Test]
        public void ModePatchPass([Values] ParserMode mode)
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
                    <TwoIntsDec decName=""BaseOnly"" mode=""patch"">
                        <a>7</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" mode=""patch"" abstract=""true"">
                        <a>8</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" mode=""patch"">
                    </TwoIntsDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec baseOnly = Dec.Database<TwoIntsDec>.Get("BaseOnly");
            Assert.IsNotNull(baseOnly);
            Assert.AreEqual(7, baseOnly.a);
            Assert.AreEqual(2, baseOnly.b);

            TwoIntsDec baseAbstract = Dec.Database<TwoIntsDec>.Get("BaseAbstract");
            Assert.IsNull(baseAbstract);

            TwoIntsDec baseConcreteUntouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteUntouched");
            Assert.IsNotNull(baseConcreteUntouched);
            Assert.AreEqual(8, baseConcreteUntouched.a);
            Assert.AreEqual(4, baseConcreteUntouched.b);

            TwoIntsDec baseConcreteTouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteTouched");
            Assert.IsNotNull(baseConcreteTouched);
            Assert.AreEqual(8, baseConcreteTouched.a);
            Assert.AreEqual(5, baseConcreteTouched.b);

            TwoIntsDec baseConcreteReplaceable = Dec.Database<TwoIntsDec>.Get("BaseConcreteReplaceable");
            Assert.IsNotNull(baseConcreteReplaceable);
            Assert.AreEqual(8, baseConcreteReplaceable.a);
            Assert.AreEqual(6, baseConcreteReplaceable.b);
        }

        [Test]
        public void ModePatchMissing([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""Forged"" mode=""patch"">
                        <a>1</a>
                        <b>2</b>
                    </TwoIntsDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            TwoIntsDec forged = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNotNull(forged);
            Assert.AreEqual(1, forged.a);
            Assert.AreEqual(2, forged.b);
        }

        [Test]
        public void ModeCreateOrReplacePass([Values] ParserMode mode)
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
                    <TwoIntsDec decName=""BaseOnly"" mode=""createOrReplace"">
                        <a>7</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" mode=""createOrReplace"" abstract=""true"">
                        <a>8</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" mode=""createOrReplace"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""Forged"" mode=""createOrReplace"">
                        <a>1</a>
                        <b>2</b>
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

            TwoIntsDec forged = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNotNull(forged);
            Assert.AreEqual(1, forged.a);
            Assert.AreEqual(2, forged.b);
        }

        [Test]
        public void ModeCreateOrPatchPass([Values] ParserMode mode)
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
                    <TwoIntsDec decName=""BaseOnly"" mode=""createOrPatch"">
                        <a>7</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" mode=""createOrPatch"" abstract=""true"">
                        <a>8</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" mode=""createOrPatch"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""Forged"" mode=""createOrPatch"">
                        <a>1</a>
                        <b>2</b>
                    </TwoIntsDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec baseOnly = Dec.Database<TwoIntsDec>.Get("BaseOnly");
            Assert.IsNotNull(baseOnly);
            Assert.AreEqual(7, baseOnly.a);
            Assert.AreEqual(2, baseOnly.b);

            TwoIntsDec baseAbstract = Dec.Database<TwoIntsDec>.Get("BaseAbstract");
            Assert.IsNull(baseAbstract);

            TwoIntsDec baseConcreteUntouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteUntouched");
            Assert.IsNotNull(baseConcreteUntouched);
            Assert.AreEqual(8, baseConcreteUntouched.a);
            Assert.AreEqual(4, baseConcreteUntouched.b);

            TwoIntsDec baseConcreteTouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteTouched");
            Assert.IsNotNull(baseConcreteTouched);
            Assert.AreEqual(8, baseConcreteTouched.a);
            Assert.AreEqual(5, baseConcreteTouched.b);

            TwoIntsDec baseConcreteReplaceable = Dec.Database<TwoIntsDec>.Get("BaseConcreteReplaceable");
            Assert.IsNotNull(baseConcreteReplaceable);
            Assert.AreEqual(8, baseConcreteReplaceable.a);
            Assert.AreEqual(6, baseConcreteReplaceable.b);

            TwoIntsDec forged = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNotNull(forged);
            Assert.AreEqual(1, forged.a);
            Assert.AreEqual(2, forged.b);
        }

        [Test]
        public void ModeCreateOrIgnorePass([Values] ParserMode mode)
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
                    <TwoIntsDec decName=""BaseOnly"" mode=""createOrIgnore"">
                        <a>7</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" mode=""createOrIgnore"" abstract=""true"">
                        <a>8</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" mode=""createOrIgnore"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""Forged"" mode=""createOrIgnore"">
                        <a>1</a>
                        <b>2</b>
                    </TwoIntsDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec baseOnly = Dec.Database<TwoIntsDec>.Get("BaseOnly");
            Assert.IsNotNull(baseOnly);
            Assert.AreEqual(1, baseOnly.a);
            Assert.AreEqual(2, baseOnly.b);

            TwoIntsDec baseAbstract = Dec.Database<TwoIntsDec>.Get("BaseAbstract");
            Assert.IsNull(baseAbstract);

            TwoIntsDec baseConcreteUntouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteUntouched");
            Assert.IsNotNull(baseConcreteUntouched);
            Assert.AreEqual(3, baseConcreteUntouched.a);
            Assert.AreEqual(4, baseConcreteUntouched.b);

            TwoIntsDec baseConcreteTouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteTouched");
            Assert.IsNotNull(baseConcreteTouched);
            Assert.AreEqual(3, baseConcreteTouched.a);
            Assert.AreEqual(5, baseConcreteTouched.b);

            TwoIntsDec baseConcreteReplaceable = Dec.Database<TwoIntsDec>.Get("BaseConcreteReplaceable");
            Assert.IsNotNull(baseConcreteReplaceable);
            Assert.AreEqual(3, baseConcreteReplaceable.a);
            Assert.AreEqual(6, baseConcreteReplaceable.b);

            TwoIntsDec forged = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNotNull(forged);
            Assert.AreEqual(1, forged.a);
            Assert.AreEqual(2, forged.b);
        }

        [Test]
        public void ModeDeletePass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""BaseDelete"">
                        <a>1</a>
                        <b>2</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAlive"">
                        <a>3</a>
                        <b>4</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstractPeace"" abstract=""true"">
                        <a>5</a>
                        <b>6</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcretePeace"" parent=""BaseAbstractPeace"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteEliminate"" parent=""BaseAbstractPeace"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstractMurder"" abstract=""true"">
                        <a>7</a>
                        <b>8</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstractSlaughter"" abstract=""true"">
                        <a>9</a>
                        <b>10</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteSlaughterDerived"" parent=""BaseAbstractSlaughter"">
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""BaseDelete"" mode=""delete"" />
                    <TwoIntsDec decName=""BaseConcreteEliminate"" mode=""delete"" />
                    <TwoIntsDec decName=""BaseAbstractMurder"" mode=""delete"" />
                    <TwoIntsDec decName=""BaseAbstractSlaughter"" mode=""delete"" />
                    <TwoIntsDec decName=""BaseConcreteSlaughterDerived"" mode=""delete"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec baseDelete = Dec.Database<TwoIntsDec>.Get("BaseDelete");
            Assert.IsNull(baseDelete);

            TwoIntsDec baseAlive = Dec.Database<TwoIntsDec>.Get("BaseAlive");
            Assert.IsNotNull(baseAlive);
            Assert.AreEqual(3, baseAlive.a);
            Assert.AreEqual(4, baseAlive.b);

            TwoIntsDec baseAbstractPeace = Dec.Database<TwoIntsDec>.Get("BaseAbstractPeace");
            Assert.IsNull(baseAbstractPeace);

            TwoIntsDec baseConcretePeace = Dec.Database<TwoIntsDec>.Get("BaseConcretePeace");
            Assert.IsNotNull(baseConcretePeace);
            Assert.AreEqual(5, baseConcretePeace.a);
            Assert.AreEqual(6, baseConcretePeace.b);

            TwoIntsDec baseConcreteEliminate = Dec.Database<TwoIntsDec>.Get("BaseConcreteEliminate");
            Assert.IsNull(baseConcreteEliminate);

            TwoIntsDec baseAbstractMurder = Dec.Database<TwoIntsDec>.Get("BaseAbstractMurder");
            Assert.IsNull(baseAbstractMurder);

            TwoIntsDec baseAbstractSlaughter = Dec.Database<TwoIntsDec>.Get("BaseAbstractSlaughter");
            Assert.IsNull(baseAbstractSlaughter);

            TwoIntsDec baseConcreteSlaughterDerived = Dec.Database<TwoIntsDec>.Get("BaseConcreteSlaughterDerived");
            Assert.IsNull(baseConcreteSlaughterDerived);
        }

        [Test]
        public void ModeDeleteParentFail([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""ParentAbstract"" abstract=""true"">
                        <a>10</a>
                        <b>20</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""ChildConcrete"" parent=""ParentAbstract"">
                        <a>30</a>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""ParentAbstract"" mode=""delete"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            TwoIntsDec childConcrete = Dec.Database<TwoIntsDec>.Get("ChildConcrete");
            Assert.IsNotNull(childConcrete);
            Assert.AreEqual(30, childConcrete.a);
            Assert.AreEqual(-2, childConcrete.b);
        }

        [Test]
        public void ModeDeleteNonexistentFail([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""Missing"" mode=""delete"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            TwoIntsDec missing = Dec.Database<TwoIntsDec>.Get("Missing");
            Assert.IsNull(missing);
        }

        [Test]
        public void ModeDeleteDoublePass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"" />
                </Decs>");
            parser.CreateModule("ModA").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"" mode=""delete"" />
                </Decs>");
            parser.CreateModule("ModB").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"" mode=""delete"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec deleteMe = Dec.Database<TwoIntsDec>.Get("DeleteMe");
            Assert.IsNull(deleteMe);
        }

        [Test]
        public void ModeDeleteAndPatchFail([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"">
                        <a>10</a>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("ModA").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"" mode=""delete"" />
                </Decs>");
            parser.CreateModule("ModB").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"" mode=""patch"">
                        <b>20</b>
                    </TwoIntsDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            TwoIntsDec deleteMe = Dec.Database<TwoIntsDec>.Get("DeleteMe");
            Assert.IsNotNull(deleteMe);
            Assert.AreEqual(-1, deleteMe.a);
            Assert.AreEqual(20, deleteMe.b);
        }

        [Test]
        public void ModeDeleteAndCreatePass([Values] ParserMode mode, [Values] bool includeCreate)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"">
                        <a>10</a>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("ModA").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"" mode=""delete"" />
                </Decs>");
            parser.CreateModule("ModB").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""DeleteMe"" {(includeCreate ? "mode=\"create\"" : "")}>
                        <b>20</b>
                    </TwoIntsDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec deleteMe = Dec.Database<TwoIntsDec>.Get("DeleteMe");
            Assert.IsNotNull(deleteMe);
            Assert.AreEqual(-1, deleteMe.a);
            Assert.AreEqual(20, deleteMe.b);
        }

        [Test]
        public void ModeReplaceIfExistsPass([Values] ParserMode mode)
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
                    <TwoIntsDec decName=""BaseOnly"" mode=""replaceIfExists"">
                        <a>7</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" mode=""replaceIfExists"" abstract=""true"">
                        <a>8</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" mode=""replaceIfExists"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""Forged"" mode=""replaceIfExists"">
                        <a>9</a>
                        <b>10</b>
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

            TwoIntsDec forged = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNull(forged);
        }

        [Test]
        public void ModePatchIfExistsPass([Values] ParserMode mode)
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
                    <TwoIntsDec decName=""BaseOnly"" mode=""patchIfExists"">
                        <a>7</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstract"" mode=""patchIfExists"" abstract=""true"">
                        <a>8</a>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteReplaceable"" mode=""patchIfExists"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""Forged"" mode=""patchIfExists"">
                        <a>9</a>
                        <b>10</b>
                    </TwoIntsDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec baseOnly = Dec.Database<TwoIntsDec>.Get("BaseOnly");
            Assert.IsNotNull(baseOnly);
            Assert.AreEqual(7, baseOnly.a);
            Assert.AreEqual(2, baseOnly.b);

            TwoIntsDec baseAbstract = Dec.Database<TwoIntsDec>.Get("BaseAbstract");
            Assert.IsNull(baseAbstract);

            TwoIntsDec baseConcreteUntouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteUntouched");
            Assert.IsNotNull(baseConcreteUntouched);
            Assert.AreEqual(8, baseConcreteUntouched.a);
            Assert.AreEqual(4, baseConcreteUntouched.b);

            TwoIntsDec baseConcreteTouched = Dec.Database<TwoIntsDec>.Get("BaseConcreteTouched");
            Assert.IsNotNull(baseConcreteTouched);
            Assert.AreEqual(8, baseConcreteTouched.a);
            Assert.AreEqual(5, baseConcreteTouched.b);

            TwoIntsDec baseConcreteReplaceable = Dec.Database<TwoIntsDec>.Get("BaseConcreteReplaceable");
            Assert.IsNotNull(baseConcreteReplaceable);
            Assert.AreEqual(8, baseConcreteReplaceable.a);
            Assert.AreEqual(6, baseConcreteReplaceable.b);

            TwoIntsDec forged = Dec.Database<TwoIntsDec>.Get("Forged");
            Assert.IsNull(forged);
        }

        [Test]
        public void ModeDeleteIfExistsPass([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""BaseDelete"">
                        <a>1</a>
                        <b>2</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAlive"">
                        <a>3</a>
                        <b>4</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstractPeace"" abstract=""true"">
                        <a>5</a>
                        <b>6</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcretePeace"" parent=""BaseAbstractPeace"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteEliminate"" parent=""BaseAbstractPeace"">
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstractMurder"" abstract=""true"">
                        <a>7</a>
                        <b>8</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseAbstractSlaughter"" abstract=""true"">
                        <a>9</a>
                        <b>10</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""BaseConcreteSlaughterDerived"" parent=""BaseAbstractSlaughter"">
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TwoIntsDec decName=""BaseDelete"" mode=""deleteIfExists"" />
                    <TwoIntsDec decName=""BaseConcreteEliminate"" mode=""deleteIfExists"" />
                    <TwoIntsDec decName=""BaseAbstractMurder"" mode=""deleteIfExists"" />
                    <TwoIntsDec decName=""BaseAbstractSlaughter"" mode=""deleteIfExists"" />
                    <TwoIntsDec decName=""BaseConcreteSlaughterDerived"" mode=""deleteIfExists"" />
                    <TwoIntsDec decName=""Missing"" mode=""deleteIfExists"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            TwoIntsDec baseDelete = Dec.Database<TwoIntsDec>.Get("BaseDelete");
            Assert.IsNull(baseDelete);

            TwoIntsDec baseAlive = Dec.Database<TwoIntsDec>.Get("BaseAlive");
            Assert.IsNotNull(baseAlive);
            Assert.AreEqual(3, baseAlive.a);
            Assert.AreEqual(4, baseAlive.b);

            TwoIntsDec baseAbstractPeace = Dec.Database<TwoIntsDec>.Get("BaseAbstractPeace");
            Assert.IsNull(baseAbstractPeace);

            TwoIntsDec baseConcretePeace = Dec.Database<TwoIntsDec>.Get("BaseConcretePeace");
            Assert.IsNotNull(baseConcretePeace);
            Assert.AreEqual(5, baseConcretePeace.a);
            Assert.AreEqual(6, baseConcretePeace.b);

            TwoIntsDec baseConcreteEliminate = Dec.Database<TwoIntsDec>.Get("BaseConcreteEliminate");
            Assert.IsNull(baseConcreteEliminate);

            TwoIntsDec baseAbstractMurder = Dec.Database<TwoIntsDec>.Get("BaseAbstractMurder");
            Assert.IsNull(baseAbstractMurder);

            TwoIntsDec baseAbstractSlaughter = Dec.Database<TwoIntsDec>.Get("BaseAbstractSlaughter");
            Assert.IsNull(baseAbstractSlaughter);

            TwoIntsDec baseConcreteSlaughterDerived = Dec.Database<TwoIntsDec>.Get("BaseConcreteSlaughterDerived");
            Assert.IsNull(baseConcreteSlaughterDerived);

            TwoIntsDec missing = Dec.Database<TwoIntsDec>.Get("Missing");
            Assert.IsNull(missing);
        }

        [Test]
        public void ModeDeleteIfExistsParentFail([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TwoIntsDec) } });

            var parser = new Dec.ParserModular();
            parser.CreateModule("Base").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""ParentAbstract"" abstract=""true"">
                        <a>10</a>
                        <b>20</b>
                    </TwoIntsDec>
                    <TwoIntsDec decName=""ChildConcrete"" parent=""ParentAbstract"">
                        <a>30</a>
                    </TwoIntsDec>
                </Decs>");
            parser.CreateModule("Mod").AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TwoIntsDec decName=""ParentAbstract"" mode=""deleteIfExists"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            TwoIntsDec childConcrete = Dec.Database<TwoIntsDec>.Get("ChildConcrete");
            Assert.IsNotNull(childConcrete);
            Assert.AreEqual(30, childConcrete.a);
            Assert.AreEqual(-2, childConcrete.b);
        }
    }
}
