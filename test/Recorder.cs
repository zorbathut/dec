namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Recorder : Base
    {
        public class PrimitivesRecordable : Def.IRecordable
        {
            public int intValue;
            public float floatValue;
            public bool boolValue;
            public string stringValue;

            public Type typeValue;

            public void Record(Def.Recorder record)
            {
                record.Record(ref intValue, "intValue");
                record.Record(ref floatValue, "floatValue");
                record.Record(ref boolValue, "boolValue");
                record.Record(ref stringValue, "stringValue");

                record.Record(ref typeValue, "typeValue");
            }
        }

        [Test]
	    public void Primitives()
	    {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            var primitives = new PrimitivesRecordable();
            primitives.intValue = 42;
            primitives.floatValue = 0.1234f;
            primitives.boolValue = true;
            primitives.stringValue = "<This is a test string value with some XML-sensitive characters.>";
            primitives.typeValue = typeof(Def.Def);

            string serialized = Def.Recorder.Write(primitives, pretty: true);
            var deserialized = Def.Recorder.Read<PrimitivesRecordable>(serialized);

            Assert.AreEqual(primitives.intValue, deserialized.intValue);
            Assert.AreEqual(primitives.floatValue, deserialized.floatValue);
            Assert.AreEqual(primitives.boolValue, deserialized.boolValue);
            Assert.AreEqual(primitives.stringValue, deserialized.stringValue);

            Assert.AreEqual(primitives.typeValue, typeof(Def.Def));
        }

        [Test]
        public void Parserless()
        {
            var primitives = new PrimitivesRecordable();
            primitives.intValue = 42;
            primitives.floatValue = 0.1234f;
            primitives.boolValue = true;
            primitives.stringValue = "<This is a test string value with some XML-sensitive characters.>";
            primitives.typeValue = typeof(Def.Def);

            string serialized = Def.Recorder.Write(primitives, pretty: true);
            var deserialized = Def.Recorder.Read<PrimitivesRecordable>(serialized);

            Assert.AreEqual(primitives.intValue, deserialized.intValue);
            Assert.AreEqual(primitives.floatValue, deserialized.floatValue);
            Assert.AreEqual(primitives.boolValue, deserialized.boolValue);
            Assert.AreEqual(primitives.stringValue, deserialized.stringValue);

            Assert.AreEqual(primitives.typeValue, typeof(Def.Def));
        }

        [Def.StaticReferences]
        public static class StaticReferenceDefs
        {
            static StaticReferenceDefs() { Def.StaticReferencesAttribute.Initialized(); }

            public static StubDef TestDefA;
            public static StubDef TestDefB;
        }
        public class DefRecordable : Def.IRecordable
        {
            public StubDef a;
            public StubDef b;
            public StubDef empty;
            public StubDef forceEmpty = StaticReferenceDefs.TestDefA;

            public void Record(Def.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
                record.Record(ref empty, "empty");
                record.Record(ref forceEmpty, "forceEmpty");
            }
        }

        [Test]
        public void Defs()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDef) }, explicitStaticRefs = new Type[] { typeof(StaticReferenceDefs) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>");
            parser.Finish();

            var defs = new DefRecordable();
            defs.a = StaticReferenceDefs.TestDefA;
            defs.b = StaticReferenceDefs.TestDefB;
            // leave empty empty, of course
            defs.forceEmpty = null;

            string serialized = Def.Recorder.Write(defs, pretty: true);
            var deserialized = Def.Recorder.Read<DefRecordable>(serialized);

            Assert.AreEqual(defs.a, deserialized.a);
            Assert.AreEqual(defs.b, deserialized.b);
            Assert.AreEqual(defs.empty, deserialized.empty);
            Assert.AreEqual(defs.forceEmpty, deserialized.forceEmpty);
        }

        [Test]
        public void DefsRemoved()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDef) }, explicitStaticRefs = new Type[] { typeof(StaticReferenceDefs) } };

            var parser = new Def.Parser();
            parser.AddString(@"
                <Defs>
                    <StubDef defName=""TestDefA"" />
                    <StubDef defName=""TestDefB"" />
                </Defs>");
            parser.Finish();

            var defs = new DefRecordable();
            defs.a = StaticReferenceDefs.TestDefA;
            defs.b = StaticReferenceDefs.TestDefB;

            Def.Database.Delete(StaticReferenceDefs.TestDefA);

            string serialized = null;
            ExpectErrors(() => serialized = Def.Recorder.Write(defs, pretty: true));
            DefRecordable deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<DefRecordable>(serialized));

            Assert.IsNull(deserialized.a);
            Assert.AreEqual(defs.b, deserialized.b);
        }


        public class RefsChildRecordable : Def.IRecordable
        {
            public void Record(Def.Recorder record)
            {
                // lol
            }
        }

        public class RefsRootRecordable : Def.IRecordable
        {
            public RefsChildRecordable childAone;
            public RefsChildRecordable childAtwo;
            public RefsChildRecordable childB;
            public RefsChildRecordable childEmpty;

            public void Record(Def.Recorder record)
            {
                record.Record(ref childAone, "childAone");
                record.Record(ref childAtwo, "childAtwo");
                record.Record(ref childB, "childB");
                record.Record(ref childEmpty, "childEmpty");
            }
        }
        
        [Test]
        public void Refs()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            var refs = new RefsRootRecordable();
            refs.childAone = new RefsChildRecordable();
            refs.childAtwo = refs.childAone;
            refs.childB = new RefsChildRecordable();
            refs.childEmpty = null;

            string serialized = Def.Recorder.Write(refs, pretty: true);
            var deserialized = Def.Recorder.Read<RefsRootRecordable>(serialized);

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        public class ContainersRecordable : Def.IRecordable
        {
            public List<int> intList = new List<int>();
            public Dictionary<string, string> stringDict = new Dictionary<string, string>();
            public int[] intArray;

            public void Record(Def.Recorder record)
            {
                record.Record(ref intList, "intList");
                record.Record(ref stringDict, "stringDict");
                record.Record(ref intArray, "intArray");
            }
        }

        [Test]
        public void Containers()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            var containers = new ContainersRecordable();
            containers.intList.Add(42);
            containers.intList.Add(1234);
            containers.intList.Add(-105);

            containers.stringDict["Key"] = "Value";
            containers.stringDict["Info"] = "Data";

            containers.intArray = new int[] { 10, 11, 12, 13, 15, 16, 18, 20, 22, 24, 27, 30, 33, 36, 39, 43, 47, 51, 56, 62, 68, 75, 82, 91 };

            string serialized = Def.Recorder.Write(containers, pretty: true);
            var deserialized = Def.Recorder.Read<ContainersRecordable>(serialized);

            Assert.AreEqual(containers.intList, deserialized.intList);
            Assert.AreEqual(containers.stringDict, deserialized.stringDict);
            Assert.AreEqual(containers.intArray, deserialized.intArray);
        }

        public class ContainersNestedRecordable : Def.IRecordable
        {
            public List<List<int>> intLL = new List<List<int>>();

            public void Record(Def.Recorder record)
            {
                record.Record(ref intLL, "intLL");
            }
        }

        [Test]
        public void ContainersNested()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            var nested = new ContainersNestedRecordable();
            nested.intLL.Add(new List<int>());
            nested.intLL.Add(null);
            nested.intLL.Add(new List<int>());
            nested.intLL.Add(new List<int>());
            nested.intLL[0].Add(42);
            nested.intLL[0].Add(95);
            nested.intLL[2].Add(203);

            string serialized = Def.Recorder.Write(nested, pretty: true);
            var deserialized = Def.Recorder.Read<ContainersNestedRecordable>(serialized);

            Assert.AreEqual(nested.intLL, deserialized.intLL);
        }

        public class RecursiveParent : Def.IRecordable
        {
            public List<RecursiveNode> children = new List<RecursiveNode>();

            public void Record(Def.Recorder record)
            {
                record.Record(ref children, "children");
            }
        }

        public class RecursiveNode : Def.IRecordable
        {
            public RecursiveNode childA;
            public RecursiveNode childB;

            public void Record(Def.Recorder record)
            {
                record.Record(ref childA, "childA");
                record.Record(ref childB, "childB");
            }
        }

        [Test]
        public void ContainerRecursive()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
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

            string serialized = Def.Recorder.Write(parent, pretty: true);
            var deserialized = Def.Recorder.Read<RecursiveParent>(serialized);

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

        public class MisparseRecordable : Def.IRecordable
        {
            // amusingly, if this is "null", it works fine, because it just says "well it's null I'll mark as a null, done"
            // I'm not sure I want to guarantee that behavior but I'm also not gonna make it an error, at least for now
            public Unparseable unparseable = new Unparseable();

            public void Record(Def.Recorder record)
            {
                record.Record(ref unparseable, "unparseable");
            }
        }

        [Test]
        public void Misparse()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            var misparse = new MisparseRecordable();

            string serialized = null;
            ExpectErrors(() => serialized = Def.Recorder.Write(misparse, pretty: true));
            Assert.IsNotNull(serialized);

            MisparseRecordable deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<MisparseRecordable>(serialized));

            Assert.IsNotNull(deserialized);

            // should just leave this alone
            Assert.IsNotNull(deserialized.unparseable);
        }

        public class RecursiveSquaredRecorder : Def.IRecordable
        {
            public RecursiveSquaredRecorder left;
            public RecursiveSquaredRecorder right;

            public void Record(Def.Recorder record)
            {
                record.Record(ref left, "left");
                record.Record(ref right, "right");
            }
        }

        [Test]
        public void RecursiveSquared()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
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

            string serialized = Def.Recorder.Write(root, pretty: true);
            var deserialized = Def.Recorder.Read<RecursiveSquaredRecorder>(serialized);

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
        public void RecursiveSquaredRoot()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
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

            string serialized = Def.Recorder.Write(root, pretty: true);
            var deserialized = Def.Recorder.Read<RecursiveSquaredRecorder>(serialized);

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
        public void RootPrimitive()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            int value = 4;

            // gonna be honest, this feels kind of like overkill
            string serialized = Def.Recorder.Write(value, pretty: true);
            var deserialized = Def.Recorder.Read<int>(serialized);

            Assert.AreEqual(value, deserialized);
        }

        private class DoubleLinkedRecorder : Def.IRecordable
        {
            public DoubleLinkedRecorder a;
            public DoubleLinkedRecorder b;

            public void Record(Def.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
            }
        }

        [Test]
        public void DepthDoubleLinked()
        {
            // This test verifies that we can write an extremely deep structure without blowing the stack.
            // We use double links so we don't have to worry about generating an absurd xml file in the process.
            // As of this writing, *without* the stack compensation code, 1000 works and 2000 doesn't
            // I'm choosing 10000 because it's well into the Doesn't Work territory, but it also doesn't take forever to run.
            const int depth = 10000;

            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
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

            string serialized = Def.Recorder.Write(root, pretty: true);
            Assert.IsNotNull(serialized);

            DoubleLinkedRecorder deserialized = Def.Recorder.Read<DoubleLinkedRecorder>(serialized);
            Assert.IsNotNull(deserialized);

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
        public void DepthSingleLinked()
        {
            // This test verifies that we can serialize and/or read an extremely deep structure without blowing the stack.
            // We use single links so we don't generate refs, we actually embed objects.
            const int depth = 10_000;

            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
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

            string serialized = Def.Recorder.Write(root, pretty: true);
            Assert.IsNotNull(serialized);

            // This verifies we haven't done an n^2 monstrosity by letting the depth get too far.
            // With 10_000 items, this generates a 300_000_000 byte file before depth controlling!
            Assert.Less(serialized.Length, 2_000_000);

            DoubleLinkedRecorder deserialized = Def.Recorder.Read<DoubleLinkedRecorder>(serialized);
            Assert.IsNotNull(deserialized);

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

        private class BaseRecordable : Def.IRecordable
        {
            public int baseVal = 0;

            public virtual void Record(Def.Recorder record)
            {
                record.Record(ref baseVal, "baseVal");
            }
        }

        private class DerivedRecordable : BaseRecordable
        {
            public int derivedVal = 0;

            public override void Record(Def.Recorder record)
            {
                base.Record(record);

                record.Record(ref derivedVal, "derivedVal");
            }
        }

        private class RecordableContainer : Def.IRecordable
        {
            public BaseRecordable baseContainer;

            public void Record(Def.Recorder record)
            {
                record.Record(ref baseContainer, "baseContainer");
            }
        }

        [Test]
        public void DerivedRecordables()
        {
            Def.Config.TestParameters = new Def.Config.UnitTestParameters { };

            var parser = new Def.Parser();
            parser.Finish();

            var root = new RecordableContainer();
            root.baseContainer = new DerivedRecordable();
            root.baseContainer.baseVal = 42;
            (root.baseContainer as DerivedRecordable).derivedVal = 81;

            string serialized = Def.Recorder.Write(root, pretty: true);
            var deserialized = Def.Recorder.Read<RecordableContainer>(serialized);

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
                    <Raf id=""ref00000"" class=""DefTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectWarnings(() => deserialized = Def.Recorder.Read<RefsRootRecordable>(serialized));

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
                    <Ref class=""DefTest.Recorder.RefsChildRecordable"" />
                    <Ref id=""ref00000"" class=""DefTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<RefsRootRecordable>(serialized));

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
                    <Ref id=""ref00000"" class=""DefTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<RefsRootRecordable>(serialized));

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
                    <Ref id=""PLACE"" class=""DefTest.Recorder.AStruct"" />
                    <Ref id=""ref00000"" class=""DefTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<RefsRootRecordable>(serialized));

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
                    <Ref id=""PLACE"" class=""DefTest.Recorder.RefsChildRecordable"" />
                    <Ref id=""ref00000"" class=""DefTest.Recorder.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty null=""true"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = Def.Recorder.Read<RefsRootRecordable>(serialized);

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        public class AttributeRecordable : Def.IRecordable
        {
            public string attributing = "";

            public void Record(Def.Recorder record)
            {
                if (record.Mode == Def.Recorder.Direction.Read)
                {
                    attributing = record.Xml.Attributes().Single(attr => attr.Name == "converted").Value;
                }
                else
                {
                    record.Xml.SetAttributeValue("converted", attributing);
                }
            }
        }

        public class AttributeHolder : Def.IRecordable
        {
            public AttributeRecordable a;
            public AttributeRecordable b;
            public AttributeRecordable c;

            public void Record(Def.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
                record.Record(ref c, "c");
            }
        }

        [Test]
        public void Attributes()
        {
            var holder = new AttributeHolder();

            holder.a = new AttributeRecordable { attributing = "hello_an_attribute" };
            holder.b = new AttributeRecordable { attributing = "<XML-SENSITIVE>" };
            holder.c = new AttributeRecordable { attributing = "I guess I'll write some more text here?" };

            string serialized = Def.Recorder.Write(holder, pretty: true);
            var deserialized = Def.Recorder.Read<AttributeHolder>(serialized);

            Assert.AreEqual(holder.a.attributing, deserialized.a.attributing);
            Assert.AreEqual(holder.b.attributing, deserialized.b.attributing);
            Assert.AreEqual(holder.c.attributing, deserialized.c.attributing);
        }

        [Test]
        public void AttributeRef()
        {
            var holder = new AttributeHolder();

            holder.a = new AttributeRecordable { attributing = "I am being referenced!" };
            holder.b = holder.a;
            holder.c = holder.a;

            string serialized = Def.Recorder.Write(holder, pretty: true);
            var deserialized = Def.Recorder.Read<AttributeHolder>(serialized);

            Assert.AreEqual(holder.a.attributing, deserialized.a.attributing);
            Assert.AreSame(holder.a, holder.b);
            Assert.AreSame(holder.a, holder.c);
        }

        public class MultiRecordRec : Def.IRecordable
        {
            public int x;
            public int y;

            public void Record(Def.Recorder record)
            {
                record.Record(ref x, "x");
                record.Record(ref y, "x");  // oops!
            }
        }

        [Test]
        public void MultiRecord()
        {
            var mr = new MultiRecordRec();
            mr.x = 3;
            mr.y = 5;

            string serialized = null;
            ExpectErrors(() => serialized = Def.Recorder.Write(mr, pretty: true));
            var deserialized = Def.Recorder.Read<MultiRecordRec>(serialized);

            Assert.AreEqual(mr.x, deserialized.x);
            // y's value is left undefined
        }

        public class PrimitivesContainer : Def.IRecordable
        {
            public PrimitivesRecordable recordable;

            public void Record(Def.Recorder record)
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
                    <Ref id=""ref00000"" class=""DefTest.Recorder.PrimitivesRecordable"" />
                  </refs>
                  <data>
                    <recordable ref=""ref00000"" null=""true""/>
                  </data>
                </Record>";
            PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<PrimitivesContainer>(serialized));

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
                    <Ref id=""ref00000"" class=""DefTest.Recorder.PrimitivesRecordable"">
                        <intValue>42</intValue>
                    </Ref>
                  </refs>
                  <data>
                    <recordable ref=""ref00000"" garbage=""yup"" />
                  </data>
                </Record>";
            PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<PrimitivesContainer>(serialized));

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
            ExpectErrors(() => deserialized = Def.Recorder.Read<PrimitivesContainer>(serialized));

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
                    <Ref id=""ref00000"" class=""DefTest.Recorder.PrimitivesContainer"" />
                  </refs>
                  <data>
                    <recordable ref=""ref00000"" />
                  </data>
                </Record>";
            PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Def.Recorder.Read<PrimitivesContainer>(serialized));

            Assert.IsNull(deserialized.recordable);
        }
    }
}
