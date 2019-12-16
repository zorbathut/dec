namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    [TestFixture]
    public class Record : Base
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[]{ });
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { typeof(StubDef) }, explicitStaticRefs: new Type[] { typeof(StaticReferenceDefs) });
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { }, explicitConverters: new Type[] { });
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

            public void Record(Def.Recorder record)
            {
                record.Record(ref intList, "intList");
                record.Record(ref stringDict, "stringDict");
            }
        }

        [Test]
        public void Containers()
        {
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
            parser.Finish();

            var containers = new ContainersRecordable();
            containers.intList.Add(42);
            containers.intList.Add(1234);
            containers.intList.Add(-105);

            containers.stringDict["Key"] = "Value";
            containers.stringDict["Info"] = "Data";

            string serialized = Def.Recorder.Write(containers, pretty: true);
            var deserialized = Def.Recorder.Read<ContainersRecordable>(serialized);

            Assert.AreEqual(containers.intList, deserialized.intList);
            Assert.AreEqual(containers.stringDict, deserialized.stringDict);
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
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
            var parser = new Def.Parser(explicitOnly: true, explicitTypes: new Type[] { });
            parser.Finish();

            int value = 4;

            // gonna be honest, this feels kind of like overkill
            string serialized = Def.Recorder.Write(value, pretty: true);
            var deserialized = Def.Recorder.Read<int>(serialized);

            Assert.AreEqual(value, deserialized);
        }
    }
}
