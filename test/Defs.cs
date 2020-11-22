namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Defs : Base
    {
        [Test]
	    public void TrivialParse([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"">
                    </StubDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void TrivialEmptyParse([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        [Test]
	    public void MissingDefType([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <NonexistentDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNull(Def.Database<StubDef>.Get("TestDefA"));
            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDefB"));
	    }

        [Test]
	    public void MissingDefName([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <StubDef />
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);
        }

        [Test]
	    public void InvalidDefName([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <StubDef defName=""1NumberPrefix"" />
                    <StubDef defName=""Contains Spaces"" />
                    <StubDef defName=""HasPunctuation!"" />
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNull(Def.Database<StubDef>.Get("1NumberPrefix"));
            Assert.IsNull(Def.Database<StubDef>.Get("Contains Spaces"));
            Assert.IsNull(Def.Database<StubDef>.Get("HasPunctuation!"));
	    }

        public class IntDef : Def.Def
        {
            public int value = 4;
        }

        [Test]
	    public void DuplicateField([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(IntDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>3</value>
                        <value>6</value>
                    </IntDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(6, result.value);
	    }

        [Test]
	    public void DuplicateDef([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(IntDef) } };

            var parser = new Def.Parser();
            ExpectErrors(() => parser.AddString(@"
                <Defs>
                    <IntDef defName=""TestDef"">
                        <value>10</value>
                    </IntDef>
                    <IntDef defName=""TestDef"">
                        <value>20</value>
                    </IntDef>
                </Defs>"));
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<IntDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(20, result.value);
	    }

        public class DeepParentDef : Def.Def
        {
            public int value = 4;
        }

        public class DeepChildDef : DeepParentDef
        {
            
        }

        [Test]
	    public void HierarchyDeepField([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DeepChildDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <DeepChildDef defName=""TestDef"">
                        <value>12</value>
                    </DeepChildDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            var result = Def.Database<DeepParentDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
	    }

        public class DupeParentDef : Def.Def
        {
            public int value = 4;
        }

        public class DupeChildDef : DupeParentDef
        {
            new public int value = 8;
        }

        [Test]
        public void UtilReflectionDuplicateField()
        {
            var def_utilreflection = GetDefAssembly().GetType("Def.UtilReflection");
            var getFieldsFromHierarchy = def_utilreflection.GetMethod("GetSerializableFieldsFromHierarchy", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            System.Reflection.FieldInfo[] fields = null;
            ExpectErrors(() => fields = (getFieldsFromHierarchy.Invoke(null, new[] { typeof(DupeChildDef) }) as IEnumerable<System.Reflection.FieldInfo>).ToArray());
            Assert.AreEqual(1, fields.Count(field => field.Name == "value"));
        }

        [Test]
	    public void HierarchyDuplicateField([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(DupeChildDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <DupeChildDef defName=""TestDef"">
                        <value>12</value>
                    </DupeChildDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true);

            var result = (DupeChildDef)Def.Database<DupeParentDef>.Get("TestDef");
            Assert.IsNotNull(result);

            Assert.AreEqual(12, result.value);
            Assert.AreEqual(4, ((DupeParentDef)result).value);
	    }

        [Test]
	    public void ExtraAttribute([Values] BehaviorMode mode)
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" invalidAttribute=""hello"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));
	    }

        public class StubBetaDef : Def.Def
        {

        }

        public class StubChildDef : StubDef
        {

        }

        [Test]
	    public void DebugPrint([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<StubDef>.Get("TestDef"));

            Assert.AreEqual(Def.Database<StubDef>.Get("TestDef").ToString(), "TestDef");
        }

        public class ErrorDef : Def.Def
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

        public class PostLoadErrorDef : Def.Def
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

        public class ErrorExceptionDef : Def.Def
        {
            public bool touched = false;

            public override void ConfigErrors(Action<string> report)
            {
                base.ConfigErrors(report);

                touched = true;

                throw new FormatException();
            }
        }

        public class PostLoadErrorExceptionDef : Def.Def
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
	    public void ConfigErrors([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(ErrorDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <ErrorDef defName=""TestDefA"" />
                    <ErrorDef defName=""TestDefB"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Def.Database<ErrorDef>.Get("TestDefA").touchedBefore);
            Assert.IsTrue(Def.Database<ErrorDef>.Get("TestDefA").touchedAfter);
            Assert.IsTrue(Def.Database<ErrorDef>.Get("TestDefB").touchedBefore);
            Assert.IsTrue(Def.Database<ErrorDef>.Get("TestDefB").touchedAfter);
        }

        [Test]
        public void PostLoadErrors([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PostLoadErrorDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <PostLoadErrorDef defName=""TestDefA"" />
                    <PostLoadErrorDef defName=""TestDefB"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Def.Database<PostLoadErrorDef>.Get("TestDefA").touchedBefore);
            Assert.IsTrue(Def.Database<PostLoadErrorDef>.Get("TestDefA").touchedAfter);
            Assert.IsTrue(Def.Database<PostLoadErrorDef>.Get("TestDefB").touchedBefore);
            Assert.IsTrue(Def.Database<PostLoadErrorDef>.Get("TestDefB").touchedAfter);
        }

        [Test]
        public void ConfigExceptionErrors([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ErrorExceptionDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <ErrorExceptionDef defName=""TestDefA"" />
                    <ErrorExceptionDef defName=""TestDefB"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Def.Database<ErrorExceptionDef>.Get("TestDefA").touched);
            Assert.IsTrue(Def.Database<ErrorExceptionDef>.Get("TestDefB").touched);
        }

        [Test]
        public void PostLoadExceptionErrors([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PostLoadErrorExceptionDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <PostLoadErrorExceptionDef defName=""TestDefA"" />
                    <PostLoadErrorExceptionDef defName=""TestDefB"" />
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            Assert.IsTrue(Def.Database<PostLoadErrorExceptionDef>.Get("TestDefA").touched);
            Assert.IsTrue(Def.Database<PostLoadErrorExceptionDef>.Get("TestDefB").touched);
        }

        public class PostLoadDef : Def.Def
        {
            public bool initted = false;

            public override void PostLoad(Action<string> report)
            {
                base.PostLoad(report);

                initted = true;
            }
        }

        [Test]
	    public void PostLoad([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(PostLoadDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <PostLoadDef defName=""TestDef"" />
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<PostLoadDef>.Get("TestDef"));
            Assert.IsTrue(Def.Database<PostLoadDef>.Get("TestDef").initted);
        }

        public class DefMemberDef : Def.Def
        {
            public Def.Def invalidReference;
        }

        [Test]
        public void DefMember([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(DefMemberDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <DefMemberDef defName=""TestDef"">
                        <invalidReference>TestDef</invalidReference>
                    </DefMemberDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectParseErrors: true);

            Assert.IsNotNull(Def.Database<DefMemberDef>.Get("TestDef"));
            Assert.IsNull(Def.Database<DefMemberDef>.Get("TestDef").invalidReference);
        }

        public class SelfReferentialDef : Def.Def
        {
            public SelfReferentialDef recursive;
        }

        [Test]
        public void SelfReferential([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SelfReferentialDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <SelfReferentialDef defName=""TestDef"">
                        <recursive>TestDef</recursive>
                    </SelfReferentialDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.IsNotNull(Def.Database<SelfReferentialDef>.Get("TestDef"));
            Assert.AreSame(Def.Database<SelfReferentialDef>.Get("TestDef"), Def.Database<SelfReferentialDef>.Get("TestDef").recursive);
        }

        public class LooseMatchDef : Def.Def
        {
            public string cat;
            public string snake_case;
            public string camelCase;
        }

        [Test]
        public void LooseMatchCapitalization([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LooseMatchDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <LooseMatchDef defName=""TestDef"">
                        <Cat>words</Cat>
                    </LooseMatchDef>
                </Defs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("cat"));

            DoBehavior(mode);
        }

        [Test]
        public void LooseMatchSnakeToCamel([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LooseMatchDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <LooseMatchDef defName=""TestDef"">
                        <snakeCase>words</snakeCase>
                    </LooseMatchDef>
                </Defs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("snake_case"));

            DoBehavior(mode);
        }

        [Test]
        public void LooseMatchCamelToSnake([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LooseMatchDef) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <LooseMatchDef defName=""TestDef"">
                        <camel_case>words</camel_case>
                    </LooseMatchDef>
                </Defs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("camelCase"));

            DoBehavior(mode);
        }

        [Test]
        public void ForbiddenField([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDef) } };

            var parser = new Def.Parser();

            // This is a little silly because, as of this writing, DefName is a property and we don't even support writing to properties.
            // So we're not really testing forbidden fields here. We're really just double-checking the fact that properties can't be written to.
            // But someday I'll probably support properties, and then this had better work.
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDef"">
                        <DefName>NotTestDef</DefName>
                    </StubDef>
                </Defs>");

            // Just in case I rename it back to lowercase, make sure we don't just get a spelling mismatch error here.
            ExpectErrors(() => parser.Finish(), err => !err.Contains("defName"));

            DoBehavior(mode);

            Assert.AreEqual("TestDef", Def.Database<StubDef>.Get("TestDef").DefName);
        }

        public class InternalBase
        {
            public int baseOnly;
        }

        public class InternalDerived : InternalBase
        {
            public int derivedOnly;
        }

        public class InternalInheritanceDef : Def.Def
        {
            public InternalBase value;
        }

        [Test]
        public void InternalInheritance([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(InternalInheritanceDef), typeof(InternalDerived) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <InternalInheritanceDef defName=""TestDef"">
                        <value class=""InternalDerived"">
                            <baseOnly>42</baseOnly>
                            <derivedOnly>100</derivedOnly>
                        </value>
                     </InternalInheritanceDef>
                </Defs>");
            parser.Finish();

            DoBehavior(mode);

            Assert.AreEqual(typeof(InternalDerived), Def.Database<InternalInheritanceDef>.Get("TestDef").value.GetType());

            Assert.AreEqual(42, Def.Database<InternalInheritanceDef>.Get("TestDef").value.baseOnly);
            Assert.AreEqual(100, ( (InternalDerived)Def.Database<InternalInheritanceDef>.Get("TestDef").value ).derivedOnly);
        }

        public class ConflictBase
        {
            public int conflict = 1;
        }

        public class ConflictDerived : ConflictBase
        {
            public new int conflict = 2;
        }

        public class ConflictInheritanceDef : Def.Def
        {
            public ConflictBase value;
        }

        [Test]
        public void ConflictInheritance([Values] BehaviorMode mode)
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ConflictInheritanceDef), typeof(ConflictDerived) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <ConflictInheritanceDef defName=""TestDef"">
                        <value class=""ConflictDerived"">
                            <conflict>42</conflict>
                        </value>
                     </ConflictInheritanceDef>
                </Defs>");
            ExpectErrors(() => parser.Finish());

            DoBehavior(mode, rewrite_expectWriteErrors: true, rewrite_expectParseErrors: true);

            // This behavior is absolutely not guaranteed, for the record.
            Assert.AreEqual(1, Def.Database<ConflictInheritanceDef>.Get("TestDef").value.conflict);
            Assert.AreEqual(42, ( (ConflictDerived)Def.Database<ConflictInheritanceDef>.Get("TestDef").value ).conflict);
        }
    }
}
