namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) }, explicitStaticRefs = new Type[] { typeof(StaticReferenceDecs) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) }, explicitStaticRefs = new Type[] { typeof(StaticReferenceDecs) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                    <StubDec decName=""TestDecB"" />
                </Decs>");
            parser.Finish();

            var decs = new DecRecordable();
            decs.a = StaticReferenceDecs.TestDecA;
            decs.b = StaticReferenceDecs.TestDecB;

            Dec.Database.Delete(StaticReferenceDecs.TestDecA);

            var deserialized = DoRecorderRoundTrip(decs, mode, expectWriteErrors: true, expectReadErrors: true);

            Assert.IsNull(deserialized.a);
            Assert.AreEqual(decs.b, deserialized.b);
        }


        public class RefsChildRecordable : Dec.IRecordable
        {
            public void Record(Dec.Recorder record)
            {
                // lol
            }
        }

        public class RefsRootRecordable : Dec.IRecordable
        {
            public RefsChildRecordable childAone;
            public RefsChildRecordable childAtwo;
            public RefsChildRecordable childB;
            public RefsChildRecordable childEmpty;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref childAone, "childAone");
                record.Record(ref childAtwo, "childAtwo");
                record.Record(ref childB, "childB");
                record.Record(ref childEmpty, "childEmpty");
            }
        }
        
        [Test]
        public void Refs([Values] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            var refs = new RefsRootRecordable();
            refs.childAone = new RefsChildRecordable();
            refs.childAtwo = refs.childAone;
            refs.childB = new RefsChildRecordable();
            refs.childEmpty = null;

            var deserialized = DoRecorderRoundTrip(refs, mode);

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

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

        public class RecursiveParent : Dec.IRecordable
        {
            public List<RecursiveNode> children = new List<RecursiveNode>();

            public void Record(Dec.Recorder record)
            {
                record.Record(ref children, "children");
            }
        }

        public class RecursiveNode : Dec.IRecordable
        {
            public RecursiveNode childA;
            public RecursiveNode childB;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref childA, "childA");
                record.Record(ref childB, "childB");
            }
        }

        [Test]
        public void ContainerRecursive([Values] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            var parent = new RecursiveParent();
            parent.children.Add(new RecursiveNode());
            parent.children.Add(new RecursiveNode());
            parent.children.Add(new RecursiveNode());

            parent.children[0].childB = parent.children[1];
            parent.children[1].childA = parent.children[0];

            // look on my works, ye mighty, and despair
            parent.children[2].childA = parent.children[2];
            parent.children[2].childB = parent.children[2];

            var deserialized = DoRecorderRoundTrip(parent, mode);

            Assert.IsNull(deserialized.children[0].childA);
            Assert.AreSame(deserialized.children[1], deserialized.children[0].childB);

            Assert.AreSame(deserialized.children[0], deserialized.children[1].childA);
            Assert.IsNull(deserialized.children[1].childB);

            Assert.AreSame(deserialized.children[2], deserialized.children[2].childA);
            Assert.AreSame(deserialized.children[2], deserialized.children[2].childB);

            Assert.AreEqual(3, deserialized.children.Count);
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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            var misparse = new MisparseRecordable();

            var deserialized = DoRecorderRoundTrip(misparse, mode, expectWriteErrors: true, expectReadErrors: true);

            Assert.IsNotNull(deserialized);

            // should just leave this alone
            Assert.IsNotNull(deserialized.unparseable);
        }

        public class RecursiveSquaredRecorder : Dec.IRecordable
        {
            public RecursiveSquaredRecorder left;
            public RecursiveSquaredRecorder right;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref left, "left");
                record.Record(ref right, "right");
            }
        }

        [Test]
        public void RecursiveSquared([Values] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

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
        public void RecursiveSquaredRoot([Values] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            int value = 4;

            // gonna be honest, this feels kind of like overkill
            var deserialized = DoRecorderRoundTrip(value, mode);

            Assert.AreEqual(value, deserialized);
        }

        private class DoubleLinkedRecorder : Dec.IRecordable
        {
            public DoubleLinkedRecorder a;
            public DoubleLinkedRecorder b;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
            }
        }

        [Test]
        public void DepthDoubleLinked([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            // This test verifies that we can write an extremely deep structure without blowing the stack.
            // We use double links so we don't have to worry about generating an absurd xml file in the process.
            // As of this writing, *without* the stack compensation code, 1000 works and 2000 doesn't
            // I'm choosing 10000 because it's well into the Doesn't Work territory, but it also doesn't take forever to run.
            const int depth = 10000;

            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new DoubleLinkedRecorder();

            {
                var current = root;
                
                for (int i = 1; i < depth; ++i)
                {
                    var next = new DoubleLinkedRecorder();
                    current.a = next;
                    current.b = next;
                    current = next;
                }
            }

            var deserialized = DoRecorderRoundTrip(root, mode);

            {
                var seen = new HashSet<DoubleLinkedRecorder>();
                var current = deserialized;
                while (current != null && !seen.Contains(current))
                {
                    Assert.AreEqual(current.a, current.b);
                    seen.Add(current);
                    current = current.a;
                }

                Assert.AreEqual(depth, seen.Count);
            }
        }

        [Test]
        public void DepthSingleLinked([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            // This test verifies that we can serialize and/or read an extremely deep structure without blowing the stack.
            // We use single links so we don't generate refs, we actually embed objects.
            const int depth = 10_000;

            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new DoubleLinkedRecorder();

            {
                var current = root;

                for (int i = 1; i < depth; ++i)
                {
                    var next = new DoubleLinkedRecorder();
                    current.a = next;
                    current = next;
                }
            }

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                // This verifies we haven't done an n^2 monstrosity by letting the depth get too far.
                // With 10_000 items, this generates a 300_000_000 byte file before depth controlling!
                Assert.Less(serialized.Length, 2_000_000);
            });

            {
                var seen = new HashSet<DoubleLinkedRecorder>();
                var current = deserialized;
                while (current != null && !seen.Contains(current))
                {
                    seen.Add(current);
                    current = current.a;
                }

                Assert.AreEqual(depth, seen.Count);
            }
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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new RecordableContainer();
            root.baseContainer = new DerivedRecordable();
            root.baseContainer.baseVal = 42;
            (root.baseContainer as DerivedRecordable).derivedVal = 81;

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(typeof(DerivedRecordable), deserialized.baseContainer.GetType());

            Assert.AreEqual(42, deserialized.baseContainer.baseVal);
            Assert.AreEqual(81, ( root.baseContainer as DerivedRecordable ).derivedVal);
        }

        [Test]
        public void BadRefTag()
        {
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Raf id=""ref00000"" class=""DecTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectWarnings(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized));

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        [Test]
        public void MissingId()
        {
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref class=""DecTest.Recorder.RefsChildRecordable"" />
                    <Ref id=""ref00000"" class=""DecTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized));

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        [Test]
        public void MissingClass()
        {
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""PLACE"" />
                    <Ref id=""ref00000"" class=""DecTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized));

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        struct AStruct { }

        [Test]
        public void RefStruct()
        {
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""PLACE"" class=""DecTest.Recorder.AStruct"" />
                    <Ref id=""ref00000"" class=""DecTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized));

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        [Test]
        public void PointlessRef()
        {
            // This is weird, but right now it's OK.

            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""PLACE"" class=""DecTest.Recorder.RefsChildRecordable"" />
                    <Ref id=""ref00000"" class=""DecTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized);

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        public class AttributeRecordable : Dec.IRecordable
        {
            public string attributing = "";

            public void Record(Dec.Recorder record)
            {
                if (record.Mode == Dec.Recorder.Direction.Read)
                {
                    attributing = record.Xml.Attributes().Single(attr => attr.Name == "converted").Value;
                }
                else
                {
                    record.Xml.SetAttributeValue("converted", attributing);
                }
            }
        }

        public class AttributeHolder : Dec.IRecordable
        {
            public AttributeRecordable a;
            public AttributeRecordable b;
            public AttributeRecordable c;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
                record.Record(ref c, "c");
            }
        }

        [Test]
        public void Attributes([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var holder = new AttributeHolder();

            holder.a = new AttributeRecordable { attributing = "hello_an_attribute" };
            holder.b = new AttributeRecordable { attributing = "<XML-SENSITIVE>" };
            holder.c = new AttributeRecordable { attributing = "I guess I'll write some more text here?" };

            var deserialized = DoRecorderRoundTrip(holder, mode);

            Assert.AreEqual(holder.a.attributing, deserialized.a.attributing);
            Assert.AreEqual(holder.b.attributing, deserialized.b.attributing);
            Assert.AreEqual(holder.c.attributing, deserialized.c.attributing);
        }

        [Test]
        public void AttributeRef([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var holder = new AttributeHolder();

            holder.a = new AttributeRecordable { attributing = "I am being referenced!" };
            holder.b = holder.a;
            holder.c = holder.a;

            var deserialized = DoRecorderRoundTrip(holder, mode);

            Assert.AreEqual(holder.a.attributing, deserialized.a.attributing);
            Assert.AreSame(holder.a, holder.b);
            Assert.AreSame(holder.a, holder.c);
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
        public void NullRef()
        {
            // This is weird and not OK. I've chosen to prioritize the null over the ref, on the theory that there are a lot of ways that broken files can turn into null anyway.

            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""DecTest.Recorder.PrimitivesRecordable"" />
                  </refs>
                  <data>
                    <recordable ref=""ref00000"" null=""true""/>
                  </data>
                </Record>";
            PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<PrimitivesContainer>(serialized));

            Assert.IsNull(deserialized.recordable);
        }

        [Test]
        public void ExtraAttributeRef()
        {
            // Just ignore the extra attribute.

            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""DecTest.Recorder.PrimitivesRecordable"">
                        <intValue>42</intValue>
                    </Ref>
                  </refs>
                  <data>
                    <recordable ref=""ref00000"" garbage=""yup"" />
                  </data>
                </Record>";
            PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<PrimitivesContainer>(serialized));

            Assert.AreEqual(42, deserialized.recordable.intValue);
        }

        [Test]
        public void MissingRef()
        {
            // Turn it into null.

            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>
                    <recordable ref=""ref00000"" />
                  </data>
                </Record>";
            PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<PrimitivesContainer>(serialized));

            Assert.IsNull(deserialized.recordable);
        }

        [Test]
        public void MistypedRef()
        {
            // Turn it into null.

            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""DecTest.Recorder.PrimitivesContainer"" />
                  </refs>
                  <data>
                    <recordable ref=""ref00000"" />
                  </data>
                </Record>";
            PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<PrimitivesContainer>(serialized));

            Assert.IsNull(deserialized.recordable);
        }

        public class DictionaryKeyRefDec : Dec.IRecordable
        {
            public StubRecordable referenceA;
            public Dictionary<StubRecordable, string> dict = new Dictionary<StubRecordable, string>();
            public StubRecordable referenceB;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref referenceA, "referenceA");
                record.Record(ref dict, "dict");
                record.Record(ref referenceB, "referenceB");
            }
        }

        [Test]
        public void DictionaryKeyRef([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var dict = new DictionaryKeyRefDec();
            dict.referenceA = new StubRecordable();
            dict.referenceB = new StubRecordable();
            dict.dict[dict.referenceA] = "Hello";
            dict.dict[dict.referenceB] = "Goodbye";

            var deserialized = DoRecorderRoundTrip(dict, mode);

            Assert.AreNotSame(deserialized.referenceA, deserialized.referenceB);
            Assert.AreEqual("Hello", deserialized.dict[deserialized.referenceA]);
            Assert.AreEqual("Goodbye", deserialized.dict[deserialized.referenceB]);
        }

        [Test]
        public void Pretty([Values(RecorderMode.Bare, RecorderMode.Pretty)] RecorderMode mode)
        {
            var item = new StubRecordable();

            var output = Dec.Recorder.Write(item, pretty: mode == RecorderMode.Pretty);

            Assert.AreEqual(mode == RecorderMode.Pretty, output.Contains("\n"));
        }
    }
}
