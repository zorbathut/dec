using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Recorder : Base
    {
        public class PrimitivesRecordable : Dec.IRecordable
        {
            public int intValue;
            public float floatValue;
            public bool boolValue;
            public string stringValue;

            public Type typeValue;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref intValue, "intValue");
                record.Record(ref floatValue, "floatValue");
                record.Record(ref boolValue, "boolValue");
                record.Record(ref stringValue, "stringValue");

                record.Record(ref typeValue, "typeValue");
            }
        }

        [Test]
        public void Primitives([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var primitives = new PrimitivesRecordable();
            primitives.intValue = 42;
            primitives.floatValue = 0.1234f;
            primitives.boolValue = true;
            primitives.stringValue = "<This is a test string value with some XML-sensitive characters.>";
            primitives.typeValue = typeof(Dec.Dec);

            var deserialized = DoRecorderRoundTrip(primitives, mode);

            Assert.AreEqual(primitives.intValue, deserialized.intValue);
            Assert.AreEqual(primitives.floatValue, deserialized.floatValue);
            Assert.AreEqual(primitives.boolValue, deserialized.boolValue);
            Assert.AreEqual(primitives.stringValue, deserialized.stringValue);

            Assert.AreEqual(primitives.typeValue, typeof(Dec.Dec));
        }

        public class EnumRecordable : Dec.IRecordable
        {
            public enum Enum
            {
                Alpha,
                Beta,
                Gamma,
            }

            public Enum alph;
            public Enum bet;
            public Enum gam;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref alph, "alph");
                record.Record(ref bet, "bet");
                record.Record(ref gam, "gam");
            }
        }

        [Test]
        public void Enum([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var enums = new EnumRecordable();
            enums.alph = EnumRecordable.Enum.Alpha;
            enums.bet = EnumRecordable.Enum.Beta;
            enums.gam = EnumRecordable.Enum.Gamma;

            var deserialized = DoRecorderRoundTrip(enums, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains("Alpha"));
                Assert.IsTrue(serialized.Contains("Beta"));
                Assert.IsTrue(serialized.Contains("Gamma"));

                Assert.IsFalse(serialized.Contains("__value"));
            });

            Assert.AreEqual(enums.alph, deserialized.alph);
            Assert.AreEqual(enums.bet, deserialized.bet);
            Assert.AreEqual(enums.gam, deserialized.gam);
        }

        [Test]
        public void Parserless([Values] RecorderMode mode)
        {
            var primitives = new PrimitivesRecordable();
            primitives.intValue = 42;
            primitives.floatValue = 0.1234f;
            primitives.boolValue = true;
            primitives.stringValue = "<This is a test string value with some XML-sensitive characters.>";
            primitives.typeValue = typeof(Dec.Dec);

            var deserialized = DoRecorderRoundTrip(primitives, mode);

            Assert.AreEqual(primitives.intValue, deserialized.intValue);
            Assert.AreEqual(primitives.floatValue, deserialized.floatValue);
            Assert.AreEqual(primitives.boolValue, deserialized.boolValue);
            Assert.AreEqual(primitives.stringValue, deserialized.stringValue);

            Assert.AreEqual(primitives.typeValue, typeof(Dec.Dec));
        }

        [Dec.StaticReferences]
        public static class StaticReferenceDecs
        {
            static StaticReferenceDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDecA;
            public static StubDec TestDecB;
        }
        public class DecRecordable : Dec.IRecordable
        {
            public StubDec a;
            public StubDec b;
            public StubDec empty;
            public StubDec forceEmpty = StaticReferenceDecs.TestDecA;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
                record.Record(ref empty, "empty");
                record.Record(ref forceEmpty, "forceEmpty");
            }
        }

        [Test]
        public void Decs([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) }, explicitStaticRefs = new Type[] { typeof(StaticReferenceDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                    <StubDec decName=""TestDecB"" />
                </Decs>");
            parser.Finish();

            var decs = new DecRecordable();
            decs.a = StaticReferenceDecs.TestDecA;
            decs.b = StaticReferenceDecs.TestDecB;
            // leave empty empty, of course
            decs.forceEmpty = null;

            var deserialized = DoRecorderRoundTrip(decs, mode);

            Assert.AreEqual(decs.a, deserialized.a);
            Assert.AreEqual(decs.b, deserialized.b);
            Assert.AreEqual(decs.empty, deserialized.empty);
            Assert.AreEqual(decs.forceEmpty, deserialized.forceEmpty);
        }

        [Test]
        public void DecsRemoved([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) }, explicitStaticRefs = new Type[] { typeof(StaticReferenceDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                    <StubDec decName=""TestDecB"" />
                </Decs>");
            parser.Finish();

            var decs = new DecRecordable();
            decs.a = StaticReferenceDecs.TestDecA;
            decs.b = StaticReferenceDecs.TestDecB;

            Dec.Database.Delete(StaticReferenceDecs.TestDecA);

            var deserialized = DoRecorderRoundTrip(decs, mode, expectWriteErrors: mode != RecorderMode.Clone, expectReadErrors: mode != RecorderMode.Clone);

            if (mode != RecorderMode.Clone)
            {
                Assert.IsNull(deserialized.a);
            }
            Assert.AreEqual(decs.b, deserialized.b);
        }

        public class ContainersRecordable : Dec.IRecordable
        {
            public List<int> intList = new List<int>();
            public Dictionary<string, string> stringDict = new Dictionary<string, string>();
            public int[] intArray;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref intList, "intList");
                record.Record(ref stringDict, "stringDict");
                record.Record(ref intArray, "intArray");
            }
        }

        [Test]
        public void Containers([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var containers = new ContainersRecordable();
            containers.intList.Add(42);
            containers.intList.Add(1234);
            containers.intList.Add(-105);

            containers.stringDict["Key"] = "Value";
            containers.stringDict["Info"] = "Data";

            containers.intArray = new int[] { 10, 11, 12, 13, 15, 16, 18, 20, 22, 24, 27, 30, 33, 36, 39, 43, 47, 51, 56, 62, 68, 75, 82, 91 };

            var deserialized = DoRecorderRoundTrip(containers, mode);

            Assert.AreEqual(containers.intList, deserialized.intList);
            Assert.AreEqual(containers.stringDict, deserialized.stringDict);
            Assert.AreEqual(containers.intArray, deserialized.intArray);
        }

        public class ContainersNestedRecordable : Dec.IRecordable
        {
            public List<List<int>> intLL = new List<List<int>>();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref intLL, "intLL");
            }
        }

        [Test]
        public void ContainersNested([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var nested = new ContainersNestedRecordable();
            nested.intLL.Add(new List<int>());
            nested.intLL.Add(null);
            nested.intLL.Add(new List<int>());
            nested.intLL.Add(new List<int>());
            nested.intLL[0].Add(42);
            nested.intLL[0].Add(95);
            nested.intLL[2].Add(203);

            var deserialized = DoRecorderRoundTrip(nested, mode);

            Assert.AreEqual(nested.intLL, deserialized.intLL);
        }

        public class Unparseable
        {

        }

        public class MisparseRecordable : Dec.IRecordable
        {
            // amusingly, if this is "null", it works fine, because it just says "well it's null I'll mark as a null, done"
            // I'm not sure I want to guarantee that behavior but I'm also not gonna make it an error, at least for now
            public Unparseable unparseable = new Unparseable();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref unparseable, "unparseable");
            }
        }

        [Test]
        public void Misparse([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            var misparse = new MisparseRecordable();

            var deserialized = DoRecorderRoundTrip(misparse, mode, expectWriteErrors: true, expectReadErrors: true);

            Assert.IsNotNull(deserialized);

            if (mode != RecorderMode.Clone)
            {
                // should just leave this alone
                Assert.IsNotNull(deserialized.unparseable);
            }
        }

        public class RecursiveSquaredRecorder : Dec.IRecordable
        {
            public RecursiveSquaredRecorder left;
            public RecursiveSquaredRecorder right;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref left, "left");
                record.Shared().Record(ref right, "right");
            }
        }

        [Test]
        public void RecursiveSquared([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new RecursiveSquaredRecorder();

            var a = new RecursiveSquaredRecorder();
            var b = new RecursiveSquaredRecorder();
            var c = new RecursiveSquaredRecorder();

            root.left = a;
            root.right = a;

            a.left = b;
            a.right = b;
            b.left = c;
            b.right = c;
            c.left = a;
            c.right = a;

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreSame(deserialized.left, deserialized.right);
            Assert.AreSame(deserialized.left.left, deserialized.right.right);
            Assert.AreSame(deserialized.left.left.left, deserialized.right.right.right);
            Assert.AreSame(deserialized.left.left.left.left, deserialized.right.right.right.right);

            Assert.AreSame(deserialized.left, deserialized.right.right.right.right);

            Assert.AreNotSame(deserialized, deserialized.left);
            Assert.AreNotSame(deserialized, deserialized.left.left);
            Assert.AreNotSame(deserialized, deserialized.left.left.left);
            Assert.AreNotSame(deserialized, deserialized.left.left.left.left);

            Assert.AreNotSame(deserialized.left, deserialized.left.left);
            Assert.AreNotSame(deserialized.left, deserialized.left.left.left);

            Assert.AreNotSame(deserialized.left.left, deserialized.left.left.left);
        }

        [Test]
        public void RecursiveSquaredRoot([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new RecursiveSquaredRecorder();

            var a = new RecursiveSquaredRecorder();
            var b = new RecursiveSquaredRecorder();
            var c = new RecursiveSquaredRecorder();

            root.left = a;
            root.right = a;

            a.left = b;
            a.right = b;
            b.left = c;
            b.right = c;
            c.left = root;
            c.right = root;

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreSame(deserialized.left, deserialized.right);
            Assert.AreSame(deserialized.left.left, deserialized.right.right);
            Assert.AreSame(deserialized.left.left.left, deserialized.right.right.right);
            Assert.AreSame(deserialized.left.left.left.left, deserialized.right.right.right.right);

            Assert.AreSame(deserialized, deserialized.right.right.right.right);

            Assert.AreNotSame(deserialized, deserialized.left);
            Assert.AreNotSame(deserialized, deserialized.left.left);
            Assert.AreNotSame(deserialized, deserialized.left.left.left);

            Assert.AreNotSame(deserialized.left, deserialized.left.left);
            Assert.AreNotSame(deserialized.left, deserialized.left.left.left);
            Assert.AreNotSame(deserialized.left, deserialized.left.left.left.left);

            Assert.AreNotSame(deserialized.left.left, deserialized.left.left.left);
            Assert.AreNotSame(deserialized.left.left.left, deserialized.left.left.left.left);

            Assert.AreNotSame(deserialized.left.left.left, deserialized.left.left.left.left);
        }

        [Test]
        public void RootPrimitive([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            int value = 4;

            // gonna be honest, this feels kind of like overkill
            var deserialized = DoRecorderRoundTrip(value, mode);

            Assert.AreEqual(value, deserialized);
        }

        public class BaseRecordable : Dec.IRecordable
        {
            public int baseVal = 0;

            public virtual void Record(Dec.Recorder record)
            {
                record.Record(ref baseVal, "baseVal");
            }
        }

        public class DerivedRecordable : BaseRecordable
        {
            public int derivedVal = 0;

            public override void Record(Dec.Recorder record)
            {
                base.Record(record);

                record.Record(ref derivedVal, "derivedVal");
            }
        }

        public class DerivedBareRecordable : BaseRecordable
        {

        }

        public class RecordableContainer : Dec.IRecordable
        {
            public BaseRecordable baseContainer;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref baseContainer, "baseContainer");
            }
        }

        [Test]
        public void DerivedRecordables([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new RecordableContainer();
            root.baseContainer = new DerivedRecordable();
            root.baseContainer.baseVal = 42;
            (root.baseContainer as DerivedRecordable).derivedVal = 81;

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(typeof(DerivedRecordable), deserialized.baseContainer.GetType());

            Assert.AreEqual(42, deserialized.baseContainer.baseVal);
            Assert.AreEqual(81, (root.baseContainer as DerivedRecordable).derivedVal);
        }

        [Test]
        public void DerivedBareRecordables([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new RecordableContainer();
            root.baseContainer = new DerivedBareRecordable();
            root.baseContainer.baseVal = 42;

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(typeof(DerivedBareRecordable), deserialized.baseContainer.GetType());

            Assert.AreEqual(42, deserialized.baseContainer.baseVal);
        }

        public class MultiRecordRec : Dec.IRecordable
        {
            public int x;
            public int y;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref x, "x");
                record.Record(ref y, "x");  // oops!
            }
        }

        [Test]
        public void MultiRecord([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var mr = new MultiRecordRec();
            mr.x = 3;
            mr.y = 5;

            var deserialized = DoRecorderRoundTrip(mr, mode, expectWriteErrors: true);

            Assert.AreEqual(mr.x, deserialized.x);
            // y's value is left undefined
        }

        public class PrimitivesContainer : Dec.IRecordable
        {
            public PrimitivesRecordable recordable;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref recordable, "recordable");
            }
        }

        [Test]
        public void Pretty([Values(RecorderMode.Bare, RecorderMode.Pretty)] RecorderMode mode)
        {
            var item = new StubRecordable();

            var output = Dec.Recorder.Write(item, pretty: mode == RecorderMode.Pretty);

            Assert.AreEqual(mode == RecorderMode.Pretty, output.Contains("\n"));
        }

        public class RecordablePrivate : Dec.IRecordable
        {
            internal RecordablePrivate() { }

            public void Record(Dec.Recorder record)
            {

            }
        }

        [Test]
        public void Private([Values] RecorderMode mode)
        {
            var item = new RecordablePrivate();

            var output = DoRecorderRoundTrip(item, mode);

            Assert.IsNotNull(output);
        }

        public class RecordableParameter : Dec.IRecordable
        {
            public RecordableParameter(int x) { }

            public void Record(Dec.Recorder record)
            {

            }
        }

        [Test]
        public void Parameter([Values] RecorderMode mode)
        {
            var item = new RecordableParameter(3);

            var output = DoRecorderRoundTrip(item, mode, expectReadErrors: true);

            Assert.IsNull(output);
        }

        public class StubRecordableIgnoring : Dec.IRecordable
        {
            public void Record(Dec.Recorder record)
            {
                record.Ignore("recordable");
            }
        }

        [Test]
        public void ParameterRef()
        {
            // Turn it into null.

            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""DecTest.Recorder.RecordableParameter"" />
                  </refs>
                  <data>
                    <recordable ref=""ref00000"" />
                  </data>
                </Record>";
            StubRecordableIgnoring deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<StubRecordableIgnoring>(serialized));

            Assert.IsNotNull(deserialized);
        }

        public class IntContainerClass : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref value, "value");
            }
        }

        public struct IntContainerStruct : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref value, "value");
            }
        }

        public class ObjectRefsRecordable : Dec.IRecordable
        {
            public object intRef;
            public object enumRef;
            public object stringRef;
            public object typeRef;
            public object nullRef;
            public object classRef;
            public object structRef;
            public object arrayRef;
            public object listRef;
            public object dictRef;
            public object hashRef;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref intRef, "intRef");
                record.Record(ref enumRef, "enumRef");
                record.Record(ref stringRef, "stringRef");
                record.Record(ref typeRef, "typeRef");
                record.Record(ref nullRef, "nullRef");
                record.Record(ref classRef, "classRef");
                record.Record(ref structRef, "structRef");
                record.Record(ref arrayRef, "arrayRef");
                record.Record(ref listRef, "listRef");
                record.Record(ref dictRef, "dictRef");
                record.Record(ref hashRef, "hashRef");
            }
        }

        [Test]
        public void ObjectRefs([Values] RecorderMode mode)
        {
            var obj = new ObjectRefsRecordable();
            obj.intRef = 42;
            obj.enumRef = GenericEnum.Gamma;
            obj.stringRef = "Hello, I am a string";
            obj.typeRef = typeof(Recorder);
            obj.nullRef = null; // redundant, obviously
            obj.classRef = new IntContainerClass() { value = 10 };
            obj.structRef = new IntContainerStruct() { value = -10 };
            obj.arrayRef = new int[] { 1, 1, 2, 3, 5, 8, 11 };
            obj.listRef = new List<int>() { 2, 3, 5, 7, 11, 13, 17 };
            obj.dictRef = new Dictionary<int, string>() { { 1, "one" }, { 2, "two" }, { 4, "four" }, { 8, "eight" } };
            obj.hashRef = new HashSet<int>() { 1, 6, 21, 107 };

            var deserialized = DoRecorderRoundTrip(obj, mode);

            Assert.AreEqual(obj.intRef, deserialized.intRef);
            Assert.AreEqual(obj.enumRef, deserialized.enumRef);
            Assert.AreEqual(obj.stringRef, deserialized.stringRef);
            Assert.AreEqual(obj.typeRef, deserialized.typeRef);
            Assert.AreEqual(obj.nullRef, deserialized.nullRef);
            Assert.AreEqual(obj.classRef.GetType(), deserialized.classRef.GetType());
            Assert.AreEqual(((IntContainerClass)obj.classRef).value, ((IntContainerClass)deserialized.classRef).value);
            Assert.AreEqual(obj.structRef.GetType(), deserialized.structRef.GetType());
            Assert.AreEqual(((IntContainerStruct)obj.structRef).value, ((IntContainerStruct)deserialized.structRef).value);
            Assert.AreEqual(obj.arrayRef, deserialized.arrayRef);
            Assert.AreEqual(obj.listRef, deserialized.listRef);
            Assert.AreEqual(obj.dictRef, deserialized.dictRef);
            Assert.AreEqual(obj.hashRef, deserialized.hashRef);
        }

        public class StringEmptyNullRecordable : Dec.IRecordable
        {
            public string stringContains;
            public string stringEmpty;
            public string stringNull;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref stringContains, "stringContains");
                record.Record(ref stringEmpty, "stringEmpty");
                record.Record(ref stringNull, "stringNull");
            }
        }

        [Test]
        public void StringEmptyNull([Values] RecorderMode mode)
        {
            var senr = new StringEmptyNullRecordable();
            senr.stringContains = "Contains";
            senr.stringEmpty = "";
            senr.stringNull = null;

            var deserialized = DoRecorderRoundTrip(senr, mode);

            Assert.AreEqual(senr.stringContains, deserialized.stringContains);
            Assert.AreEqual(senr.stringEmpty, deserialized.stringEmpty);
            Assert.AreEqual(senr.stringNull, deserialized.stringNull);
        }

        public class BaseType : Dec.IRecordable
        {
            public void Record(Dec.Recorder recorder)
            {

            }
        }

        public class DerivedType : BaseType
        {

        }

        [Test]
        public void PolymorphicRoot([Values] RecorderMode mode)
        {
            BaseType root = new DerivedType();
            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.GetType(), deserialized.GetType());
        }

        public class SharedArrayDec : Dec.IRecordable
        {
            public StubRecordable[] data_a;
            public StubRecordable[] data_b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref data_a, "data_a");
                recorder.Shared().Record(ref data_b, "data_b");
            }
        }

        // This is specifically hard because arrays require a constructor parameter.
        [Test]
        public void SharedArray([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            var root = new SharedArrayDec();
            root.data_b = root.data_a = new StubRecordable[10];

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.data_a.Length, deserialized.data_a.Length);
            Assert.AreSame(deserialized.data_a, deserialized.data_b);
        }

        public struct NullableStruct : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }
        }

        public class Nullables : Dec.IRecordable
        {
            public int? nullableIntA;
            public int? nullableIntB;
            public float? nullableFloatA;
            public float? nullableFloatB;
            public bool? nullableBoolA;
            public bool? nullableBoolB;
            public NullableStruct nullableStructA;
            public NullableStruct nullableStructB;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref nullableIntA, "nullableIntA");
                recorder.Record(ref nullableIntB, "nullableIntB");
                recorder.Record(ref nullableFloatA, "nullableFloatA");
                recorder.Record(ref nullableFloatB, "nullableFloatB");
                recorder.Record(ref nullableBoolA, "nullableBoolA");
                recorder.Record(ref nullableBoolB, "nullableBoolB");
                recorder.Record(ref nullableStructA, "nullableStructA");
                recorder.Record(ref nullableStructB, "nullableStructB");
            }
        }

        [Test]
        public void Nullable([Values] RecorderMode mode)
        {
            var root = new Nullables();
            root.nullableIntA = 42;
            root.nullableFloatA = 0.1234f;
            root.nullableBoolA = true;
            root.nullableStructA = new NullableStruct { value = 42 };

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(root.nullableIntA, deserialized.nullableIntA);
            Assert.AreEqual(root.nullableIntB, deserialized.nullableIntB);
            Assert.AreEqual(root.nullableFloatA, deserialized.nullableFloatA);
            Assert.AreEqual(root.nullableFloatB, deserialized.nullableFloatB);
            Assert.AreEqual(root.nullableBoolA, deserialized.nullableBoolA);
            Assert.AreEqual(root.nullableBoolB, deserialized.nullableBoolB);
            Assert.AreEqual(root.nullableStructA, deserialized.nullableStructA);
            Assert.AreEqual(root.nullableStructB, deserialized.nullableStructB);
        }
    }
}
