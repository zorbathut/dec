using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class RecorderRef : Base
    {
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
                record.Shared().Record(ref childAone, "childAone");
                record.Shared().Record(ref childAtwo, "childAtwo");
                record.Shared().Record(ref childB, "childB");
                record.Shared().Record(ref childEmpty, "childEmpty");
            }
        }

        [Test]
        public void Refs([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

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

        public class RecursiveParent : Dec.IRecordable
        {
            public List<RecursiveNode> children;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref children, "children");
            }
        }

        public class RecursiveNode : Dec.IRecordable
        {
            public RecursiveNode childA;
            public RecursiveNode childB;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref childA, "childA");
                record.Shared().Record(ref childB, "childB");
            }
        }

        [Test]
        public void ContainerRecursive([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parent = new RecursiveParent();
            parent.children = new List<RecursiveNode>();
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


        private class DoubleLinkedRecorder : Dec.IRecordable
        {
            public DoubleLinkedRecorder a;
            public DoubleLinkedRecorder b;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref a, "a");
                record.Shared().Record(ref b, "b");
            }
        }

        [Test]
        public void DepthDoubleLinked([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            // This test verifies that we can write an extremely deep structure without blowing the stack.
            // We use double links so we don't have to worry about generating an absurd xml file in the process.
            // As of this writing, *without* the stack compensation code, 1000 works and 2000 doesn't
            // I'm choosing 10000 because it's well into the Doesn't Work territory, but it also doesn't take forever to run.
            const int depth = 10000;

            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

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
        public void DepthSingleLinked([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            // This test verifies that we can serialize and/or read an extremely deep structure without blowing the stack.
            // We use single links so we don't generate refs, we actually embed objects.
            const int depth = 10_000;

            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

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

        private class UnsharedRecorder : Dec.IRecordable
        {
            public UnsharedRecorder a;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref a, "a");
            }
        }

        [Test]
        public void DepthUnsharedWarning([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            // This test is all about yelling at you if your stack depth gets too high

            // We're actually really close to hitting stack overflow here, so we run it with 130 so we can still read it.
            const int depth = 130;

            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new UnsharedRecorder();

            {
                var current = root;

                for (int i = 1; i < depth; ++i)
                {
                    var next = new UnsharedRecorder();
                    current.a = next;
                    current = next;
                }
            }

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteErrors: mode != RecorderMode.Simple, expectWriteWarnings: mode == RecorderMode.Clone);

            {
                var seen = new HashSet<UnsharedRecorder>();
                var current = deserialized;
                while (current != null && !seen.Contains(current))
                {
                    seen.Add(current);
                    current = current.a;
                }

                Assert.AreEqual(depth, seen.Count);
            }
        }


        class OptionalChain : Dec.IRecordable
        {
            public int[] payload;
            public OptionalChain chain;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref payload, "payload");
                record.Record(ref chain, "chain");

                if (record.Mode == Dec.Recorder.Direction.Read && payload != null)
                {
                    Assert.AreEqual(1, payload[0]);
                }
            }
        }

        [Test]
        public void DepthTrueUnshared([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            const int maxDepth = 80;
            OptionalChain[] chains = new OptionalChain[maxDepth];

            for (int i = 0; i < maxDepth; ++i)
            {
                var link = chains[i] = new OptionalChain();
                for (int j = 0; j < i; ++j)
                {
                    link = link.chain = new OptionalChain();
                }

                link.payload = new int[] { 1 };
            }

            var deserialized = DoRecorderRoundTrip(chains, mode, expectWriteWarnings: mode == RecorderMode.Clone);
        }

        [Test]
        public void BadRefTag()
        {
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Raf id=""ref00000"" class=""DecTest.RecorderRef.RefsChildRecordable"" />
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
                    <Ref id=""ref00000"" class=""DecTest.RecorderRef.RefsChildRecordable"" />
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
                    <Ref id=""ref00000"" class=""DecTest.RecorderRef.RefsChildRecordable"" />
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
                    <Ref id=""PLACE"" class=""DecTest.RecorderRef.AStruct"" />
                    <Ref id=""ref00000"" class=""DecTest.RecorderRef.RefsChildRecordable"" />
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
                    <Ref id=""PLACE"" class=""DecTest.RecorderRef.RefsChildRecordable"" />
                    <Ref id=""ref00000"" class=""DecTest.RecorderRef.RefsChildRecordable"" />
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
            Recorder.PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized));

            Assert.IsNotNull(deserialized.recordable);
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
            Recorder.PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized));

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
            Recorder.PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized));

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
            Recorder.PrimitivesContainer deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized));

            Assert.IsNull(deserialized.recordable);
        }

        public class DictionaryKeyRefDec : Dec.IRecordable
        {
            public StubRecordable referenceA;
            public Dictionary<StubRecordable, string> dict;
            public StubRecordable referenceB;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref referenceA, "referenceA");
                record.Shared().Record(ref dict, "dict");
                record.Shared().Record(ref referenceB, "referenceB");
            }
        }

        [Test]
        public void DictionaryKeyRef([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            var dict = new DictionaryKeyRefDec();
            dict.referenceA = new StubRecordable();
            dict.referenceB = new StubRecordable();
            dict.dict = new Dictionary<StubRecordable, string>();
            dict.dict[dict.referenceA] = "Hello";
            dict.dict[dict.referenceB] = "Goodbye";

            var deserialized = DoRecorderRoundTrip(dict, mode);

            Assert.AreNotSame(deserialized.referenceA, deserialized.referenceB);
            Assert.AreEqual("Hello", deserialized.dict[deserialized.referenceA]);
            Assert.AreEqual("Goodbye", deserialized.dict[deserialized.referenceB]);
        }

        public class ParserRefDec : Dec.Dec
        {
            public Stub initialized = new Stub();
            public Stub setToNull = null;
        }

        [Test]
        public void ParserRef([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ParserRefDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ParserRefDec decName=""Test"">
                        <initialized ref=""invalid"" />
                        <setToNull ref=""invalid"" />
                    </ParserRefDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var test = Dec.Database<ParserRefDec>.Get("Test");
            Assert.IsNotNull(test);
            Assert.IsNotNull(test.initialized);
            Assert.IsNotNull(test.setToNull);
        }

        public class BaseRecordable : Dec.IRecordable
        {
            public virtual void Record(Dec.Recorder recorder) { }
        }

        public class DerivedRecordable : BaseRecordable
        {
            public override void Record(Dec.Recorder recorder) { }
        }

        public class RecordableContainer : Dec.IRecordable
        {
            public BaseRecordable a;
            public BaseRecordable b;

            public virtual void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, "a");
                recorder.Shared().Record(ref b, "b");
            }
        }

        [Test]
        public void DerivedRefRecordables([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RecordableContainer();
            root.a = new DerivedRecordable();
            root.b = root.a;

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.IsInstanceOf<DerivedRecordable>(deserialized.a);
            Assert.IsInstanceOf<DerivedRecordable>(deserialized.b);

            Assert.AreSame(deserialized.a, deserialized.b);
        }
    }
}
