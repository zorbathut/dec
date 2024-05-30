namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Decs : Base
    {
        [Test]
        public void TrivialParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"">
                    </StubDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
        }

        [Test]
        public void TrivialEmptyParse([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
        }

        [Test]
        public void MissingDecType([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <NonexistentDec decName=""TestDecA"" />
                    <StubDec decName=""TestDecB"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<StubDec>.Get("TestDecA"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDecB"));
        }

        [Test]
        public void MissingDecName([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);
        }

        [Test]
        public void InvalidDecName([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""1NumberPrefix"" />
                    <StubDec decName=""Contains Spaces"" />
                    <StubDec decName=""HasPunctuation!"" />
                    <StubDec decName=""&quot;Quotes&quot;"" />
                    <StubDec decName=""ActuallyAValidDecName"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<StubDec>.Get("1NumberPrefix"));
            Assert.IsNull(Dec.Database<StubDec>.Get("Contains Spaces"));
            Assert.IsNull(Dec.Database<StubDec>.Get("HasPunctuation!"));
            Assert.IsNull(Dec.Database<StubDec>.Get("\"Quotes\""));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("ActuallyAValidDecName"));
        }

        public class IntDec : Dec.Dec
        {
            public int value = 4;
        }

        [Test]
        public void DuplicateField([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>3</value>
                        <value>6</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(6, result.value);
        }

        [Test]
        public void DuplicateDec([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(IntDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntDec decName=""TestDec"">
                        <value>10</value>
                    </IntDec>
                    <IntDec decName=""TestDec"">
                        <value>20</value>
                    </IntDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, errorValidator: err => err.Contains("IntDec:TestDec"));

            var result = Dec.Database<IntDec>.Get("TestDec");
            Assert.IsNotNull(result);

            // this can be either 10 or 20, but shouldn't change between runs, only between versions
            Assert.True(result.value == 10);
        }

        public class DeepParentDec : Dec.Dec
        {
            public int value = 4;
        }

        public class DeepChildDec : DeepParentDec
        {
            
        }

        [Test]
        public void HierarchyDeepField([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DeepChildDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DeepChildDec decName=""TestDec"">
                        <value>12</value>
                    </DeepChildDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<DeepParentDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
        }

        public class DupeParentDec : Dec.Dec
        {
            public int value = 4;
        }

        public class DupeChildDec : DupeParentDec
        {
            new public int value = 8;
        }

        [Test]
        public void UtilReflectionDuplicateField()
        {
            var dec_utilreflection = GetDecAssembly().GetType("Dec.UtilReflection");
            var getFieldsFromHierarchy = dec_utilreflection.GetMethod("GetSerializableFieldsFromHierarchy", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            System.Reflection.FieldInfo[] fields = null;
            ExpectErrors(() => fields = (getFieldsFromHierarchy.Invoke(null, new[] { typeof(DupeChildDec) }) as IEnumerable<System.Reflection.FieldInfo>).ToArray());
            Assert.AreEqual(1, fields.Count(field => field.Name == "value"));
        }

        [Test]
        public void HierarchyDuplicateField([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DupeChildDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DupeChildDec decName=""TestDec"">
                        <value>12</value>
                    </DupeChildDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true, validation_expectWriteErrors: true);

            var result = (DupeChildDec)Dec.Database<DupeParentDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
            Assert.AreEqual(4, ((DupeParentDec)result).value);
        }

        [Test]
        public void ExtraAttribute([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" invalidAttribute=""hello"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
        }

        public class StubBetaDec : Dec.Dec
        {

        }

        public class StubChildDec : StubDec
        {

        }

        [Test]
        public void DebugPrint([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));

            Assert.AreEqual("[StubDec:TestDec]", Dec.Database<StubDec>.Get("TestDec").ToString());
        }

        public class ErrorDec : Dec.Dec
        {
            public bool touchedBefore = false;
            public bool touchedAfter = false;

            public override void ConfigErrors(Action<string> report)
            {
                base.ConfigErrors(report);

                touchedBefore = true;

                report("I am never valid");

                touchedAfter = true;
            }
        }

        public class PostLoadErrorDec : Dec.Dec
        {
            public bool touchedBefore = false;
            public bool touchedAfter = false;

            public override void PostLoad(Action<string> report)
            {
                base.PostLoad(report);

                touchedBefore = true;

                report("I am never valid, at a weird time");

                touchedAfter = true;
            }
        }

        public class ErrorExceptionDec : Dec.Dec
        {
            public bool touched = false;

            public override void ConfigErrors(Action<string> report)
            {
                base.ConfigErrors(report);

                touched = true;

                throw new FormatException();
            }
        }

        public class PostLoadErrorExceptionDec : Dec.Dec
        {
            public bool touched = false;

            public override void PostLoad(Action<string> report)
            {
                base.PostLoad(report);

                touched = true;

                throw new FormatException();
            }
        }

        [Test]
        public void ConfigErrors([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ErrorDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ErrorDec decName=""TestDecA"" />
                    <ErrorDec decName=""TestDecB"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Dec.Database<ErrorDec>.Get("TestDecA").touchedBefore);
            Assert.IsTrue(Dec.Database<ErrorDec>.Get("TestDecA").touchedAfter);
            Assert.IsTrue(Dec.Database<ErrorDec>.Get("TestDecB").touchedBefore);
            Assert.IsTrue(Dec.Database<ErrorDec>.Get("TestDecB").touchedAfter);
        }

        [Test]
        public void PostLoadErrors([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PostLoadErrorDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PostLoadErrorDec decName=""TestDecA"" />
                    <PostLoadErrorDec decName=""TestDecB"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Dec.Database<PostLoadErrorDec>.Get("TestDecA").touchedBefore);
            Assert.IsTrue(Dec.Database<PostLoadErrorDec>.Get("TestDecA").touchedAfter);
            Assert.IsTrue(Dec.Database<PostLoadErrorDec>.Get("TestDecB").touchedBefore);
            Assert.IsTrue(Dec.Database<PostLoadErrorDec>.Get("TestDecB").touchedAfter);
        }

        [Test]
        public void ConfigExceptionErrors([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ErrorExceptionDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ErrorExceptionDec decName=""TestDecA"" />
                    <ErrorExceptionDec decName=""TestDecB"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Dec.Database<ErrorExceptionDec>.Get("TestDecA").touched);
            Assert.IsTrue(Dec.Database<ErrorExceptionDec>.Get("TestDecB").touched);
        }

        [Test]
        public void PostLoadExceptionErrors([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PostLoadErrorExceptionDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PostLoadErrorExceptionDec decName=""TestDecA"" />
                    <PostLoadErrorExceptionDec decName=""TestDecB"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Dec.Database<PostLoadErrorExceptionDec>.Get("TestDecA").touched);
            Assert.IsTrue(Dec.Database<PostLoadErrorExceptionDec>.Get("TestDecB").touched);
        }

        public class PostLoadDec : Dec.Dec
        {
            public bool initted = false;

            public override void PostLoad(Action<string> report)
            {
                base.PostLoad(report);

                initted = true;
            }
        }

        [Test]
        public void PostLoad([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PostLoadDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PostLoadDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<PostLoadDec>.Get("TestDec"));
            Assert.IsTrue(Dec.Database<PostLoadDec>.Get("TestDec").initted);
        }

        public class DecMemberDec : Dec.Dec
        {
            public Dec.Dec invalidReference;
        }

        [Test]
        public void DecMember([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DecMemberDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <DecMemberDec decName=""TestDec"">
                        <invalidReference>TestDec</invalidReference>
                    </DecMemberDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<DecMemberDec>.Get("TestDec"));
            Assert.IsNull(Dec.Database<DecMemberDec>.Get("TestDec").invalidReference);
        }

        public class SelfReferentialDec : Dec.Dec
        {
            public SelfReferentialDec recursive;
        }

        [Test]
        public void SelfReferential([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SelfReferentialDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SelfReferentialDec decName=""TestDec"">
                        <recursive>TestDec</recursive>
                    </SelfReferentialDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<SelfReferentialDec>.Get("TestDec"));
            Assert.AreSame(Dec.Database<SelfReferentialDec>.Get("TestDec"), Dec.Database<SelfReferentialDec>.Get("TestDec").recursive);
        }

        public class LooseMatchDec : Dec.Dec
        {
            public string cat;
            public string snake_case;
            public string camelCase;
        }

        [Test]
        public void LooseMatchCapitalization([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LooseMatchDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LooseMatchDec decName=""TestDec"">
                        <Cat>words</Cat>
                    </LooseMatchDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), errorValidator: err => err.Contains("cat"));

            DoParserTests(mode);
        }

        [Test]
        public void LooseMatchSnakeToCamel([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LooseMatchDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LooseMatchDec decName=""TestDec"">
                        <snakeCase>words</snakeCase>
                    </LooseMatchDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), errorValidator: err => err.Contains("snake_case"));

            DoParserTests(mode);
        }

        [Test]
        public void LooseMatchCamelToSnake([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LooseMatchDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LooseMatchDec decName=""TestDec"">
                        <camel_case>words</camel_case>
                    </LooseMatchDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), errorValidator: err => err.Contains("camelCase"));

            DoParserTests(mode);
        }

        [Test]
        public void ForbiddenField([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) } });

            var parser = new Dec.Parser();

            // This is a little silly because, as of this writing, DecName is a property and we don't even support writing to properties.
            // So we're not really testing forbidden fields here. We're really just double-checking the fact that properties can't be written to.
            // But someday I'll probably support properties, and then this had better work.
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"">
                        <DecName>NotTestDec</DecName>
                    </StubDec>
                </Decs>");

            // Just in case I rename it back to lowercase, make sure we don't just get a spelling mismatch error here.
            ExpectErrors(() => parser.Finish(), errorValidator: err => !err.Contains("decName"));

            DoParserTests(mode);

            Assert.AreEqual("TestDec", Dec.Database<StubDec>.Get("TestDec").DecName);
        }

        public class InternalBase
        {
            public int baseOnly;
        }

        public class InternalDerived : InternalBase
        {
            public int derivedOnly;
        }

        public class InternalInheritanceDec : Dec.Dec
        {
            public InternalBase value;
        }

        [Test]
        public void InternalInheritance([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(InternalInheritanceDec), typeof(InternalDerived) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <InternalInheritanceDec decName=""TestDec"">
                        <value class=""InternalDerived"">
                            <baseOnly>42</baseOnly>
                            <derivedOnly>100</derivedOnly>
                        </value>
                     </InternalInheritanceDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(typeof(InternalDerived), Dec.Database<InternalInheritanceDec>.Get("TestDec").value.GetType());

            Assert.AreEqual(42, Dec.Database<InternalInheritanceDec>.Get("TestDec").value.baseOnly);
            Assert.AreEqual(100, ((InternalDerived)Dec.Database<InternalInheritanceDec>.Get("TestDec").value).derivedOnly);
        }

        public class ConflictBase
        {
            public int conflict = 1;
        }

        public class ConflictDerived : ConflictBase
        {
            public new int conflict = 2;
        }

        public class ConflictInheritanceDec : Dec.Dec
        {
            public ConflictBase value;
        }

        [Test]
        public void ConflictInheritance([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConflictInheritanceDec), typeof(ConflictDerived) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ConflictInheritanceDec decName=""TestDec"">
                        <value class=""ConflictDerived"">
                            <conflict>42</conflict>
                        </value>
                     </ConflictInheritanceDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true, validation_expectWriteErrors: true);

            // This behavior is absolutely not guaranteed, for the record.
            Assert.AreEqual(1, Dec.Database<ConflictInheritanceDec>.Get("TestDec").value.conflict);
            Assert.AreEqual(42, ((ConflictDerived)Dec.Database<ConflictInheritanceDec>.Get("TestDec").value).conflict);
        }

        public class WeirdList : List<int> { }
        public class WeirdDictionary : Dictionary<string, int> { }

        public class ContainerInheritanceDec : Dec.Dec
        {
            public WeirdList list;
            public WeirdDictionary dict;
        }

        [Test]
        public void ListInheritance([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ContainerInheritanceDec), typeof(WeirdList), typeof(WeirdDictionary) } });

            // Currently providing absolutely no guarantees for how these weird things parse, only that the types will be properly preserved.
            // Also: Don't do this, yo.
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ContainerInheritanceDec decName=""TestDec"">
                        <list class=""WeirdList"" />
                        <dict class=""WeirdDictionary"" />
                     </ContainerInheritanceDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(typeof(WeirdList), Dec.Database<ContainerInheritanceDec>.Get("TestDec").list.GetType());
            Assert.AreEqual(typeof(WeirdDictionary), Dec.Database<ContainerInheritanceDec>.Get("TestDec").dict.GetType());
        }

        public abstract class AbstractDec : Dec.Dec { }
        public class ConcreteDec : AbstractDec { }

        [Test]
        public void Abstract([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(AbstractDec), typeof(ConcreteDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <AbstractDec decName=""Abstract"" />
                    <ConcreteDec decName=""Concrete"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<AbstractDec>.Get("Abstract"));
            Assert.IsNotNull(Dec.Database<AbstractDec>.Get("Concrete"));
        }

        public class RawMemberDec : Dec.Dec
        {
            public Dec.Dec member;
        }

        public class AbstractMemberDec : Dec.Dec
        {
            public TrueAbstractDec member;
        }

        [Dec.Abstract] public abstract class TrueAbstractDec : Dec.Dec { }
        public class TrueConcreteDec : TrueAbstractDec { }

        [Test]
        public void RawMember([Values] ParserMode mode, [Values] bool classSpecified)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RawMemberDec), typeof(TrueConcreteDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <RawMemberDec decName=""Missing"">
                        <member {(classSpecified ? "class=\"TrueConcreteDec\"" : "")}>Concrete</member>
                    </RawMemberDec>
                    <TrueConcreteDec decName=""Concrete"" />
                </Decs>");
            if (!classSpecified)
            {
                ExpectErrors(() => parser.Finish());
            }
            else
            {
                parser.Finish();
            }

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<RawMemberDec>.Get("Missing"));
            Assert.IsNotNull(Dec.Database<TrueConcreteDec>.Get("Concrete"));
            if (classSpecified)
            {
                Assert.AreSame(Dec.Database<RawMemberDec>.Get("Missing").member, Dec.Database<TrueConcreteDec>.Get("Concrete"));
            }
            else
            {
                Assert.IsNull(Dec.Database<RawMemberDec>.Get("Missing").member);
            }
        }

        [Test]
        public void AbstractMember([Values] ParserMode mode, [Values] bool classSpecified)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(AbstractMemberDec), typeof(TrueConcreteDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <AbstractMemberDec decName=""Missing"">
                        <member {( classSpecified ? "class=\"TrueConcreteDec\"" : "" )}>Concrete</member>
                    </AbstractMemberDec>
                    <TrueConcreteDec decName=""Concrete"" />
                </Decs>");
            if (!classSpecified)
            {
                ExpectErrors(() => parser.Finish());
            }
            else
            {
                parser.Finish();
            }

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<AbstractMemberDec>.Get("Missing"));
            Assert.IsNotNull(Dec.Database<TrueConcreteDec>.Get("Concrete"));
            if (classSpecified)
            {
                Assert.AreSame(Dec.Database<AbstractMemberDec>.Get("Missing").member, Dec.Database<TrueConcreteDec>.Get("Concrete"));
            }
            else
            {
                Assert.IsNull(Dec.Database<AbstractMemberDec>.Get("Missing").member);
            }
        }

        public class ConstructorPrivateDec : Dec.Dec
        {
            private ConstructorPrivateDec() { }
        }

        [Test]
        public void ConstructorPrivate([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConstructorPrivateDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ConstructorPrivateDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<ConstructorPrivateDec>.Get("TestDec"));
        }

        public class ConstructorParameterDec : Dec.Dec
        {
            public ConstructorParameterDec(int x) { }
        }

        [Test]
        public void ConstructorParameter([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConstructorParameterDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ConstructorParameterDec decName=""TestDec"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            Assert.IsNull(Dec.Database<ConstructorParameterDec>.Get("TestDec"));
        }

        [Test]
        public void TooManyAttributes([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDec"" horse=""horse"" />
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);
        }
    }
}
