using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class DecInheritance : Base
    {
        public class SimpleDec : Dec.Dec
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
        public void Simple([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDec>

                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(3, result.defaulted);
            Assert.AreEqual(60, result.overridden);
            Assert.AreEqual(12, result.subObject.defaulted);
            Assert.AreEqual(90, result.subObject.overridden);

            Assert.IsNull(Dec.Database<SimpleDec>.Get("Base"));
        }

        [Test]
        public void List([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"">
                        <list>
                          <li>2</li>
                          <li>4</li>
                        </list>
                    </SimpleDec>

                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <list>
                          <li>50</li>
                        </list>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(1, result.list.Count);
            Assert.AreEqual(50, result.list[0]);

            Assert.IsNull(Dec.Database<SimpleDec>.Get("Base"));
        }

        [Test]
        public void FromNonAbstract([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDec>

                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var bas = Dec.Database<SimpleDec>.Get("Base");
            Assert.AreEqual(3, bas.defaulted);
            Assert.AreEqual(42, bas.overridden);
            Assert.AreEqual(12, bas.subObject.defaulted);
            Assert.AreEqual(80, bas.subObject.overridden);

            var thing = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(3, thing.defaulted);
            Assert.AreEqual(60, thing.overridden);
            Assert.AreEqual(12, thing.subObject.defaulted);
            Assert.AreEqual(90, thing.subObject.overridden);
        }

        [Test]
        public void MissingConcrete([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"">
                        <defaulted>3</defaulted>
                        <overridden>42</overridden>
                        <subObject>
                            <defaulted>12</defaulted>
                            <overridden>80</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(0, Dec.Database<SimpleDec>.Count);
        }

        [Test]
        public void MissingBase([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Thing"" parent=""Base"">
                        <overridden>60</overridden>
                        <subObject>
                            <overridden>90</overridden>
                        </subObject>
                    </SimpleDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("Base") && (err.Contains("no such dec exists") || err.Contains("does not exist")));

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("Thing");
            Assert.AreEqual(60, result.overridden);
            Assert.AreEqual(90, result.subObject.overridden);
        }

        [Test]
        public void MissingGrandparent([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Branch"" parent=""Missing"" abstract=""true"">
                        <defaulted>1</defaulted>
                    </SimpleDec>
                    <SimpleDec decName=""Leaf"" parent=""Branch"">
                        <overridden>2</overridden>
                    </SimpleDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("Branch") && err.Contains("Missing"));

            DoParserTests(mode);

            var result = Dec.Database<SimpleDec>.Get("Leaf");
            Assert.AreEqual(1, result.defaulted);
            Assert.AreEqual(2, result.overridden);
        }

        [Test]
        public void MissingParentSharedByChildren([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" parent=""Missing"" abstract=""true"">
                        <defaulted>1</defaulted>
                    </SimpleDec>
                    <SimpleDec decName=""ChildA"" parent=""Base"" />
                    <SimpleDec decName=""ChildB"" parent=""Base"" />
                    <SimpleDec decName=""ChildC"" parent=""Base"" />
                </Decs>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("Base") && err.Contains("Missing");
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("ChildA").defaulted);
            Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("ChildB").defaulted);
            Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("ChildC").defaulted);
        }

        [Test]
        public void AbstractOnlyParentProblems([Values] bool cycle, [Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var problem = cycle ?
                @"<SimpleDec decName=""LoopA"" parent=""LoopB"" abstract=""true"" />
                  <SimpleDec decName=""LoopB"" parent=""LoopA"" abstract=""true"" />" :
                @"<SimpleDec decName=""LoopA"" parent=""Missing"" abstract=""true"" />";

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    {problem}
                    <SimpleDec decName=""Unrelated"" />
                </Decs>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("LoopA") || err.Contains("LoopB");
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<SimpleDec>.Get("Unrelated"));
        }

        [Test]
        public void ParentCycle([Values] bool selfParent, [Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var cycle = selfParent ?
                @"<SimpleDec decName=""LoopA"" parent=""LoopA""><defaulted>1</defaulted></SimpleDec>" :
                @"<SimpleDec decName=""LoopA"" parent=""LoopB""><defaulted>1</defaulted></SimpleDec>
                  <SimpleDec decName=""LoopB"" parent=""LoopA""><doubleOverridden>3</doubleOverridden></SimpleDec>";

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    {cycle}
                    <SimpleDec decName=""Leaf"" parent=""LoopA"">
                        <overridden>2</overridden>
                    </SimpleDec>
                </Decs>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("LoopA");
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            // Every dec reaching the cycle inherits each of its members once.
            var expectedDoubleOverridden = selfParent ? 0 : 3;

            var loopA = Dec.Database<SimpleDec>.Get("LoopA");
            Assert.AreEqual(1, loopA.defaulted);
            Assert.AreEqual(expectedDoubleOverridden, loopA.doubleOverridden);

            if (!selfParent)
            {
                var loopB = Dec.Database<SimpleDec>.Get("LoopB");
                Assert.AreEqual(1, loopB.defaulted);
                Assert.AreEqual(3, loopB.doubleOverridden);
            }

            var leaf = Dec.Database<SimpleDec>.Get("Leaf");
            Assert.AreEqual(1, leaf.defaulted);
            Assert.AreEqual(2, leaf.overridden);
            Assert.AreEqual(expectedDoubleOverridden, leaf.doubleOverridden);
        }

        [Test]
        public void AbstractOverride([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Base"" abstract=""true"" />
                    <SimpleDec decName=""Base"" abstract=""true"" />
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("defined twice"));

            DoParserTests(mode);

            Assert.AreEqual(0, Dec.Database<SimpleDec>.Count);
        }

        [Test]
        public void AbstractNonabstractClash([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Before"" abstract=""true"">
                        <overridden>10</overridden>
                     </SimpleDec>
                    <SimpleDec decName=""Before"">
                        <overridden>20</overridden>
                     </SimpleDec>
                    <SimpleDec decName=""After"">
                        <overridden>30</overridden>
                     </SimpleDec>
                    <SimpleDec decName=""After"" abstract=""true"">
                        <overridden>40</overridden>
                     </SimpleDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("defined twice"));

            DoParserTests(mode);

            Assert.AreEqual(20, Dec.Database<SimpleDec>.Get("Before").overridden);
            Assert.AreEqual(30, Dec.Database<SimpleDec>.Get("After").overridden);
        }

        [Test]
        public void BadAbstractTag([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""Obj"" abstract=""cheese"">
                        <overridden>10</overridden>
                     </SimpleDec>
                </Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("abstract attribute"));

            DoParserTests(mode);

            Assert.AreEqual(10, Dec.Database<SimpleDec>.Get("Obj").overridden);
        }

        [Test]
        public void DoubleInheritance([Values] bool trunkAbstract, [Values] bool branchAbstract, [Values] bool leafAbstract, [Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <SimpleDec decName=""Trunk"" {(trunkAbstract ? "abstract=\"true\"" : "")}>
                        <defaulted>1</defaulted>
                        <overridden>2</overridden>
                        <doubleOverridden>3</doubleOverridden>
                     </SimpleDec>
                    <SimpleDec decName=""Branch"" parent=""Trunk"" {(branchAbstract ? "abstract=\"true\"" : "")}>
                        <overridden>20</overridden>
                        <doubleOverridden>30</doubleOverridden>
                     </SimpleDec>
                    <SimpleDec decName=""Leaf"" parent=""Branch"" {(leafAbstract ? "abstract=\"true\"" : "")}>
                        <doubleOverridden>300</doubleOverridden>
                     </SimpleDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            if (trunkAbstract)
            {
                Assert.IsNull(Dec.Database<SimpleDec>.Get("Trunk"));
            }
            else
            {
                Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("Trunk").defaulted);
                Assert.AreEqual(2, Dec.Database<SimpleDec>.Get("Trunk").overridden);
                Assert.AreEqual(3, Dec.Database<SimpleDec>.Get("Trunk").doubleOverridden);
            }

            if (branchAbstract)
            {
                Assert.IsNull(Dec.Database<SimpleDec>.Get("Branch"));
            }
            else
            {
                Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("Branch").defaulted);
                Assert.AreEqual(20, Dec.Database<SimpleDec>.Get("Branch").overridden);
                Assert.AreEqual(30, Dec.Database<SimpleDec>.Get("Branch").doubleOverridden);
            }

            if (leafAbstract)
            {
                Assert.IsNull(Dec.Database<SimpleDec>.Get("Leaf"));
            }
            else
            {
                Assert.AreEqual(1, Dec.Database<SimpleDec>.Get("Leaf").defaulted);
                Assert.AreEqual(20, Dec.Database<SimpleDec>.Get("Leaf").overridden);
                Assert.AreEqual(300, Dec.Database<SimpleDec>.Get("Leaf").doubleOverridden);
            }
        }

        [Test]
        public void ConcreteAncestorsAppend([Values] bool rootAbstract, [Values] bool reversed, [Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });

            var decs = new List<string>
            {
                $@"<SimpleDec decName=""Root"" {(rootAbstract ? "abstract=\"true\"" : "")}><list mode=""append""><li>1</li></list></SimpleDec>",
                @"<SimpleDec decName=""Trunk"" parent=""Root""><list mode=""append""><li>2</li></list></SimpleDec>",
                @"<SimpleDec decName=""Branch"" parent=""Trunk""><list mode=""append""><li>3</li></list></SimpleDec>",
                @"<SimpleDec decName=""Leaf"" parent=""Branch""><list mode=""append""><li>4</li></list></SimpleDec>",
            };
            if (reversed)
            {
                decs.Reverse();
            }

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $"<Decs>{string.Join("", decs)}</Decs>");
            parser.Finish();

            DoParserTests(mode);

            if (rootAbstract)
            {
                Assert.IsNull(Dec.Database<SimpleDec>.Get("Root"));
            }
            else
            {
                CollectionAssert.AreEqual(new[] { 1 }, Dec.Database<SimpleDec>.Get("Root").list);
            }
            CollectionAssert.AreEqual(new[] { 1, 2 }, Dec.Database<SimpleDec>.Get("Trunk").list);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Dec.Database<SimpleDec>.Get("Branch").list);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, Dec.Database<SimpleDec>.Get("Leaf").list);
        }

        public class LayerObj
        {
            public int x;
            public int y;
            public List<int> list;
        }

        public class LayerBase
        {
            public int a;
        }

        public class LayerDerived : LayerBase
        {
            public int b;
        }

        public class LayerDerivedAlt : LayerBase
        {
            public int c;
        }

        public interface ILayer { }

        public class LayerImpl : ILayer
        {
            public int a;
        }

        public struct LayerStruct
        {
            public int a;
            public int b;
            public int c;
        }

        public class LayerList : List<int> { }

        public class LayerListAlt : List<int> { }

        public class LayerDict : Dictionary<int, int> { }

        public class LayerListDerived : LayerList { }

        public class LayerDec : Dec.Dec
        {
            public int value;
            public LayerObj obj;
            public LayerObj objDefault = new LayerObj { x = 5 };
            public LayerBase poly;
            public ILayer iface;
            public List<int> list;
            public int[] array;
            public Stack<int> stack;
            public Queue<int> queue;
            public HashSet<int> set;
            public byte[] bytes;
            public List<int> listDefault = new List<int> { 9 };
            public Dictionary<int, int> dictDefault = new Dictionary<int, int> { { 9, 9 } };
            public List<int> listDerivedDefault = new LayerListDerived { 9 };
        }

        public class LayerPolyDec : Dec.Dec
        {
            public IEnumerable<int> enumerable;
            public object anything;
            public object value;
            public List<int> list;
        }

        public class LayerNullableDec : Dec.Dec
        {
            public LayerStruct? nullableStruct = new LayerStruct { a = 1 };
        }

        // Builds a chain of abstract layers ending in a concrete dec named Leaf, each layer's body given in order from root to leaf.
        private Dec.Parser LayerParser(params string[] bodies)
        {
            return LayerParser<LayerDec>(bodies);
        }

        private Dec.Parser LayerParser<T>(params string[] bodies) where T : Dec.Dec
        {
            Dec.Config.UsingNamespaces = new string[] { "DecTest.DecInheritance" };
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(T) } });

            var decs = new System.Text.StringBuilder();
            for (int i = 0; i < bodies.Length; ++i)
            {
                var name = i == bodies.Length - 1 ? "Leaf" : $"Layer{i}";
                var abstrct = i == bodies.Length - 1 ? "" : @"abstract=""true""";
                var parent = i == 0 ? "" : $@"parent=""Layer{i - 1}""";
                decs.Append($@"<{typeof(T).Name} decName=""{name}"" {abstrct} {parent}>{bodies[i]}</{typeof(T).Name}>");
            }

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $"<Decs>{decs}</Decs>");
            return parser;
        }

        private static LayerDec Leaf()
        {
            return Dec.Database<LayerDec>.Get("Leaf");
        }

        [Test]
        public void LayerContainersReplaceThenAppend([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<list><li>1</li></list><array><li>1</li></array><stack><li>1</li></stack><queue><li>1</li></queue>",
                "<list><li>2</li></list><array><li>2</li></array><stack><li>2</li></stack><queue><li>2</li></queue>",
                @"<list mode=""append""><li>3</li></list><array mode=""append""><li>3</li></array><stack mode=""append""><li>3</li></stack><queue mode=""append""><li>3</li></queue>");
            parser.Finish();

            DoParserTests(mode);

            CollectionAssert.AreEqual(new[] { 2, 3 }, Leaf().list);
            CollectionAssert.AreEqual(new[] { 2, 3 }, Leaf().array);
            CollectionAssert.AreEqual(new[] { 3, 2 }, Leaf().stack);
            CollectionAssert.AreEqual(new[] { 2, 3 }, Leaf().queue);
        }

        [Test]
        public void LayerNullThenObject([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<obj><x>1</x></obj>",
                @"<obj null=""true"" />",
                "<obj><y>2</y></obj>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(0, Leaf().obj.x);
            Assert.AreEqual(2, Leaf().obj.y);
        }

        [Test]
        public void LayerNullLast([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<obj><x>1</x></obj>",
                @"<obj null=""true"" />");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNull(Leaf().obj);
        }

        [Test]
        public void LayerNullThenObjectWithDefault([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<objDefault><y>1</y></objDefault>",
                @"<objDefault null=""true"" />",
                "<objDefault><list><li>3</li></list></objDefault>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(0, Leaf().objDefault.x);
            Assert.AreEqual(0, Leaf().objDefault.y);
            CollectionAssert.AreEqual(new[] { 3 }, Leaf().objDefault.list);
        }

        [Test]
        public void LayerNullThenNullableStruct([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = LayerParser<LayerNullableDec>(
                "<nullableStruct><b>2</b></nullableStruct>",
                @"<nullableStruct null=""true"" />",
                "<nullableStruct><c>3</c></nullableStruct>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(new LayerStruct { c = 3 }, Dec.Database<LayerNullableDec>.Get("Leaf").nullableStruct);
        }

        [Test]
        public void LayerClassThenNone([Values] ParserMode mode)
        {
            var parser = LayerParser(
                @"<poly class=""LayerDerived""><a>1</a><b>2</b></poly>",
                "<poly><a>3</a></poly>");
            parser.Finish();

            DoParserTests(mode);

            var poly = (LayerDerived)Leaf().poly;
            Assert.AreEqual(3, poly.a);
            Assert.AreEqual(2, poly.b);
        }

        [Test]
        public void LayerNoneThenClass([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<poly><a>1</a></poly>",
                @"<poly class=""LayerDerived""><b>2</b></poly>");
            parser.Finish();

            DoParserTests(mode);

            var poly = (LayerDerived)Leaf().poly;
            Assert.AreEqual(1, poly.a);
            Assert.AreEqual(2, poly.b);
        }

        [Test]
        public void LayerClassThenUnrelatedClass([Values] ParserMode mode)
        {
            var parser = LayerParser(
                @"<poly class=""LayerDerived""><b>2</b></poly>",
                @"<poly class=""LayerDerivedAlt""><a>3</a></poly>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("LayerDerivedAlt");
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            var poly = (LayerDerived)Leaf().poly;
            Assert.AreEqual(3, poly.a);
            Assert.AreEqual(2, poly.b);
        }

        [Test]
        public void LayerClassThenBaseClass([Values] ParserMode mode)
        {
            var parser = LayerParser(
                @"<poly class=""LayerDerived""><b>2</b></poly>",
                @"<poly class=""LayerBase""><a>3</a></poly>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("LayerBase");
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            var poly = (LayerDerived)Leaf().poly;
            Assert.AreEqual(3, poly.a);
            Assert.AreEqual(2, poly.b);
        }

        [Test]
        public void LayerInterfaceClassFromChild([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<iface><a>1</a></iface>",
                @"<iface class=""LayerImpl"" />");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(1, ((LayerImpl)Leaf().iface).a);
        }

        [Test]
        public void LayerContainerClassThenAppend([Values] ParserMode mode)
        {
            var parser = LayerParser(
                @"<list class=""LayerList""><li>1</li></list>",
                @"<list mode=""append""><li>2</li></list>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsInstanceOf<LayerList>(Leaf().list);
            CollectionAssert.AreEqual(new[] { 1, 2 }, Leaf().list);
        }

        [Test]
        public void LayerClassThenExplicitReplace([Values] ParserMode mode)
        {
            // An explicit replace starts over, class included.
            var parser = LayerParser(
                @"<list class=""LayerList""><li>1</li></list>",
                @"<list mode=""replace""><li>2</li></list>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(typeof(List<int>), Leaf().list.GetType());
            CollectionAssert.AreEqual(new[] { 2 }, Leaf().list);
        }

        [Test]
        public void LayerInvalidTextKeepsOtherLayers([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<obj>junk</obj>",
                "<obj><x>1</x></obj>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return true;
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            Assert.AreEqual(1, Leaf().obj.x);
        }

        [Test]
        public void LayerInvalidAppendKeepsParent([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<list><li>1</li></list>",
                @"<list mode=""append"">junk</list>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return true;
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            CollectionAssert.AreEqual(new[] { 1 }, Leaf().list);
        }

        [Test]
        public void LayerUnknownFieldEachLayer([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<nope>1</nope>",
                "<nope>2</nope><value>3</value>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("nope");
            });
            Assert.AreEqual(2, errors);

            DoParserTests(mode);

            Assert.AreEqual(3, Leaf().value);
        }

        [Test]
        public void LayerDuplicateFieldInFirstLayer([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<value>1</value><value>2</value>",
                "<obj><x>1</x></obj>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("value");
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            Assert.AreEqual(2, Leaf().value);
        }

        [Test]
        public void ReplaceDefaultWithClass([Values] ParserMode mode)
        {
            var parser = LayerParser(
                @"<listDefault mode=""replace"" class=""LayerList""><li>2</li></listDefault><dictDefault mode=""replace"" class=""LayerDict""><li><key>2</key><value>2</value></li></dictDefault><listDerivedDefault mode=""replace"" class=""LayerList""><li>2</li></listDerivedDefault>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsInstanceOf<LayerList>(Leaf().listDefault);
            CollectionAssert.AreEqual(new[] { 2 }, Leaf().listDefault);
            Assert.IsInstanceOf<LayerDict>(Leaf().dictDefault);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 2, 2 } }, Leaf().dictDefault);
            Assert.AreEqual(typeof(LayerList), Leaf().listDerivedDefault.GetType());
            CollectionAssert.AreEqual(new[] { 2 }, Leaf().listDerivedDefault);
        }

        [Test]
        public void LayerContainerClassThenImplicitReplace([Values] ParserMode mode)
        {
            var parser = LayerParser(
                @"<list class=""LayerList""><li>1</li></list>",
                @"<list class=""LayerListAlt""><li>2</li></list>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsInstanceOf<LayerListAlt>(Leaf().list);
            CollectionAssert.AreEqual(new[] { 2 }, Leaf().list);
        }

        [Test]
        public void ImplicitReplaceDefaultWithClass([Values] ParserMode mode)
        {
            var parser = LayerParser(@"<listDefault class=""LayerList""><li>2</li></listDefault>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsInstanceOf<LayerList>(Leaf().listDefault);
            CollectionAssert.AreEqual(new[] { 2 }, Leaf().listDefault);
        }

        [Test]
        public void AppendDefaultWithClass([Values] ParserMode mode)
        {
            // Appending keeps the C# default, so the class still has to match it.
            var parser = LayerParser(@"<listDefault mode=""append"" class=""LayerList""><li>2</li></listDefault>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("LayerList"));

            DoParserTests(mode);

            CollectionAssert.AreEqual(new[] { 9, 2 }, Leaf().listDefault);
        }

        [Test]
        public void LayerContainerInheritsClass([Values] ParserMode mode)
        {
            var parser = LayerParser<LayerPolyDec>(
                @"<enumerable class=""LayerList""><li>1</li></enumerable><anything class=""LayerList""><li>1</li></anything><value class=""int"">1</value><list class=""LayerList""><li>1</li></list>",
                "<enumerable><li>2</li></enumerable><anything><li>2</li></anything><value>2</value><list><li>2</li></list>");
            parser.Finish();

            DoParserTests(mode);

            var leaf = Dec.Database<LayerPolyDec>.Get("Leaf");
            Assert.IsInstanceOf<LayerList>(leaf.enumerable);
            CollectionAssert.AreEqual(new[] { 2 }, leaf.enumerable);
            Assert.IsInstanceOf<LayerList>(leaf.anything);
            CollectionAssert.AreEqual(new[] { 2 }, (LayerList)leaf.anything);
            Assert.AreEqual(2, leaf.value);
            Assert.IsInstanceOf<LayerList>(leaf.list);
            CollectionAssert.AreEqual(new[] { 2 }, leaf.list);
        }

        [Test]
        public void LayerHashSetPatch([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<set><li>1</li></set>",
                @"<set mode=""patch""><li>1</li><li>2</li></set>");
            parser.Finish();

            DoParserTests(mode);

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, Leaf().set);
        }

        [Test]
        public void LayerBase64ThenAppend([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<bytes>AQID</bytes>",
                @"<bytes mode=""append""><li>4</li></bytes>");
            parser.Finish();

            DoParserTests(mode);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, Leaf().bytes);
        }

        [Test]
        public void LayerDuplicateField([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<value>1</value>",
                "<value>2</value><value>3</value>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err =>
            {
                errors++;
                return err.Contains("value");
            });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            Assert.AreEqual(3, Leaf().value);
        }

        [Test]
        public void LayerReplacedInvalidValue([Values] ParserMode mode)
        {
            // XML a later layer replaces is never parsed, so it can't report errors.
            var parser = LayerParser(
                "<value>abc</value>",
                "<value>3</value>");
            parser.Finish();

            DoParserTests(mode);

            Assert.AreEqual(3, Leaf().value);
        }

        [Test]
        public void LayerInheritedInvalidValue([Values] ParserMode mode)
        {
            var parser = LayerParser(
                "<value>abc</value>",
                "<obj><x>1</x></obj>");
            ExpectErrors(() => parser.Finish(), err => err.Contains("abc"));

            DoParserTests(mode);

            Assert.AreEqual(0, Leaf().value);
            Assert.AreEqual(1, Leaf().obj.x);
        }
    }
}
