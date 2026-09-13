using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ReflectionIntrospection : Base
    {
        // ==== Test types ====

        public class ScalarsRec : Dec.IRecordable
        {
            public int intField = 42;
            public string stringField = "hello";
            public GenericEnum enumField = GenericEnum.Beta;
            public StubDec decField;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref intField, "intField");
                record.Record(ref stringField, "stringField");
                record.Record(ref enumField, "enumField");
                record.Record(ref decField, "decField");
            }
        }

        public class TypeListRec : Dec.IRecordable
        {
            public List<Type> types = new List<Type>() { typeof(string) };

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref types, "types");
            }
        }

        public class SharedRefsRec : Dec.IRecordable
        {
            public StubRecordableInt a;
            public StubRecordableInt b;
            public StubRecordableInt empty;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref a, "a");
                record.Shared().Record(ref b, "b");
                record.Shared().Record(ref empty, "empty");
            }
        }

        public class ArraysRec : Dec.IRecordable
        {
            public int[] numbers = { 10, 20, 30 };
            public int[] nullArray;
            public byte[] bytes = { 1, 2, 3 };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref numbers, "numbers");
                record.Record(ref nullArray, "nullArray");
                record.Record(ref bytes, "bytes");
            }
        }

        public class ListRec : Dec.IRecordable
        {
            public List<string> strings = new List<string>() { "alpha", "beta" };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref strings, "strings");
            }
        }

        public class QueueStackRec : Dec.IRecordable
        {
            public Queue<int> queue = new Queue<int>();
            public Stack<int> stack = new Stack<int>();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref queue, "queue");
                record.Record(ref stack, "stack");
            }
        }

        public class ConditionRec : Dec.IRecordable
        {
            public int id;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref id, "id");
            }
        }

        public class CombinatorRec : Dec.IRecordable
        {
            public ConditionRec[] conditions;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref conditions, "conditions");
            }
        }

        public struct OptionStruct : Dec.IRecordable
        {
            public string text;
            public StubRecordableInt node;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref text, "text");
                record.Shared().Record(ref node, "node");
            }
        }

        public class OptionsRec : Dec.IRecordable
        {
            public OptionStruct[] options;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref options, "options");
            }
        }

        public class NestedRec : Dec.IRecordable
        {
            public StubRecordableInt inner = new StubRecordableInt();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref inner, "inner");
            }
        }

        public class ConvPoint
        {
            public int x;
            public int y;
        }

        public class ConvPointConverter : Dec.ConverterString<ConvPoint>
        {
            public override string Write(ConvPoint input)
            {
                return $"{input.x},{input.y}";
            }

            public override ConvPoint Read(string input, Dec.Context context)
            {
                var chunks = input.Split(',');
                return new ConvPoint { x = int.Parse(chunks[0]), y = int.Parse(chunks[1]) };
            }
        }

        public class ConverterHolderRec : Dec.IRecordable
        {
            public ConvPoint point = new ConvPoint { x = 1, y = 2 };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref point, "point");
            }
        }

        public class ConvRecPoint
        {
            public int x;
            public int y;
        }

        public class ConvRecPointConverter : Dec.ConverterRecord<ConvRecPoint>
        {
            public override void Record(ref ConvRecPoint input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.x, "x");
                recorder.Record(ref input.y, "y");
            }
        }

        public struct ConvRecVec
        {
            public float a;
            public float b;
        }

        public class ConvRecVecConverter : Dec.ConverterRecord<ConvRecVec>
        {
            public override void Record(ref ConvRecVec input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.a, "a");
                recorder.Record(ref input.b, "b");
            }
        }

        // A ConverterRecord body that records a temporary and replaces its instance, so the value handed back from Record is the only carrier of a write.
        public class ConvRecReplaced
        {
            public int v;
        }

        public class ConvRecReplacedConverter : Dec.ConverterRecord<ConvRecReplaced>
        {
            public override void Record(ref ConvRecReplaced input, Dec.Recorder recorder)
            {
                int v = input.v;
                recorder.Record(ref v, "v");
                input = new ConvRecReplaced() { v = v };
            }
        }

        public class ConvFacNode
        {
            public int payload;

            public ConvFacNode(int payload)
            {
                this.payload = payload;
            }
        }

        public class ConvFacNodeConverter : Dec.ConverterFactory<ConvFacNode>
        {
            public override void Write(ConvFacNode input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.payload, "payload");
            }

            public override ConvFacNode Create(Dec.Recorder recorder)
            {
                return new ConvFacNode(0);
            }

            public override void Read(ref ConvFacNode input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.payload, "payload");
            }
        }

        public class ConvOuter
        {
            public int k;
            public ConvRecPoint inner = new ConvRecPoint();
        }

        public class ConvOuterConverter : Dec.ConverterRecord<ConvOuter>
        {
            public override void Record(ref ConvOuter input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.k, "k");
                recorder.Record(ref input.inner, "inner");
            }
        }

        public class ConverterRecordHolderRec : Dec.IRecordable
        {
            public ConvRecPoint point = new ConvRecPoint() { x = 1, y = 2 };
            public ConvRecVec vec = new ConvRecVec() { a = 1.5f, b = 2.5f };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref point, "point");
                record.Record(ref vec, "vec");
            }
        }

        public class ConverterFactoryHolderRec : Dec.IRecordable
        {
            public ConvFacNode node = new ConvFacNode(7);

            public void Record(Dec.Recorder record)
            {
                record.Record(ref node, "node");
            }
        }

        public class ConverterReplacedHolderRec : Dec.IRecordable
        {
            public ConvRecReplaced rep = new ConvRecReplaced() { v = 3 };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref rep, "rep");
            }
        }

        public class ConverterSharedHolderRec : Dec.IRecordable
        {
            public ConvRecPoint shared = new ConvRecPoint() { x = 3, y = 4 };
            public ConvRecPoint[] points;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref shared, "shared");
                record.Shared().Record(ref points, "points");
            }
        }

        public class ConverterArrayHolderRec : Dec.IRecordable
        {
            public ConvRecVec[] vecs = { new ConvRecVec() { a = 1, b = 2 }, new ConvRecVec() { a = 3, b = 4 } };
            public List<ConvRecPoint> pointList = new List<ConvRecPoint>() { new ConvRecPoint() { x = 5, y = 6 } };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref vecs, "vecs");
                record.Record(ref pointList, "pointList");
            }
        }

        public class ConverterNestedHolderRec : Dec.IRecordable
        {
            public ConvOuter outer = new ConvOuter() { k = 1, inner = new ConvRecPoint() { x = 5, y = 6 } };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref outer, "outer");
            }
        }

        public class AsThisConverterRec : Dec.IRecordable
        {
            public ConvRecPoint point = new ConvRecPoint() { x = 8, y = 9 };

            public void Record(Dec.Recorder record)
            {
                record.RecordAsThis(ref point);
            }
        }

        public class AsThisNullRec : Dec.IRecordable
        {
            public AsThisPayload payload;

            public void Record(Dec.Recorder record)
            {
                record.RecordAsThis(ref payload);
            }
        }

        public class CondRecConv : Dec.IConditionalRecordable
        {
            public int data = 5;

            public bool ShouldRecord(Dec.Recorder.IUserSettings userSettings)
            {
                return !(userSettings is ExcludeSettings);
            }

            public void Record(Dec.Recorder record)
            {
                record.Record(ref data, "data");
            }
        }

        public class CondRecConvConverter : Dec.ConverterRecord<CondRecConv>
        {
            public override void Record(ref CondRecConv input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.data, "converted");
            }
        }

        public class CondConvHolder : Dec.IRecordable
        {
            public CondRecConv cond = new CondRecConv();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref cond, "cond");
            }
        }

        public class PlainPoco
        {
            public int z;
        }

        public class AsThisPayload : Dec.IRecordable
        {
            public int alpha;
            public string beta;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref alpha, "alpha");
                record.Record(ref beta, "beta");
            }
        }

        public class AsThisWrapper : Dec.IRecordable
        {
            public AsThisPayload payload = new AsThisPayload();

            public void Record(Dec.Recorder record)
            {
                record.RecordAsThis(ref payload);
            }
        }

        public class AsThisHolder : Dec.IRecordable
        {
            public AsThisWrapper wrapper = new AsThisWrapper();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref wrapper, "wrapper");
            }
        }

        public class AsThisListWrapper : Dec.IRecordable
        {
            public List<int> items = new List<int>() { 1, 2, 3 };

            public void Record(Dec.Recorder record)
            {
                record.RecordAsThis(ref items);
            }
        }

        public class AsThisListHolder : Dec.IRecordable
        {
            public AsThisListWrapper wrapper = new AsThisListWrapper();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref wrapper, "wrapper");
            }
        }

        public struct AsThisStruct : Dec.IRecordable
        {
            public AsThisPayload payload;

            public void Record(Dec.Recorder record)
            {
                record.RecordAsThis(ref payload);
            }
        }

        public class AsThisSharedContainerRec : Dec.IRecordable
        {
            public AsThisStruct[] items;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref items, "items");
            }
        }

        public class BaseRec : Dec.IRecordable
        {
            public int baseVal;

            public virtual void Record(Dec.Recorder record)
            {
                record.Record(ref baseVal, "baseVal");
            }
        }

        public class DerivedRec : BaseRec
        {
            public int derivedVal;

            public override void Record(Dec.Recorder record)
            {
                base.Record(record);
                record.Record(ref derivedVal, "derivedVal");
            }
        }

        public class PolymorphicHolder : Dec.IRecordable
        {
            public BaseRec item = new DerivedRec() { baseVal = 1, derivedVal = 2 };

            public void Record(Dec.Recorder record)
            {
                record.Record(ref item, "item");
            }
        }

        public class ReadOnlyShapesRec : Dec.IRecordable
        {
            public Dictionary<string, int> dict = new Dictionary<string, int>() { { "k1", 1 }, { "k2", 2 } };
            public HashSet<int> set = new HashSet<int>() { 5 };
            public int[,] grid = new int[2, 2] { { 1, 2 }, { 3, 4 } };
            public (int, string) tuple = (7, "t");

            public void Record(Dec.Recorder record)
            {
                record.Record(ref dict, "dict");
                record.Record(ref set, "set");
                record.Record(ref grid, "grid");
                record.Record(ref tuple, "tuple");
            }
        }

        // A user type that is a tuple by interface and a record body by contract; the writer dispatches on IRecordable first, so SetByPath must too.
        public class TupleShapedRec : System.Runtime.CompilerServices.ITuple, Dec.IRecordable
        {
            public int inner = 3;

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public object this[int index]
            {
                get
                {
                    return inner;
                }
            }

            public void Record(Dec.Recorder record)
            {
                record.Record(ref inner, "inner");
            }
        }

        public class BespokeRec : Dec.IRecordable
        {
            public Dictionary<Type, StubRecordableInt> dict = new Dictionary<Type, StubRecordableInt>() { { typeof(StubRecordableInt), new StubRecordableInt() { data = 9 } } };

            public void Record(Dec.Recorder record)
            {
                record.Bespoke_KeyTypeDict().Record(ref dict, "dict");
            }
        }

        public class ExcludeSettings : Dec.Recorder.IUserSettings { }

        public class CondRec : Dec.IConditionalRecordable
        {
            public int data = 5;

            public bool ShouldRecord(Dec.Recorder.IUserSettings userSettings)
            {
                return !(userSettings is ExcludeSettings);
            }

            public void Record(Dec.Recorder record)
            {
                record.Record(ref data, "data");
            }
        }

        public class CondRecConverter : Dec.ConverterString<CondRec>
        {
            public override string Write(CondRec input)
            {
                return $"cond:{input.data}";
            }

            public override CondRec Read(string input, Dec.Context context)
            {
                return new CondRec { data = int.Parse(input.Substring(5)) };
            }
        }

        public class CondHolder : Dec.IRecordable
        {
            public CondRec cond = new CondRec();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref cond, "cond");
            }
        }

        public class SharedContainerTwiceRec : Dec.IRecordable
        {
            public ConditionRec[] arrA;
            public ConditionRec[] arrB;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref arrA, "arrA");
                record.Shared().Record(ref arrB, "arrB");
            }
        }

        public class CombinatorCrossRec : Dec.IRecordable
        {
            public ConditionRec[] conditions;
            public ConditionRec first;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref conditions, "conditions");
                record.Shared().Record(ref first, "first");
            }
        }

        public class CycleRec : Dec.IRecordable
        {
            public CycleRec next;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref next, "next");
            }
        }

        public class MultiplicityRec : Dec.IRecordable
        {
            public StubRecordableInt x;
            public StubRecordableInt y;
            public bool shareY;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref x, "x");
                if (shareY)
                {
                    record.Shared().Record(ref y, "y");
                }
                else
                {
                    record.Record(ref y, "y");
                }
            }
        }

        public class SelfRec : Dec.IRecordable
        {
            public SelfRec self;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref self, "self");
            }
        }

        public class TrimRec : Dec.IRecordable
        {
            public StubRecordableInt keep = new StubRecordableInt();
            public StubRecordableInt trim = new StubRecordableInt();
            public int scalar = 3;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref keep, "keep");
                record.Record(ref trim, "trim");
                record.Record(ref scalar, "scalar");
            }
        }

        public class NullableRec : Dec.IRecordable
        {
            public int? maybe;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref maybe, "maybe");
            }
        }

        public class IntrospectPlain
        {
            public int p = 1;
            public string q = "pq";
        }

        public struct IntrospectStruct
        {
            public int s;
            public float t;
        }

        public class IntrospectBaseDec : Dec.Dec
        {
            #pragma warning disable CS0649
            private int hidden;
            #pragma warning restore CS0649

            public int Hidden()
            {
                return hidden;
            }
        }

        public class IntrospectDec : IntrospectBaseDec
        {
            public int value;
            public string text;
            public IntrospectDec other;
            public IntrospectPlain plain = new IntrospectPlain();
            public IntrospectPlain plainAlias;
            public IntrospectStruct strukt;
            public StubRecordableInt rec = new StubRecordableInt();
            public List<int> list = new List<int>();
            public readonly int fixedValue = 7;
        }

        public class IntrospectSharedHolder : Dec.IRecordable
        {
            public StubRecordableInt payload;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref payload, "payload");
            }
        }

        public class IntrospectSharedDec : Dec.Dec
        {
            public IntrospectSharedHolder holder = new IntrospectSharedHolder();
        }

        public class IntrospectRecordableDec : Dec.Dec, Dec.IRecordable
        {
            public int fieldName;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref fieldName, "label");
            }
        }

        public class PlainHolderRec : Dec.IRecordable
        {
            public IntrospectPlain plain = new IntrospectPlain();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref plain, "plain");
            }
        }

        public class IntrospectCycleDec : Dec.Dec
        {
            public List<object> loop;
        }

        // ==== Helpers ====

        private static Dec.Reflection.Entry Child(Dec.Reflection.Entry parent, string label)
        {
            var matches = parent.Children.Where(c => c.Label == label).ToList();
            Assert.AreEqual(1, matches.Count, $"expected exactly one child labeled {label}");
            return matches[0];
        }

        private static void AssertLeaf(Dec.Reflection.Entry entry)
        {
            Assert.AreEqual(0, entry.Children.Count, $"expected {entry.Label} to be a leaf");
        }

        private static void AssertTreesEqual(Dec.Reflection.Entry lhs, Dec.Reflection.Entry rhs)
        {
            Assert.AreEqual(lhs.Label, rhs.Label);
            Assert.AreEqual(lhs.DeclaredType, rhs.DeclaredType);
            Assert.AreEqual(lhs.Shared, rhs.Shared);
            Assert.AreEqual(lhs.Writable, rhs.Writable);
            Assert.AreEqual(lhs.Value, rhs.Value);
            Assert.AreEqual(lhs.Path, rhs.Path);
            Assert.AreEqual(lhs.Children.Count, rhs.Children.Count);
            for (int i = 0; i < lhs.Children.Count; ++i)
            {
                AssertTreesEqual(lhs.Children[i], rhs.Children[i]);
            }
        }

        private static readonly Type[] Converters = { typeof(ConvPointConverter), typeof(CondRecConverter), typeof(ConvRecPointConverter), typeof(ConvRecVecConverter), typeof(ConvRecReplacedConverter), typeof(ConvFacNodeConverter), typeof(ConvOuterConverter), typeof(CondRecConvConverter) };

        private void RegisterConverters()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = Converters });
        }

        // Parses the Dec fixtures (with the converters registered too, so a test can use both) and returns dec A.
        private IntrospectDec ParseIntrospectDecs()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IntrospectBaseDec), typeof(IntrospectDec), typeof(IntrospectSharedDec), typeof(IntrospectRecordableDec), typeof(IntrospectCycleDec) }, explicitConverters = Converters });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IntrospectDec decName=""A"">
                        <value>3</value>
                        <text>hello</text>
                        <other>B</other>
                        <plain><p>5</p><q>five</q></plain>
                        <strukt><s>2</s><t>1.5</t></strukt>
                        <rec><dataRecorded>9</dataRecorded></rec>
                        <list><li>1</li><li>2</li></list>
                        <hidden>4</hidden>
                    </IntrospectDec>
                    <IntrospectDec decName=""B"" />
                    <IntrospectSharedDec decName=""S"">
                        <holder><payload><dataRecorded>6</dataRecorded></payload></holder>
                    </IntrospectSharedDec>
                    <IntrospectRecordableDec decName=""R"">
                        <label>8</label>
                    </IntrospectRecordableDec>
                    <IntrospectCycleDec decName=""C"" />
                </Decs>");
            parser.Finish();

            return Dec.Database<IntrospectDec>.Get("A");
        }

        private static void AssertNoneShared(Dec.Reflection.Entry entry)
        {
            Assert.IsFalse(entry.Shared, entry.Path.Serialize());
            foreach (var child in entry.Children)
            {
                AssertNoneShared(child);
            }
        }

        // ==== Enumerate goldens ====

        [Test]
        public void Scalars()
        {
            var obj = new ScalarsRec();
            var root = Dec.Reflection.Enumerate(obj);

            Assert.IsNull(root.Label);
            Assert.AreEqual(typeof(ScalarsRec), root.DeclaredType);
            Assert.IsFalse(root.Shared);
            Assert.IsFalse(root.Writable);
            Assert.IsNull(root.Parent);
            Assert.AreSame(obj, root.Value);
            Assert.AreEqual(new[] { "intField", "stringField", "enumField", "decField" }, root.Children.Select(c => c.Label).ToArray());

            var intField = Child(root, "intField");
            Assert.AreEqual(typeof(int), intField.DeclaredType);
            Assert.AreEqual(42, intField.Value);
            Assert.IsFalse(intField.Shared);
            Assert.IsTrue(intField.Writable);
            Assert.AreSame(root, intField.Parent);
            AssertLeaf(intField);
            StringAssert.EndsWith("intField", intField.Path.Serialize());

            var stringField = Child(root, "stringField");
            Assert.AreEqual(typeof(string), stringField.DeclaredType);
            Assert.AreEqual("hello", stringField.Value);
            AssertLeaf(stringField);

            var enumField = Child(root, "enumField");
            Assert.AreEqual(typeof(GenericEnum), enumField.DeclaredType);
            Assert.AreEqual(GenericEnum.Beta, enumField.Value);
            AssertLeaf(enumField);

            var decField = Child(root, "decField");
            Assert.AreEqual(typeof(StubDec), decField.DeclaredType);
            Assert.IsNull(decField.Value);
            Assert.IsFalse(decField.Shared);
            AssertLeaf(decField);
        }

        [Test]
        public void TypeSharedness()
        {
            // A live Type value's GetType() is the private System.RuntimeType; the writer normalizes it and never refs Types, so Shared must be false even at a shareable position.
            var obj = new TypeListRec();
            var root = Dec.Reflection.Enumerate(obj);

            var types = Child(root, "types");
            Assert.IsTrue(types.Shared);
            Assert.AreEqual(1, types.Children.Count);

            var element = types.Children[0];
            Assert.AreEqual(typeof(Type), element.DeclaredType);
            Assert.AreEqual(typeof(string), element.Value);
            Assert.IsFalse(element.Shared);
            AssertLeaf(element);
        }

        [Test]
        public void SharedRefs()
        {
            var shared = new StubRecordableInt() { data = 7 };
            var obj = new SharedRefsRec() { a = shared, b = shared };
            var root = Dec.Reflection.Enumerate(obj);

            var a = Child(root, "a");
            Assert.AreEqual(typeof(StubRecordableInt), a.DeclaredType);
            Assert.IsTrue(a.Shared);
            Assert.AreSame(shared, a.Value);
            Assert.IsTrue(a.Writable);
            AssertLeaf(a);

            var b = Child(root, "b");
            Assert.IsTrue(b.Shared);
            Assert.AreSame(shared, b.Value);
            AssertLeaf(b);

            var empty = Child(root, "empty");
            Assert.IsTrue(empty.Shared);
            Assert.IsNull(empty.Value);
            AssertLeaf(empty);

            // cross-check: the real writer does emit a ref for this shape
            var doc = XDocument.Parse(Dec.Recorder.Write(obj));
            Assert.IsTrue(doc.Descendants().Any(e => e.Attribute("ref") != null));
        }

        [Test]
        public void Arrays()
        {
            var obj = new ArraysRec();
            var root = Dec.Reflection.Enumerate(obj);

            var numbers = Child(root, "numbers");
            Assert.AreEqual(typeof(int[]), numbers.DeclaredType);
            Assert.AreSame(obj.numbers, numbers.Value);
            Assert.IsTrue(numbers.Writable);
            Assert.AreEqual(new[] { "0", "1", "2" }, numbers.Children.Select(c => c.Label).ToArray());
            Assert.AreEqual(new object[] { 10, 20, 30 }, numbers.Children.Select(c => c.Value).ToArray());
            foreach (var element in numbers.Children)
            {
                Assert.AreEqual(typeof(int), element.DeclaredType);
                Assert.IsTrue(element.Writable);
                AssertLeaf(element);
            }
            StringAssert.EndsWith("numbers[1]", numbers.Children[1].Path.Serialize());

            var nullArray = Child(root, "nullArray");
            Assert.AreEqual(typeof(int[]), nullArray.DeclaredType);
            Assert.IsNull(nullArray.Value);
            Assert.IsTrue(nullArray.Writable);
            AssertLeaf(nullArray);

            var bytes = Child(root, "bytes");
            Assert.AreEqual(typeof(byte[]), bytes.DeclaredType);
            Assert.AreSame(obj.bytes, bytes.Value);
            Assert.IsTrue(bytes.Writable);
            AssertLeaf(bytes);
        }

        [Test]
        public void Lists()
        {
            var obj = new ListRec();
            var root = Dec.Reflection.Enumerate(obj);

            var strings = Child(root, "strings");
            Assert.AreEqual(typeof(List<string>), strings.DeclaredType);
            Assert.IsTrue(strings.Writable);
            Assert.AreEqual(new[] { "0", "1" }, strings.Children.Select(c => c.Label).ToArray());
            Assert.AreEqual(new object[] { "alpha", "beta" }, strings.Children.Select(c => c.Value).ToArray());
            Assert.AreEqual(typeof(string), strings.Children[0].DeclaredType);
            Assert.IsTrue(strings.Children[0].Writable);
        }

        [Test]
        public void QueueStack()
        {
            var obj = new QueueStackRec();
            obj.queue.Enqueue(1);
            obj.queue.Enqueue(2);
            obj.queue.Enqueue(3);
            obj.stack.Push(1);
            obj.stack.Push(2);
            obj.stack.Push(3);

            var root = Dec.Reflection.Enumerate(obj);

            var queue = Child(root, "queue");
            Assert.IsTrue(queue.Writable);
            Assert.IsFalse(queue.Children[0].Writable);

            var stack = Child(root, "stack");
            Assert.IsTrue(stack.Writable);
            Assert.IsFalse(stack.Children[0].Writable);

            // pin element order against what the file actually contains
            var doc = XDocument.Parse(Dec.Recorder.Write(obj));
            var queueXml = doc.Descendants("queue").Single().Elements("li").Select(li => (object)int.Parse(li.Value)).ToArray();
            var stackXml = doc.Descendants("stack").Single().Elements("li").Select(li => (object)int.Parse(li.Value)).ToArray();
            Assert.AreEqual(queueXml, queue.Children.Select(c => c.Value).ToArray());
            Assert.AreEqual(stackXml, stack.Children.Select(c => c.Value).ToArray());
        }

        [Test]
        public void CombinatorShape()
        {
            // A .Shared() array's elements are the actual shared refs in the file: the container entry descends to expose them, and each element is then a ref leaf.
            var obj = new CombinatorRec() { conditions = new[] { new ConditionRec() { id = 1 }, new ConditionRec() { id = 2 } } };
            var root = Dec.Reflection.Enumerate(obj);

            var conditions = Child(root, "conditions");
            Assert.IsTrue(conditions.Shared);
            Assert.IsTrue(conditions.Writable);
            Assert.AreEqual(2, conditions.Children.Count);

            for (int i = 0; i < 2; ++i)
            {
                var element = conditions.Children[i];
                Assert.AreEqual(typeof(ConditionRec), element.DeclaredType);
                Assert.IsTrue(element.Shared);
                Assert.AreSame(obj.conditions[i], element.Value);
                Assert.IsTrue(element.Writable);
                AssertLeaf(element);
            }
        }

        [Test]
        public void StructElementArray()
        {
            var node = new StubRecordableInt() { data = 3 };
            var obj = new OptionsRec() { options = new[] { new OptionStruct() { text = "one", node = node }, new OptionStruct() { text = "two" } } };
            var root = Dec.Reflection.Enumerate(obj);

            var options = Child(root, "options");
            Assert.AreEqual(2, options.Children.Count);

            var first = options.Children[0];
            Assert.AreEqual("0", first.Label);
            Assert.AreEqual(typeof(OptionStruct), first.DeclaredType);
            Assert.IsFalse(first.Shared);
            Assert.IsTrue(first.Writable);

            var text = Child(first, "text");
            Assert.AreEqual("one", text.Value);
            Assert.IsTrue(text.Writable);
            AssertLeaf(text);

            var nodeEntry = Child(first, "node");
            Assert.IsTrue(nodeEntry.Shared);
            Assert.AreSame(node, nodeEntry.Value);
            Assert.IsTrue(nodeEntry.Writable);
            AssertLeaf(nodeEntry);

            StringAssert.EndsWith("options[0].text", text.Path.Serialize());
        }

        [Test]
        public void NestedRecordable()
        {
            var obj = new NestedRec();
            obj.inner.data = 11;
            var root = Dec.Reflection.Enumerate(obj);

            var inner = Child(root, "inner");
            Assert.AreEqual(typeof(StubRecordableInt), inner.DeclaredType);
            Assert.IsFalse(inner.Shared);
            Assert.IsTrue(inner.Writable);

            // label != field name, via StubRecordableInt's deliberate mismatch
            var data = Child(inner, "dataRecorded");
            Assert.AreEqual(11, data.Value);
            Assert.IsTrue(data.Writable);
            AssertLeaf(data);
        }

        [Test]
        public void ConverterStringLeaf()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvPointConverter) } });

            var obj = new ConverterHolderRec();
            var root = Dec.Reflection.Enumerate(obj);

            var point = Child(root, "point");
            Assert.AreEqual(typeof(ConvPoint), point.DeclaredType);
            Assert.AreSame(obj.point, point.Value);
            Assert.IsTrue(point.Writable);
            AssertLeaf(point);
        }

        [Test]
        public void ConverterRecordDescends()
        {
            RegisterConverters();

            var obj = new ConverterRecordHolderRec();
            var root = Dec.Reflection.Enumerate(obj);

            // class-backed
            var point = Child(root, "point");
            Assert.AreEqual(typeof(ConvRecPoint), point.DeclaredType);
            Assert.AreSame(obj.point, point.Value);
            Assert.IsTrue(point.Writable);
            Assert.AreEqual(new[] { "x", "y" }, point.Children.Select(c => c.Label).ToArray());

            var x = Child(point, "x");
            Assert.AreEqual(typeof(int), x.DeclaredType);
            Assert.AreEqual(1, x.Value);
            Assert.IsTrue(x.Writable);
            Assert.AreEqual(new Dec.PathMember(point.Path, "x"), x.Path);
            AssertLeaf(x);

            // struct-backed
            var vec = Child(root, "vec");
            Assert.AreEqual(typeof(ConvRecVec), vec.DeclaredType);
            Assert.IsFalse(vec.Shared);
            Assert.AreEqual(2.5f, Child(vec, "b").Value);
            Assert.IsTrue(Child(vec, "b").Writable);

            // the file has the same children
            var xml = Dec.Recorder.Write(obj);
            StringAssert.Contains("<x>1</x>", xml);
            StringAssert.Contains("<b>2.5</b>", xml);
        }

        [Test]
        public void ConverterFactoryDescends()
        {
            RegisterConverters();

            var obj = new ConverterFactoryHolderRec();
            var root = Dec.Reflection.Enumerate(obj);

            var node = Child(root, "node");
            Assert.AreEqual(typeof(ConvFacNode), node.DeclaredType);
            Assert.AreSame(obj.node, node.Value);
            Assert.IsTrue(node.Writable);

            // the factory's Write receives its input by value, so nothing beneath it is settable
            var payload = Child(node, "payload");
            Assert.AreEqual(7, payload.Value);
            Assert.IsFalse(payload.Writable);
            AssertLeaf(payload);

            StringAssert.Contains("<payload>7</payload>", Dec.Recorder.Write(obj));
        }

        [Test]
        public void ConverterRecordSharedIsLeaf()
        {
            RegisterConverters();

            var obj = new ConverterSharedHolderRec() { points = new[] { new ConvRecPoint() { x = 1, y = 1 }, new ConvRecPoint() { x = 2, y = 2 } } };
            var root = Dec.Reflection.Enumerate(obj);

            // a converter-backed value at a shared position is a ref leaf, through the same gate as a recordable
            var shared = Child(root, "shared");
            Assert.IsTrue(shared.Shared);
            Assert.IsTrue(shared.Writable);
            AssertLeaf(shared);

            // the combinator shape, converter flavor: the shared container descends and its elements are ref leaves
            var points = Child(root, "points");
            Assert.IsTrue(points.Shared);
            Assert.AreEqual(2, points.Children.Count);
            for (int i = 0; i < 2; ++i)
            {
                Assert.IsTrue(points.Children[i].Shared);
                Assert.AreSame(obj.points[i], points.Children[i].Value);
                AssertLeaf(points.Children[i]);
            }
        }

        [Test]
        public void ConverterNested()
        {
            RegisterConverters();

            var obj = new ConverterNestedHolderRec();
            var root = Dec.Reflection.Enumerate(obj);

            var inner = Child(Child(root, "outer"), "inner");
            Assert.AreEqual(typeof(ConvRecPoint), inner.DeclaredType);

            var x = Child(inner, "x");
            Assert.AreEqual(5, x.Value);
            Assert.IsTrue(x.Writable);
            StringAssert.EndsWith("outer.inner.x", x.Path.Serialize());
        }

        [Test]
        public void ConditionalFallsThroughToConverterRecord()
        {
            RegisterConverters();

            var obj = new CondConvHolder();

            var included = Child(Dec.Reflection.Enumerate(obj), "cond");
            Assert.AreEqual(new[] { "data" }, included.Children.Select(c => c.Label).ToArray());

            var excluded = Child(Dec.Reflection.Enumerate(obj, userSettings: new ExcludeSettings()), "cond");
            Assert.AreEqual(new[] { "converted" }, excluded.Children.Select(c => c.Label).ToArray());
            Assert.IsTrue(Child(excluded, "converted").Writable);

            StringAssert.Contains("<data>", Dec.Recorder.Write(obj));
            StringAssert.Contains("<converted>", Dec.Recorder.Write(obj, userSettings: new ExcludeSettings()));
        }

        [Test]
        public void ShouldDescendTrimsConverter()
        {
            RegisterConverters();

            var obj = new ConverterRecordHolderRec();
            var consulted = new List<string>();
            var root = Dec.Reflection.Enumerate(obj, e =>
            {
                consulted.Add(e.Label);
                return e.Label != "vec";
            });

            Assert.AreEqual(2, Child(root, "point").Children.Count);
            AssertLeaf(Child(root, "vec"));
            Assert.AreEqual(new[] { "point", "vec" }, consulted.ToArray());
        }

        [Test]
        public void RecordAsThisFlattens()
        {
            var obj = new AsThisHolder();
            obj.wrapper.payload.alpha = 4;
            obj.wrapper.payload.beta = "b";
            var root = Dec.Reflection.Enumerate(obj);

            var wrapper = Child(root, "wrapper");
            Assert.AreEqual(typeof(AsThisWrapper), wrapper.DeclaredType);
            Assert.AreSame(obj.wrapper, wrapper.Value);
            Assert.AreEqual(new[] { "alpha", "beta" }, wrapper.Children.Select(c => c.Label).ToArray());
            Assert.AreEqual(4, Child(wrapper, "alpha").Value);
            Assert.AreEqual("b", Child(wrapper, "beta").Value);
        }

        [Test]
        public void RecordAsThisInSharedContainer()
        {
            // A struct recordable in a .Shared() container whose body is RecordAsThis(ref classField): the file inlines the payload, so the entry must descend, not leaf.
            var obj = new AsThisSharedContainerRec() { items = new[] { new AsThisStruct() { payload = new AsThisPayload() { alpha = 9 } } } };
            var root = Dec.Reflection.Enumerate(obj);

            var items = Child(root, "items");
            Assert.IsTrue(items.Shared);
            Assert.AreEqual(1, items.Children.Count);

            var element = items.Children[0];
            Assert.AreEqual(typeof(AsThisStruct), element.DeclaredType);
            Assert.IsFalse(element.Shared);
            Assert.AreEqual(9, Child(element, "alpha").Value);
        }

        [Test]
        public void RecordAsThisContainer()
        {
            // RecordAsThis on a container flattens the elements to the wrapper's level, and they stay writable.
            var obj = new AsThisListHolder();
            var root = Dec.Reflection.Enumerate(obj);

            var wrapper = Child(root, "wrapper");
            Assert.AreEqual(typeof(AsThisListWrapper), wrapper.DeclaredType);
            Assert.AreEqual(new[] { "0", "1", "2" }, wrapper.Children.Select(c => c.Label).ToArray());
            Assert.AreEqual(new object[] { 1, 2, 3 }, wrapper.Children.Select(c => c.Value).ToArray());
            Assert.IsTrue(wrapper.Children[0].Writable);
        }

        [Test]
        public void SharedContainerRepeat()
        {
            // A repeat encounter of a shared container is a leaf: the file contains a bare ref there, and unconditional descent could not terminate on self-containing containers. Only the first occurrence carries children.
            var arr = new[] { new ConditionRec() { id = 1 } };
            var obj = new SharedContainerTwiceRec() { arrA = arr, arrB = arr };
            var root = Dec.Reflection.Enumerate(obj);

            var arrA = Child(root, "arrA");
            Assert.IsTrue(arrA.Shared);
            Assert.AreEqual(1, arrA.Children.Count);

            var arrB = Child(root, "arrB");
            Assert.IsTrue(arrB.Shared);
            Assert.AreSame(arr, arrB.Value);
            AssertLeaf(arrB);
        }

        [Test]
        public void CombinatorShapeFileCrossCheck()
        {
            // An element referenced from two shared positions must actually emit a ref in the file, matching the Shared=true entries.
            var c1 = new ConditionRec() { id = 1 };
            var obj = new CombinatorCrossRec() { conditions = new[] { c1, new ConditionRec() { id = 2 } }, first = c1 };

            var root = Dec.Reflection.Enumerate(obj);
            Assert.IsTrue(Child(root, "conditions").Children[0].Shared);
            Assert.IsTrue(Child(root, "first").Shared);
            AssertLeaf(Child(root, "first"));

            var doc = XDocument.Parse(Dec.Recorder.Write(obj));
            Assert.IsTrue(doc.Descendants("Ref").Any(), "expected a <Ref> block in the serialized output");
            Assert.IsTrue(doc.Descendants().Any(e => e.Attribute("ref") != null), "expected a ref attribute in the serialized output");
        }

        [Test]
        public void SubclassInBaseField()
        {
            var obj = new PolymorphicHolder();
            var root = Dec.Reflection.Enumerate(obj);

            var item = Child(root, "item");
            Assert.AreEqual(typeof(BaseRec), item.DeclaredType);
            Assert.IsInstanceOf<DerivedRec>(item.Value);
            Assert.AreEqual(1, Child(item, "baseVal").Value);
            Assert.AreEqual(2, Child(item, "derivedVal").Value);
        }

        [Test]
        public void ReadOnlyShapes()
        {
            var obj = new ReadOnlyShapesRec();
            var root = Dec.Reflection.Enumerate(obj);

            var dict = Child(root, "dict");
            Assert.IsTrue(dict.Writable);
            Assert.AreEqual(2, dict.Children.Count);
            var pair = dict.Children.Single(c => c.Label == "k1");
            Assert.IsFalse(pair.Writable);
            Assert.AreEqual(typeof(KeyValuePair<string, int>), pair.DeclaredType);
            var key = Child(pair, "key");
            Assert.AreEqual("k1", key.Value);
            Assert.AreEqual(typeof(string), key.DeclaredType);
            Assert.IsFalse(key.Writable);
            var value = Child(pair, "value");
            Assert.AreEqual(1, value.Value);
            Assert.AreEqual(typeof(int), value.DeclaredType);
            Assert.IsFalse(value.Writable);

            var set = Child(root, "set");
            Assert.IsTrue(set.Writable);
            Assert.AreEqual(1, set.Children.Count);
            Assert.AreEqual(5, set.Children[0].Value);
            Assert.IsFalse(set.Children[0].Writable);

            var grid = Child(root, "grid");
            Assert.IsTrue(grid.Writable);
            Assert.AreEqual(2, grid.Children.Count);
            Assert.IsFalse(grid.Children[0].Writable);
            Assert.AreEqual(2, grid.Children[0].Children.Count);
            Assert.AreEqual(1, grid.Children[0].Children[0].Value);
            Assert.AreEqual(4, grid.Children[1].Children[1].Value);
            Assert.IsFalse(grid.Children[1].Children[1].Writable);

            var tuple = Child(root, "tuple");
            Assert.IsTrue(tuple.Writable);
            Assert.AreEqual(2, tuple.Children.Count);
            Assert.AreEqual(7, tuple.Children[0].Value);
            Assert.AreEqual("t", tuple.Children[1].Value);
            Assert.IsFalse(tuple.Children[0].Writable);
        }

        [Test]
        public void BespokeKeyTypeDict()
        {
            var obj = new BespokeRec();
            var root = Dec.Reflection.Enumerate(obj);

            // the bespoke flag is read-side-only; composition is an ordinary dictionary
            var dict = Child(root, "dict");
            Assert.AreEqual(1, dict.Children.Count);
            var pair = dict.Children[0];
            Assert.AreEqual(typeof(Type), Child(pair, "key").DeclaredType);
            Assert.IsFalse(Child(pair, "key").Shared);
            Assert.AreEqual(9, ((StubRecordableInt)Child(pair, "value").Value).data);

            // the settability derivation must recurse through the unsettable segment: a recordable's fields beneath a dictionary value are read-only
            Assert.IsFalse(Child(Child(pair, "value"), "dataRecorded").Writable);
        }

        [Test]
        public void ConditionalRecordable()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(CondRecConverter) } });

            var obj = new CondHolder();

            // included: descends into the Record body
            var rootIncluded = Dec.Reflection.Enumerate(obj);
            var condIncluded = Child(rootIncluded, "cond");
            Assert.AreEqual(5, Child(condIncluded, "data").Value);

            // excluded: falls through to the converter, exactly as a real write does
            var rootExcluded = Dec.Reflection.Enumerate(obj, userSettings: new ExcludeSettings());
            var condExcluded = Child(rootExcluded, "cond");
            AssertLeaf(condExcluded);

            // cross-check against real writes
            StringAssert.Contains("data", Dec.Recorder.Write(obj));
            StringAssert.Contains("cond:5", Dec.Recorder.Write(obj, userSettings: new ExcludeSettings()));
        }

        [Test]
        public void SharedSelfReference()
        {
            var obj = new SelfRec();
            obj.self = obj;

            var root = Dec.Reflection.Enumerate(obj);
            var self = Child(root, "self");
            Assert.IsTrue(self.Shared);
            Assert.AreSame(obj, self.Value);
            AssertLeaf(self);
        }

        [Test]
        public void UnsharedCycle()
        {
            var a = new CycleRec();
            var b = new CycleRec();
            a.next = b;
            b.next = a;

            // the real writer errors on this shape too; introspection must not silently diverge or spin to the depth cap
            ExpectErrors(() => Dec.Reflection.Enumerate(a), str => str.Contains("previously-seen"));
        }

        [Test]
        public void MultiplicityUnsharedTwice()
        {
            var shared = new StubRecordableInt();
            var obj = new MultiplicityRec() { x = shared, y = shared, shareY = false };

            ExpectErrors(() => Dec.Reflection.Enumerate(obj), str => str.Contains("previously-seen"));
        }

        [Test]
        public void MultiplicityMismatch()
        {
            var shared = new StubRecordableInt();
            var obj = new MultiplicityRec() { x = shared, y = shared, shareY = true };

            ExpectErrors(() => Dec.Reflection.Enumerate(obj), str => str.Contains("previously-seen"));
        }

        [Test]
        public void RootErrors()
        {
            ExpectErrors(() => Assert.IsNull(Dec.Reflection.Enumerate<ScalarsRec>(null)), str => true);
        }

        [Test]
        public void RootUnsupported()
        {
            // a type nothing in the record system knows how to compose is the shared pipeline's error, exactly as on a real write
            ExpectErrors(() => Dec.Reflection.Enumerate(new PlainPoco()), str => str.Contains("composition method"));
        }

        [Test]
        public void RootList()
        {
            var list = new List<int>() { 1, 2, 3 };
            var root = Dec.Reflection.Enumerate(list);

            Assert.AreEqual(typeof(List<int>), root.DeclaredType);
            Assert.AreSame(list, root.Value);
            Assert.IsFalse(root.Writable);
            Assert.AreEqual(3, root.Children.Count);
            Assert.AreEqual(2, root.Children[1].Value);
            Assert.IsTrue(root.Children[1].Writable);
            Assert.AreEqual(new Dec.PathIndex(root.Path, 1), root.Children[1].Path);
        }

        [Test]
        public void RootPrimitive()
        {
            var root = Dec.Reflection.Enumerate(5);
            Assert.AreEqual(typeof(int), root.DeclaredType);
            Assert.AreEqual(5, root.Value);
            AssertLeaf(root);
        }

        [Test]
        public void RootConverterRecord()
        {
            RegisterConverters();

            var root = Dec.Reflection.Enumerate(new ConvRecPoint() { x = 1, y = 2 });
            Assert.AreEqual(typeof(ConvRecPoint), root.DeclaredType);
            Assert.AreEqual(2, Child(root, "y").Value);
            Assert.IsTrue(Child(root, "y").Writable);
        }

        [Test]
        public void RootDeclaredAsObject()
        {
            // the root mirrors Recorder.Write<T>: the declared type is T, and the structure beneath is the same
            var obj = new ScalarsRec();
            var concrete = Dec.Reflection.Enumerate(obj);
            var asObject = Dec.Reflection.Enumerate<object>(obj);

            Assert.AreEqual(typeof(ScalarsRec), concrete.DeclaredType);
            Assert.AreEqual(typeof(object), asObject.DeclaredType);
            Assert.AreEqual(concrete.Children.Count, asObject.Children.Count);
            for (int i = 0; i < concrete.Children.Count; ++i)
            {
                AssertTreesEqual(concrete.Children[i], asObject.Children[i]);
            }
        }

        [Test]
        public void RootValueType()
        {
            // a struct root is boxed on the way in, so nothing beneath it is settable
            var root = Dec.Reflection.Enumerate(new OptionStruct() { text = "t" });
            Assert.AreEqual(typeof(OptionStruct), root.DeclaredType);
            Assert.AreEqual("t", Child(root, "text").Value);
            Assert.IsFalse(Child(root, "text").Writable);
            Assert.IsFalse(Child(root, "node").Writable);
        }

        [Test]
        public void Determinism()
        {
            var obj = new OptionsRec() { options = new[] { new OptionStruct() { text = "one", node = new StubRecordableInt() } } };

            var first = Dec.Reflection.Enumerate(obj);
            var second = Dec.Reflection.Enumerate(obj);
            AssertTreesEqual(first, second);
        }

        [Test]
        public void ShouldDescendPredicate()
        {
            var obj = new TrimRec();
            var consulted = new List<string>();

            var root = Dec.Reflection.Enumerate(obj, e =>
            {
                consulted.Add(e.Label);
                return e.Label != "trim";
            });

            Assert.AreEqual(1, Child(root, "keep").Children.Count);
            AssertLeaf(Child(root, "trim"));
            AssertLeaf(Child(root, "scalar"));

            // consulted only for descendable non-root entries; never the root, never natural leaves
            Assert.AreEqual(new[] { "keep", "trim" }, consulted.ToArray());
        }

        // ==== Dec roots ====

        [Test]
        public void DecRoot()
        {
            var dec = ParseIntrospectDecs();
            var root = Dec.Reflection.Enumerate(dec);

            // a Dec composes by reflection: every serializable field in declaration order, derived before base, nothing ever a reference
            Assert.AreEqual(typeof(IntrospectDec), root.DeclaredType);
            Assert.IsInstanceOf<Dec.PathDec>(root.Path);
            Assert.IsFalse(root.Writable);
            Assert.AreEqual(new[] { "value", "text", "other", "plain", "plainAlias", "strukt", "rec", "list", "fixedValue", "hidden" }, root.Children.Select(c => c.Label).ToArray());
            Assert.IsTrue(root.Children.All(c => c.Writable));
            AssertNoneShared(root);

            Assert.AreEqual(3, Child(root, "value").Value);
            Assert.AreEqual("hello", Child(root, "text").Value);

            // a Dec-typed field is a reference leaf
            var other = Child(root, "other");
            Assert.AreSame(Dec.Database<IntrospectDec>.Get("B"), other.Value);
            AssertLeaf(other);

            // a plain class descends by reflection with writable children
            var plain = Child(root, "plain");
            Assert.AreEqual(typeof(IntrospectPlain), plain.DeclaredType);
            Assert.AreEqual(5, Child(plain, "p").Value);
            Assert.IsTrue(Child(plain, "q").Writable);
            Assert.AreEqual(new Dec.PathMember(plain.Path, "q"), Child(plain, "q").Path);

            Assert.IsNull(Child(root, "plainAlias").Value);
            Assert.AreEqual(1.5f, Child(Child(root, "strukt"), "t").Value);
            Assert.AreEqual(9, Child(Child(root, "rec"), "dataRecorded").Value);
            Assert.AreEqual(2, Child(root, "list").Children.Count);
            Assert.AreEqual(7, Child(root, "fixedValue").Value);
            Assert.AreEqual(4, Child(root, "hidden").Value);

            var xml = new Dec.Composer().ComposeXml(false);
            StringAssert.Contains("<value>3</value>", xml);
            StringAssert.Contains("<p>5</p>", xml);
            StringAssert.Contains("<dataRecorded>9</dataRecorded>", xml);
            StringAssert.Contains("<hidden>4</hidden>", xml);
        }

        [Test]
        public void DecRootRecordable()
        {
            ParseIntrospectDecs();
            var root = Dec.Reflection.Enumerate(Dec.Database<IntrospectRecordableDec>.Get("R"));

            // a Dec implementing IRecordable composes through Record(), so labels win over field names
            Assert.AreEqual(new[] { "label" }, root.Children.Select(c => c.Label).ToArray());
            Assert.AreEqual(8, Child(root, "label").Value);
        }

        [Test]
        public void DecRootDeclaredAsBase()
        {
            var dec = ParseIntrospectDecs();
            var concrete = Dec.Reflection.Enumerate(dec);
            var asBase = Dec.Reflection.Enumerate<Dec.Dec>(dec);

            Assert.AreEqual(typeof(Dec.Dec), asBase.DeclaredType);
            Assert.AreEqual(concrete.Children.Count, asBase.Children.Count);
            for (int i = 0; i < concrete.Children.Count; ++i)
            {
                AssertTreesEqual(concrete.Children[i], asBase.Children[i]);
            }
        }

        [Test]
        public void DecRootSharedInline()
        {
            ParseIntrospectDecs();
            var root = Dec.Reflection.Enumerate(Dec.Database<IntrospectSharedDec>.Get("S"));

            // the composer has no reference system, so a .Shared() position inside a Dec is inlined and descends
            var payload = Child(Child(root, "holder"), "payload");
            Assert.IsFalse(payload.Shared);
            Assert.AreEqual(6, Child(payload, "dataRecorded").Value);
            Assert.IsTrue(Child(payload, "dataRecorded").Writable);
        }

        [Test]
        public void DecRootAliasing()
        {
            var decA = ParseIntrospectDecs();
            var decB = Dec.Database<IntrospectDec>.Get("B");
            decA.plainAlias = decA.plain;
            decB.plain = decA.plain;

            // an aliased plain object enumerates in full at every position, and a write through any of them shows at all of them
            var rootA = Dec.Reflection.Enumerate(decA);
            Assert.AreEqual(2, Child(rootA, "plain").Children.Count);
            Assert.AreEqual(2, Child(rootA, "plainAlias").Children.Count);
            Assert.IsTrue(Child(Child(rootA, "plainAlias"), "p").Writable);

            Dec.Reflection.SetByPath(decA, Child(Child(rootA, "plain"), "p").Path, 42);
            Assert.AreEqual(42, Child(Child(Dec.Reflection.Enumerate(decA), "plainAlias"), "p").Value);
            Assert.AreEqual(42, Child(Child(Dec.Reflection.Enumerate(decB), "plain"), "p").Value);
        }

        [Test]
        public void DecRootContainerCycle()
        {
            ParseIntrospectDecs();
            var dec = Dec.Database<IntrospectCycleDec>.Get("C");
            dec.loop = new List<object>();
            dec.loop.Add(dec.loop);

            // compose mode has no reference system to terminate a cycle, so the depth cap must
            ExpectErrors(() => Dec.Reflection.Enumerate(dec), str => str.Contains("Depth limiter"));
        }

        [Test]
        public void SetDecRoot()
        {
            var dec = ParseIntrospectDecs();
            var root = Dec.Reflection.Enumerate(dec);

            Dec.Reflection.SetByPath(dec, Child(root, "value").Path, 30);
            Assert.AreEqual(30, dec.value);

            Dec.Reflection.SetByPath(dec, Child(Child(root, "plain"), "q").Path, "six");
            Assert.AreEqual("six", dec.plain.q);

            // struct field: the boxed write-back through reflection
            Dec.Reflection.SetByPath(dec, Child(Child(root, "strukt"), "t").Path, 2.5f);
            Assert.AreEqual(2.5f, dec.strukt.t);
            Assert.AreEqual(2, dec.strukt.s);

            Dec.Reflection.SetByPath(dec, Child(Child(root, "rec"), "dataRecorded").Path, 90);
            Assert.AreEqual(90, dec.rec.data);

            Dec.Reflection.SetByPath(dec, Child(root, "other").Path, dec);
            Assert.AreSame(dec, dec.other);

            Dec.Reflection.SetByPath(dec, Child(root, "list").Path, new List<int>() { 7 });
            Assert.AreEqual(new[] { 7 }, dec.list.ToArray());

            // readonly and base-private fields are composed, so they are settable too
            Dec.Reflection.SetByPath(dec, Child(root, "fixedValue").Path, 70);
            Assert.AreEqual(70, dec.fixedValue);
            Dec.Reflection.SetByPath(dec, Child(root, "hidden").Path, 40);
            Assert.AreEqual(40, dec.Hidden());

            var xml = new Dec.Composer().ComposeXml(false);
            StringAssert.Contains("<value>30</value>", xml);
            StringAssert.Contains("<q>six</q>", xml);
            StringAssert.Contains("<t>2.5</t>", xml);
            StringAssert.Contains("<dataRecorded>90</dataRecorded>", xml);
            StringAssert.Contains("<other>A</other>", xml);
            StringAssert.Contains("<hidden>40</hidden>", xml);
        }

        [Test]
        public void SetDecFailures()
        {
            var dec = ParseIntrospectDecs();
            var root = Dec.Reflection.Enumerate(dec);

            // a Dec-typed field is a reference leaf mid-path, whatever the root
            ExpectErrors(() => Dec.Reflection.SetByPath(dec, new Dec.PathMember(Child(root, "other").Path, "value"), 1), str => str.Contains("Dec"));

            // a label no field carries
            ExpectErrors(() => Dec.Reflection.SetByPath(dec, new Dec.PathMember(root.Path, "nope"), 1), str => str.Contains("could not resolve"));

            // the field walk stops at leaves: a Dec's int and string fields have no interior to reflect into
            ExpectErrors(() => Dec.Reflection.SetByPath(dec, new Dec.PathMember(Child(root, "value").Path, "deeper"), 1), str => str.Contains("no addressable interior"));
            ExpectErrors(() => Dec.Reflection.SetByPath(dec, new Dec.PathMember(Child(root, "text").Path, "deeper"), 1), str => str.Contains("no addressable interior"));
            Assert.AreEqual(3, dec.value);
            Assert.AreEqual("hello", dec.text);

            // reflection stays refused under a recorder root: a plain class there has no interior
            var holder = new PlainHolderRec();
            ExpectErrors(() => Dec.Reflection.SetByPath(holder, new Dec.PathMember(new Dec.PathMember(new Dec.PathRoot("R"), "plain"), "p"), 1), str => str.Contains("no addressable interior"));
        }

        // ==== SetByPath ====

        [Test]
        public void SetScalar()
        {
            var obj = new ScalarsRec();
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(root, "intField").Path, 99);
            Assert.AreEqual(99, obj.intField);
            StringAssert.Contains("99", Dec.Recorder.Write(obj));

            Dec.Reflection.SetByPath(obj, Child(root, "stringField").Path, "replaced");
            Assert.AreEqual("replaced", obj.stringField);
        }

        [Test]
        public void SetManualPath()
        {
            // a path reconstructed from scratch works regardless of its root
            var obj = new ScalarsRec();
            var path = new Dec.PathMember(new Dec.PathRoot("whatever"), "intField");

            Dec.Reflection.SetByPath(obj, path, 123);
            Assert.AreEqual(123, obj.intField);
        }

        [Test]
        public void SetArrayElement()
        {
            var obj = new ArraysRec();
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(root, "numbers").Children[1].Path, 77);
            Assert.AreEqual(new[] { 10, 77, 30 }, obj.numbers);
        }

        [Test]
        public void SetListElement()
        {
            var obj = new ListRec();
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(root, "strings").Children[0].Path, "gamma");
            Assert.AreEqual(new List<string>() { "gamma", "beta" }, obj.strings);
        }

        [Test]
        public void SetNestedRecordableField()
        {
            var obj = new NestedRec();
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(Child(root, "inner"), "dataRecorded").Path, 55);
            Assert.AreEqual(55, obj.inner.data);
        }

        [Test]
        public void SetStructElementField()
        {
            // the boxed-struct write-back: mutate a field of a struct element inside an array
            var obj = new OptionsRec() { options = new[] { new OptionStruct() { text = "one" }, new OptionStruct() { text = "two" } } };
            var root = Dec.Reflection.Enumerate(obj);

            var textPath = Child(Child(root, "options").Children[1], "text").Path;
            Dec.Reflection.SetByPath(obj, textPath, "rewritten");
            Assert.AreEqual("rewritten", obj.options[1].text);
            Assert.AreEqual("one", obj.options[0].text);
            StringAssert.Contains("rewritten", Dec.Recorder.Write(obj));
        }

        [Test]
        public void SetThroughRecordAsThis()
        {
            // top-level asThis
            var wrapper = new AsThisWrapper();
            var wrapperRoot = Dec.Reflection.Enumerate(wrapper);
            Dec.Reflection.SetByPath(wrapper, Child(wrapperRoot, "alpha").Path, 21);
            Assert.AreEqual(21, wrapper.payload.alpha);

            // nested asThis
            var holder = new AsThisHolder();
            var holderRoot = Dec.Reflection.Enumerate(holder);
            Dec.Reflection.SetByPath(holder, Child(Child(holderRoot, "wrapper"), "alpha").Path, 34);
            Assert.AreEqual(34, holder.wrapper.payload.alpha);

            // asThis on a struct element in a shared container
            var container = new AsThisSharedContainerRec() { items = new[] { new AsThisStruct() { payload = new AsThisPayload() } } };
            var containerRoot = Dec.Reflection.Enumerate(container);
            Dec.Reflection.SetByPath(container, Child(Child(containerRoot, "items").Children[0], "alpha").Path, 55);
            Assert.AreEqual(55, container.items[0].payload.alpha);

            // asThis on a container: elements flattened to the wrapper's level, nested and as the root
            var listHolder = new AsThisListHolder();
            var listHolderRoot = Dec.Reflection.Enumerate(listHolder);
            Dec.Reflection.SetByPath(listHolder, Child(listHolderRoot, "wrapper").Children[1].Path, 9);
            Assert.AreEqual(new List<int>() { 1, 9, 3 }, listHolder.wrapper.items);

            var listWrapper = new AsThisListWrapper();
            var listWrapperRoot = Dec.Reflection.Enumerate(listWrapper);
            Dec.Reflection.SetByPath(listWrapper, listWrapperRoot.Children[2].Path, 30);
            Assert.AreEqual(new List<int>() { 1, 2, 30 }, listWrapper.items);
        }

        [Test]
        public void SetSharedRef()
        {
            var shared = new StubRecordableInt() { data = 1 };
            var obj = new SharedRefsRec() { a = shared, b = shared };
            var root = Dec.Reflection.Enumerate(obj);

            var replacement = new StubRecordableInt() { data = 2 };
            Dec.Reflection.SetByPath(obj, Child(root, "a").Path, replacement);
            Assert.AreSame(replacement, obj.a);
            Assert.AreSame(shared, obj.b);
        }

        [Test]
        public void SetWholeArray()
        {
            var obj = new ArraysRec();
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(root, "numbers").Path, new int[] { 5 });
            Assert.AreEqual(new[] { 5 }, obj.numbers);

            // populate a null container, the NormalizeNullArrayFields replacement
            Dec.Reflection.SetByPath(obj, Child(root, "nullArray").Path, new int[0]);
            Assert.AreEqual(new int[0], obj.nullArray);
        }

        [Test]
        public void SetNull()
        {
            var obj = new NestedRec();
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(root, "inner").Path, null);
            Assert.IsNull(obj.inner);
        }

        [Test]
        public void SetNullable()
        {
            var obj = new NullableRec();
            var root = Dec.Reflection.Enumerate(obj);
            var path = Child(root, "maybe").Path;

            Dec.Reflection.SetByPath(obj, path, 5);
            Assert.AreEqual(5, obj.maybe);

            Dec.Reflection.SetByPath(obj, path, null);
            Assert.IsNull(obj.maybe);
        }

        [Test]
        public void SetFailures()
        {
            var scalars = new ScalarsRec();
            var scalarsRoot = Dec.Reflection.Enumerate(scalars);
            var arrays = new ArraysRec();
            var arraysRoot = Dec.Reflection.Enumerate(arrays);

            // nonexistent label
            ExpectErrors(() => Dec.Reflection.SetByPath(scalars, new Dec.PathMember(new Dec.PathRoot("R"), "nope"), 1), str => str.Contains("could not resolve"));

            // index out of range
            ExpectErrors(() => Dec.Reflection.SetByPath(arrays, new Dec.PathIndex(Child(arraysRoot, "numbers").Path, 10), 1), str => str.Contains("could not resolve"));

            // descent into null
            ExpectErrors(() => Dec.Reflection.SetByPath(arrays, new Dec.PathIndex(Child(arraysRoot, "nullArray").Path, 0), 1), str => str.Contains("null value"));

            // type mismatch
            ExpectErrors(() => Dec.Reflection.SetByPath(scalars, Child(scalarsRoot, "intField").Path, "not an int"), str => str.Contains("cannot assign"));
            Assert.AreEqual(42, scalars.intField);

            // path deeper than structure
            ExpectErrors(() => Dec.Reflection.SetByPath(scalars, new Dec.PathMember(Child(scalarsRoot, "intField").Path, "deeper"), 1), str => str.Contains("no addressable interior"));

            // positions the compose pipeline settles as leaves before any dispatch: a byte[] interior, and a Dec's interior
            ExpectErrors(() => Dec.Reflection.SetByPath(arrays, new Dec.PathIndex(Child(arraysRoot, "bytes").Path, 1), (byte)9), str => str.Contains("byte[]"));
            Assert.AreEqual(2, arrays.bytes[1]);

            scalars.decField = new StubDec();
            ExpectErrors(() => Dec.Reflection.SetByPath(scalars, new Dec.PathMember(Child(scalarsRoot, "decField").Path, "x"), 1), str => str.Contains("Dec"));

            // read-only position: a dictionary value's own path
            var shapes = new ReadOnlyShapesRec();
            var shapesRoot = Dec.Reflection.Enumerate(shapes);
            var dictValue = Child(Child(shapesRoot, "dict").Children.Single(c => c.Label == "k1"), "value");
            ExpectErrors(() => Dec.Reflection.SetByPath(shapes, dictValue.Path, 3), str => str.Contains("read-only"));

            // read-only position: a queue element's path, refused up front by the settability scan despite serializing like an index
            var queueHolder = new QueueStackRec();
            queueHolder.queue.Enqueue(1);
            var queueRoot = Dec.Reflection.Enumerate(queueHolder);
            ExpectErrors(() => Dec.Reflection.SetByPath(queueHolder, Child(queueRoot, "queue").Children[0].Path, 5), str => str.Contains("read-only"));
        }

        [Test]
        public void SetTupleShapedRecordable()
        {
            var obj = new TupleShapedRec();
            var root = Dec.Reflection.Enumerate(obj);
            var inner = Child(root, "inner");
            Assert.IsTrue(inner.Writable);

            Dec.Reflection.SetByPath(obj, inner.Path, 8);
            Assert.AreEqual(8, obj.inner);
        }

        [Test]
        public void SetContainerRefusals()
        {
            // Enumerate never hands out a settable path into these containers, so each is reached with a hand-built index over the container's own path; the refusal has to come from the container dispatch itself.
            var shapes = new ReadOnlyShapesRec();
            var shapesRoot = Dec.Reflection.Enumerate(shapes);
            ExpectErrors(() => Dec.Reflection.SetByPath(shapes, new Dec.PathIndex(Child(shapesRoot, "grid").Path, 0), 9), str => str.Contains("multidimensional"));
            ExpectErrors(() => Dec.Reflection.SetByPath(shapes, new Dec.PathIndex(Child(shapesRoot, "tuple").Path, 0), 9), str => str.Contains("tuple"));
            ExpectErrors(() => Dec.Reflection.SetByPath(shapes, new Dec.PathIndex(Child(shapesRoot, "dict").Path, 0), 9), str => str.Contains("dictionary or set"));
            ExpectErrors(() => Dec.Reflection.SetByPath(shapes, new Dec.PathIndex(Child(shapesRoot, "set").Path, 0), 9), str => str.Contains("dictionary or set"));
            Assert.AreEqual(1, shapes.grid[0, 0]);
            Assert.AreEqual(7, shapes.tuple.Item1);
            Assert.AreEqual(1, shapes.dict["k1"]);
            Assert.IsTrue(shapes.set.Contains(5));

            var queueStack = new QueueStackRec();
            queueStack.queue.Enqueue(1);
            queueStack.stack.Push(2);
            var queueStackRoot = Dec.Reflection.Enumerate(queueStack);
            ExpectErrors(() => Dec.Reflection.SetByPath(queueStack, new Dec.PathIndex(Child(queueStackRoot, "queue").Path, 0), 9), str => str.Contains("Queue"));
            ExpectErrors(() => Dec.Reflection.SetByPath(queueStack, new Dec.PathIndex(Child(queueStackRoot, "stack").Path, 0), 9), str => str.Contains("Stack"));
            Assert.AreEqual(1, queueStack.queue.Peek());
            Assert.AreEqual(2, queueStack.stack.Peek());
        }

        [Test]
        public void SetSuppressedConditional()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(CondRecConverter) } });

            var obj = new CondHolder();
            var root = Dec.Reflection.Enumerate(obj);
            var dataPath = Child(Child(root, "cond"), "data").Path;

            // works under settings that include the body
            Dec.Reflection.SetByPath(obj, dataPath, 8);
            Assert.AreEqual(8, obj.cond.data);

            // refused under settings that suppress it: Enumerate reports no such path there
            ExpectErrors(() => Dec.Reflection.SetByPath(obj, dataPath, 9, userSettings: new ExcludeSettings()), str => str.Contains("suppressed"));
            Assert.AreEqual(8, obj.cond.data);
        }

        [Test]
        public void SetMalformedPath()
        {
            // a segment hand-constructed with a null parent must be a loud error, not a NullReferenceException
            var obj = new ScalarsRec();
            ExpectErrors(() => Dec.Reflection.SetByPath(obj, new Dec.PathMember(null, "intField"), 1), str => str.Contains("null parent"));
            ExpectErrors(() => Dec.Reflection.SetByPath(obj, new Dec.PathMember(new Dec.PathMember(null, "a"), "b"), 1), str => str.Contains("null parent"));
        }

        [Test]
        public void SetRootErrors()
        {
            ExpectErrors(() => Dec.Reflection.SetByPath(null, new Dec.PathMember(new Dec.PathRoot("R"), "x"), 1), str => true);

            // a value-type root arrives boxed, so nothing written beneath it could reach the caller's variable
            var optionStruct = new OptionStruct() { text = "t" };
            var structPath = Child(Dec.Reflection.Enumerate(optionStruct), "text").Path;
            ExpectErrors(() => Dec.Reflection.SetByPath(optionStruct, structPath, "u"), str => str.Contains("value type"));

            // a primitive root is a value type too
            ExpectErrors(() => Dec.Reflection.SetByPath(5, new Dec.PathMember(new Dec.PathRoot("R"), "x"), 1), str => str.Contains("value type"));

            // a reference-type root with no interior
            ExpectErrors(() => Dec.Reflection.SetByPath("text", new Dec.PathMember(new Dec.PathRoot("R"), "x"), 1), str => str.Contains("no addressable interior"));
        }

        [Test]
        public void SetConverterRoot()
        {
            RegisterConverters();

            var point = new ConvRecPoint() { x = 1, y = 2 };
            Dec.Reflection.SetByPath(point, Child(Dec.Reflection.Enumerate(point), "x").Path, 8);
            Assert.AreEqual(8, point.x);

            // a body that replaces its instance has nowhere to deliver the replacement at the root
            var replaced = new ConvRecReplaced() { v = 3 };
            var vPath = Child(Dec.Reflection.Enumerate(replaced), "v").Path;
            ExpectErrors(() => Dec.Reflection.SetByPath(replaced, vPath, 11), str => str.Contains("replacement"));
            Assert.AreEqual(3, replaced.v);
        }

        [Test]
        public void SetListRoot()
        {
            var list = new List<int>() { 1, 2, 3 };
            var listRoot = Dec.Reflection.Enumerate(list);
            Dec.Reflection.SetByPath(list, listRoot.Children[1].Path, 77);
            Assert.AreEqual(new[] { 1, 77, 3 }, list.ToArray());

            var array = new[] { "a", "b" };
            var arrayRoot = Dec.Reflection.Enumerate(array);
            Dec.Reflection.SetByPath(array, arrayRoot.Children[0].Path, "z");
            Assert.AreEqual(new[] { "z", "b" }, array);
        }

        [Test]
        public void SetThroughConverterRecord()
        {
            RegisterConverters();

            var obj = new ConverterRecordHolderRec();
            var root = Dec.Reflection.Enumerate(obj);

            // class-backed: the write lands on the live object
            Dec.Reflection.SetByPath(obj, Child(Child(root, "point"), "y").Path, 20);
            Assert.AreEqual(20, obj.point.y);

            // struct-backed: the value the converter's Record(ref) hands back is written into the holder's field
            Dec.Reflection.SetByPath(obj, Child(Child(root, "vec"), "b").Path, 9.5f);
            Assert.AreEqual(9.5f, obj.vec.b);
            Assert.AreEqual(1.5f, obj.vec.a);

            var xml = Dec.Recorder.Write(obj);
            StringAssert.Contains("<y>20</y>", xml);
            StringAssert.Contains("<b>9.5</b>", xml);
        }

        [Test]
        public void SetThroughConverterRecordReplacingInstance()
        {
            RegisterConverters();

            var obj = new ConverterReplacedHolderRec();
            var original = obj.rep;
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(Child(root, "rep"), "v").Path, 11);
            Assert.AreEqual(11, obj.rep.v);
            Assert.AreNotSame(original, obj.rep);
        }

        [Test]
        public void SetConverterElement()
        {
            RegisterConverters();

            var obj = new ConverterArrayHolderRec();
            var root = Dec.Reflection.Enumerate(obj);

            // struct element of an array: the indexed write-back
            Dec.Reflection.SetByPath(obj, Child(Child(root, "vecs").Children[1], "a").Path, 30f);
            Assert.AreEqual(30f, obj.vecs[1].a);
            Assert.AreEqual(4f, obj.vecs[1].b);
            Assert.AreEqual(1f, obj.vecs[0].a);

            // class element of a list
            Dec.Reflection.SetByPath(obj, Child(Child(root, "pointList").Children[0], "x").Path, 50);
            Assert.AreEqual(50, obj.pointList[0].x);
        }

        [Test]
        public void SetThroughNestedConverter()
        {
            RegisterConverters();

            var obj = new ConverterNestedHolderRec();
            var root = Dec.Reflection.Enumerate(obj);

            Dec.Reflection.SetByPath(obj, Child(Child(Child(root, "outer"), "inner"), "x").Path, 55);
            Assert.AreEqual(55, obj.outer.inner.x);
        }

        [Test]
        public void SetThroughRecordAsThisConverter()
        {
            RegisterConverters();

            var obj = new AsThisConverterRec();
            var root = Dec.Reflection.Enumerate(obj);
            Assert.AreEqual(new[] { "x", "y" }, root.Children.Select(c => c.Label).ToArray());

            Dec.Reflection.SetByPath(obj, Child(root, "y").Path, 90);
            Assert.AreEqual(90, obj.point.y);
        }

        [Test]
        public void SetConverterFailures()
        {
            RegisterConverters();

            // ConverterFactory interiors are refused, matching the read-only entries Enumerate reports
            var factoryHolder = new ConverterFactoryHolderRec();
            var factoryRoot = Dec.Reflection.Enumerate(factoryHolder);
            ExpectErrors(() => Dec.Reflection.SetByPath(factoryHolder, Child(Child(factoryRoot, "node"), "payload").Path, 1), str => str.Contains("ConverterFactory"));
            Assert.AreEqual(7, factoryHolder.node.payload);

            // ConverterString: no interior at all
            var stringHolder = new ConverterHolderRec();
            var stringRoot = Dec.Reflection.Enumerate(stringHolder);
            ExpectErrors(() => Dec.Reflection.SetByPath(stringHolder, new Dec.PathMember(Child(stringRoot, "point").Path, "x"), 1), str => str.Contains("ConverterString"));

            // a label the converter body doesn't record
            var recordHolder = new ConverterRecordHolderRec();
            var recordRoot = Dec.Reflection.Enumerate(recordHolder);
            ExpectErrors(() => Dec.Reflection.SetByPath(recordHolder, new Dec.PathMember(Child(recordRoot, "point").Path, "z"), 1), str => str.Contains("could not resolve"));
        }

        [Test]
        public void SetSuppressedConditionalConverterRecord()
        {
            RegisterConverters();

            var obj = new CondConvHolder();
            var dataPath = Child(Child(Dec.Reflection.Enumerate(obj), "cond"), "data").Path;
            var convertedPath = Child(Child(Dec.Reflection.Enumerate(obj, userSettings: new ExcludeSettings()), "cond"), "converted").Path;

            // each path exists only under the settings it was enumerated with
            Dec.Reflection.SetByPath(obj, dataPath, 8);
            Assert.AreEqual(8, obj.cond.data);

            Dec.Reflection.SetByPath(obj, convertedPath, 9, userSettings: new ExcludeSettings());
            Assert.AreEqual(9, obj.cond.data);

            ExpectErrors(() => Dec.Reflection.SetByPath(obj, dataPath, 10, userSettings: new ExcludeSettings()), str => str.Contains("could not resolve"));
            ExpectErrors(() => Dec.Reflection.SetByPath(obj, convertedPath, 11), str => str.Contains("could not resolve"));
            Assert.AreEqual(9, obj.cond.data);
        }

        [Test]
        public void SetRecordAsThisNull()
        {
            var obj = new AsThisNullRec();
            ExpectErrors(() => Dec.Reflection.SetByPath(obj, new Dec.PathMember(new Dec.PathRoot("R"), "alpha"), 1), str => str.Contains("null value"));
        }

        [Test]
        public void WritableEntriesAccept()
        {
            var decA = ParseIntrospectDecs();

            // every entry Enumerate reports as Writable must accept a SetByPath, and where a distinct value can be written it must be visible to a fresh enumeration, so a write that landed in a discarded copy is caught. Roots whose bodies replace their instance are excluded (SetConverterRoot pins that refusal).
            var node = new StubRecordableInt() { data = 3 };
            var subjects = new object[]
            {
                decA,
                Dec.Database<IntrospectSharedDec>.Get("S"),
                new ConvRecPoint() { x = 1, y = 2 },
                new List<int>() { 1, 2 },
                new int[] { 1, 2 },
                new ScalarsRec(),
                new TypeListRec(),
                new SharedRefsRec() { a = node, b = node },
                new ArraysRec(),
                new ListRec(),
                new QueueStackRec(),
                new CombinatorRec() { conditions = new[] { new ConditionRec() { id = 1 } } },
                new OptionsRec() { options = new[] { new OptionStruct() { text = "one", node = node } } },
                new NestedRec(),
                new ConverterHolderRec(),
                new AsThisHolder(),
                new AsThisListHolder(),
                new AsThisSharedContainerRec() { items = new[] { new AsThisStruct() { payload = new AsThisPayload() } } },
                new PolymorphicHolder(),
                new ReadOnlyShapesRec(),
                new NullableRec(),
                new ConverterRecordHolderRec(),
                new ConverterFactoryHolderRec(),
                new ConverterReplacedHolderRec(),
                new ConverterSharedHolderRec() { points = new[] { new ConvRecPoint() } },
                new ConverterArrayHolderRec(),
                new ConverterNestedHolderRec(),
                new AsThisConverterRec(),
                new CondConvHolder(),
                new TupleShapedRec(),
            };

            foreach (var subject in subjects)
            {
                var root = Dec.Reflection.Enumerate(subject);
                int writable = 0;

                var pending = new Stack<Dec.Reflection.Entry>();
                pending.Push(root);
                while (pending.Count > 0)
                {
                    var entry = pending.Pop();
                    if (entry.Writable)
                    {
                        var sentinel = Sentinel(entry.Value);
                        Dec.Reflection.SetByPath(subject, entry.Path, sentinel ?? entry.Value);
                        ++writable;

                        if (sentinel != null)
                        {
                            var after = FindByPath(Dec.Reflection.Enumerate(subject), entry.Path);
                            Assert.IsNotNull(after, entry.Path.Serialize());
                            Assert.AreEqual(sentinel, after.Value, entry.Path.Serialize());
                        }
                    }

                    foreach (var child in entry.Children)
                    {
                        pending.Push(child);
                    }
                }

                Assert.Greater(writable, 0, subject.GetType().Name);
            }
        }
    }
}
