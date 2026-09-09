using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public void DepthDoubleLinked([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple, RecorderMode.Checksum)] RecorderMode mode)
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
        public void DepthSingleLinked([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple, RecorderMode.Checksum)] RecorderMode mode)
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
        public void DepthUnsharedWarning([ValuesExcept(RecorderMode.Validation, RecorderMode.Checksum)] RecorderMode mode)
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

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteErrors: mode != RecorderMode.Simple, expectWriteWarnings: mode == RecorderMode.Clone, errorValidator: err => err.Contains("Depth limiter") && err.Contains("unshareable node stack"), warningValidator: wrn => wrn.Contains("Depth limiter") && wrn.Contains("unshareable node stack"));

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
        public void DepthTrueUnshared([ValuesExcept(RecorderMode.Validation, RecorderMode.Checksum)] RecorderMode mode)
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

            var deserialized = DoRecorderRoundTrip(chains, mode, expectWriteWarnings: mode == RecorderMode.Clone, warningValidator: wrn => wrn.Contains("Depth limiter") && wrn.Contains("unshareable node stack"));
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
            ExpectWarnings(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized), wrn => wrn.Contains("Reference element should be named 'Ref'"));

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized), err => err.Contains("Missing reference ID"));

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized), err => err.Contains("Missing reference class name"));

            Assert.IsNotNull(deserialized.childAone);
            Assert.IsNotNull(deserialized.childAtwo);
            Assert.IsNotNull(deserialized.childB);
            Assert.IsNull(deserialized.childEmpty);

            Assert.AreEqual(deserialized.childAone, deserialized.childAtwo);
            Assert.AreNotEqual(deserialized.childAone, deserialized.childB);
        }

        [Test]
        public void UnresolvableClass()
        {
            // Simulates a stale save file referencing a class that has since been renamed or deleted.
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""PLACE"" class=""DecTest.RecorderRef.ClassThatNoLongerExists"" />
                    <Ref id=""ref00000"" class=""DecTest.RecorderRef.RefsChildRecordable"" />
                  </refs>
                  <data>
                    <childAone ref=""ref00000"" />
                    <childAtwo ref=""ref00000"" />
                    <childB />
                    <childEmpty ref=""PLACE"" />
                  </data>
                </Record>";
            RefsRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized), err => err.Contains("Couldn't find type named") || err.Contains("without a valid reference mapping"));

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefsRootRecordable>(serialized), err => err.Contains("which is a value type"));

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized), err => err.Contains("Found a reference in a non-.Shared() context") || err.Contains("Null element may not have ref"));

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized), err => err.Contains("Found a reference in a non-.Shared() context") || err.Contains("unknown attributes"));

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized), err => err.Contains("Found a reference in a non-.Shared() context") || err.Contains("without a valid reference mapping"));

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<Recorder.PrimitivesContainer>(serialized), err => err.Contains("Found a reference in a non-.Shared() context") || err.Contains("cannot be converted to expected type"));

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
        public void DictionaryKeyRef([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple, RecorderMode.Checksum)] RecorderMode mode)
        {
            // StubRecordable hashes by identity, so it survives being read into the dictionary before its contents arrive.
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

        public class ContentHashedKey : Dec.IRecordable
        {
            public int id;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref id, "id");
            }

            public override bool Equals(object obj)
            {
                return obj is ContentHashedKey rhs && rhs.id == id;
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        public class ContentHashedKeyRoot : Dec.IRecordable
        {
            public ContentHashedKey reference;
            public Dictionary<ContentHashedKey, string> dict;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref reference, "reference");
                record.Shared().Record(ref dict, "dict");
            }
        }

        [Test]
        public void DictionaryKeyRefContentHashed([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple, RecorderMode.Checksum)] RecorderMode mode)
        {
            // A key that hashes on its contents can't be read back as a reference, so it isn't shareable; using one that's shared elsewhere is a conflict. Clone doesn't go through the reference block at all and has no such restriction.
            var root = new ContentHashedKeyRoot();
            root.reference = new ContentHashedKey() { id = 1 };
            root.dict = new Dictionary<ContentHashedKey, string>();
            root.dict[root.reference] = "Hello";

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteErrors: mode != RecorderMode.Clone, errorValidator: err => (err.Contains("previously-seen unshared object") || err.Contains("previously-seen shared object")) && err.Contains("overrides GetHashCode"));

            Assert.AreEqual(1, deserialized.dict.Count);
        }

        public class IdentityHashedBase : Dec.IRecordable
        {
            public int id;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref id, "id");
            }
        }

        public class ContentHashedDerived : IdentityHashedBase
        {
            public override bool Equals(object obj)
            {
                return obj is ContentHashedDerived rhs && rhs.id == id;
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        public class IdentityHashedBaseKeyRoot : Dec.IRecordable
        {
            public Dictionary<IdentityHashedBase, string> dict;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref dict, "dict");
            }
        }

        [Test]
        public void DictionaryKeyRefDerivedContentHashed()
        {
            // The declared key type hashes by identity, so nothing about the position rules this out; only the object the reference resolves to gives it away. Dec won't write this, but an older file or a hand-edited one can hold it.
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""key"" class=""DecTest.RecorderRef.ContentHashedDerived""><id>1</id></Ref>
                  </refs>
                  <data>
                    <dict>
                      <li>
                        <key ref=""key"" />
                        <value>Hello</value>
                      </li>
                    </dict>
                  </data>
                </Record>";

            IdentityHashedBaseKeyRoot deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<IdentityHashedBaseKeyRoot>(serialized), err => err.Contains("only a type that hashes by identity") || err.Contains("includes null key"));

            Assert.AreEqual(0, deserialized.dict.Count);
        }

        public interface IHashKey
        {
        }

        public class ContentHashedInterfaced : Dec.IRecordable, IHashKey
        {
            public int id;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref id, "id");
            }

            public override bool Equals(object obj)
            {
                return obj is ContentHashedInterfaced rhs && rhs.id == id;
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        public class InterfaceKeyRoot : Dec.IRecordable
        {
            public Dictionary<IHashKey, string> dict;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref dict, "dict");
            }
        }

        [Test]
        public void DictionaryKeyRefInterfaceContentHashed()
        {
            // Same, for an interface-typed key; reflection can't tell us anything about an interface's GetHashCode at all.
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""key"" class=""DecTest.RecorderRef.ContentHashedInterfaced""><id>1</id></Ref>
                  </refs>
                  <data>
                    <dict>
                      <li>
                        <key ref=""key"" />
                        <value>Hello</value>
                      </li>
                    </dict>
                  </data>
                </Record>";

            InterfaceKeyRoot deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<InterfaceKeyRoot>(serialized), err => err.Contains("only a type that hashes by identity") || err.Contains("includes null key"));

            Assert.AreEqual(0, deserialized.dict.Count);
        }

        public class IdentityHashedInterfaced : Dec.IRecordable, IHashKey
        {
            public int id;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref id, "id");
            }
        }

        [Test]
        public void DictionaryKeyRefInterfaceIdentityHashed()
        {
            // The other side of it: an interface-typed key whose concrete type hashes by identity is fine, and mustn't be rejected.
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""key"" class=""DecTest.RecorderRef.IdentityHashedInterfaced""><id>1</id></Ref>
                  </refs>
                  <data>
                    <dict>
                      <li>
                        <key ref=""key"" />
                        <value>Hello</value>
                      </li>
                    </dict>
                  </data>
                </Record>";

            var deserialized = Dec.Recorder.Read<InterfaceKeyRoot>(serialized);

            Assert.AreEqual(1, deserialized.dict.Count);
            foreach (var kvp in deserialized.dict)
            {
                Assert.AreEqual(1, (kvp.Key as IdentityHashedInterfaced).id);
                Assert.AreEqual("Hello", deserialized.dict[kvp.Key]);
            }
        }

        public class IdentityHashedForcedKey : Dec.IRecordable, Dec.IRefForce
        {
            public int id;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref id, "id");
            }
        }

        public class IdentityHashedForcedKeyRoot : Dec.IRecordable
        {
            public Dictionary<IdentityHashedForcedKey, string> dict;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref dict, "dict");
            }
        }

        [Test]
        public void DictionaryKeyForced([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            // The end of the rule: an identity-hashed key can be hoisted into the refs block and read back into the dictionary before it holds anything.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var key = new IdentityHashedForcedKey() { id = 7 };
            var root = new IdentityHashedForcedKeyRoot() { dict = new Dictionary<IdentityHashedForcedKey, string>() };
            root.dict[key] = "Hello";

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains(@"<key ref="));
            });

            Assert.AreEqual(1, deserialized.dict.Count);
            foreach (var kvp in deserialized.dict)
            {
                Assert.AreEqual(7, kvp.Key.id);
                Assert.AreEqual("Hello", deserialized.dict[kvp.Key]);
            }
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
            ExpectErrors(() => parser.Finish(), err => err.Contains("Found a reference tag while not evaluating Recorder mode"));

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

        public class StubHolderShared : Dec.IRecordable
        {
            public StubRecordable stub;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref stub, "stub");
            }
        }

        public class StubHolderUnshared : Dec.IRecordable
        {
            public StubRecordable stub;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref stub, "stub");
            }
        }

        [Test]
        public void SharedBeforeUnshared([ValuesExcept(RecorderMode.Simple)] RecorderMode mode, [Values] bool firstShared, [Values] bool secondShared)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new List<object>();
            var stub = new StubRecordable();

            if (firstShared)
            {
                root.Add(new StubHolderShared { stub = stub });
            }
            else
            {
                root.Add(new StubHolderUnshared { stub = stub });
            }

            if (secondShared)
            {
                root.Add(new StubHolderShared { stub = stub });
            }
            else
            {
                root.Add(new StubHolderUnshared { stub = stub });
            }

            string expectedError = null;

            if (mode == RecorderMode.Clone || mode == RecorderMode.Checksum)
            {
                // no errors expected
            }
            else if (!firstShared)
            {
                expectedError = "Attempted to create a new shared reference at [RECORD[1].stub] to a previously-seen unshared object at [RECORD[0].stub].";
            }
            else if (!secondShared)
            {
                expectedError = "Attempted to create a new unshared reference at [RECORD[1].stub] to a previously-seen shared object at [RECORD[0].stub].";
            }

            if (expectedError != null)
            {
                DoRecorderRoundTrip(root, mode, expectWriteErrors: true, errorValidator: err => err.Contains(expectedError));
            }
            else
            {
                DoRecorderRoundTrip(root, mode);
            }
        }

        public class UnsharedDictRoot : Dec.IRecordable
        {
            public Dictionary<IdentityHashedBase, string> dict;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref dict, "dict");
            }
        }

        [Test]
        public void DictionaryKeyRefUnsharedContainer()
        {
            // The key type is fine; it's the dictionary that isn't shared, which is the plain non-.Shared() case and has to be reported as one.
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""key"" class=""DecTest.RecorderRef.IdentityHashedBase""><id>1</id></Ref>
                  </refs>
                  <data>
                    <dict>
                      <li>
                        <key ref=""key"" />
                        <value>Hello</value>
                      </li>
                    </dict>
                  </data>
                </Record>";

            UnsharedDictRoot deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<UnsharedDictRoot>(serialized), err => err.Contains("non-.Shared() context"));

            Assert.AreEqual(1, deserialized.dict.Count);
        }

        public class NullElementSetRoot : Dec.IRecordable
        {
            public HashSet<StubRecordable> set;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref set, "set");
            }
        }

        [Test]
        public void HashSetNullElementWrite()
        {
            // A null element is legal in a HashSet of a reference type; it isn't readable, but the write must report that rather than crash.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new NullElementSetRoot() { set = new HashSet<StubRecordable>() { null, new StubRecordable() } };

            string serialized = Dec.Recorder.Write(root);
            Assert.IsTrue(serialized.Contains("null"));
        }
        public class RefValueRecordable : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref value, "value");
            }
        }

        public class DeepStrippedHolder : Dec.IRecordable
        {
            public DeepStrippedHolder next;
            public RefValueRecordable payload;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref next, "next");
                record.Shared().Record(ref payload, "payload");
            }
        }

        [Test]
        public void DepthStrippedTwice([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            // The payload's home is deeper than the depth limiter's cutoff, so the depth pass gets a look at it after it's already been hoisted into the refs block.
            const int depth = 30;

            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var payload = new RefValueRecordable() { value = 42 };

            var root = new DeepStrippedHolder();
            {
                var current = root;
                for (int i = 1; i < depth; ++i)
                {
                    current = current.next = new DeepStrippedHolder();
                }
                current.payload = payload;
            }
            root.payload = payload;

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                // The class tag shows up only on Ref elements, so this counts the payload's entries in the refs block.
                Assert.AreEqual(1, Regex.Matches(serialized, "RefValueRecordable").Count);
            });

            var deep = deserialized;
            while (deep.next != null)
            {
                deep = deep.next;
            }

            Assert.AreSame(deserialized.payload, deep.payload);
            Assert.AreEqual(42, deserialized.payload.value);
        }
        public class RefValueRootRecordable : Dec.IRecordable
        {
            public RefValueRecordable one;
            public RefValueRecordable two;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref one, "one");
                record.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void DuplicateRefId()
        {
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""dupe"" class=""DecTest.RecorderRef.RefValueRecordable""><value>1</value></Ref>
                    <Ref id=""dupe"" class=""DecTest.RecorderRef.RefValueRecordable""><value>2</value></Ref>
                  </refs>
                  <data>
                    <one ref=""dupe"" />
                    <two ref=""dupe"" />
                  </data>
                </Record>";

            RefValueRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RefValueRootRecordable>(serialized), err => err.Contains("Duplicate reference ID"));

            Assert.AreSame(deserialized.one, deserialized.two);
            Assert.AreEqual(1, deserialized.one.value);
        }

        public class RefNamedRecordable : Dec.IRecordable, Dec.IRefName
        {
            public string name;
            public int value;

            public string RefName()
            {
                return name;
            }

            public void Record(Dec.Recorder record)
            {
                record.Record(ref name, "name");
                record.Record(ref value, "value");
            }
        }

        public class RefNamedRootRecordable : Dec.IRecordable
        {
            public RefNamedRecordable sharedA;
            public RefNamedRecordable sharedB;
            public RefNamedRecordable solo;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref sharedA, "sharedA");
                record.Shared().Record(ref sharedB, "sharedB");
                record.Shared().Record(ref solo, "solo");
            }
        }

        [Test]
        public void RefNamed([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var shared = new RefNamedRecordable() { name = "alpha", value = 1 };
            var root = new RefNamedRootRecordable() { sharedA = shared, sharedB = shared, solo = new RefNamedRecordable() { name = "beta", value = 2 } };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains(@"id=""alpha"""));
                Assert.IsTrue(serialized.Contains(@"ref=""alpha"""));
            });

            Assert.AreSame(deserialized.sharedA, deserialized.sharedB);
            Assert.AreEqual(1, deserialized.sharedA.value);
            Assert.AreEqual(2, deserialized.solo.value);
        }

        [Test]
        public void RefNamedUnreferenced([ValuesExcept(RecorderMode.Simple, RecorderMode.RefEverything)] RecorderMode mode)
        {
            // A name only takes effect on an object that actually lands in the refs block. RefEverything is excluded because it puts everything there.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefNamedRootRecordable() { solo = new RefNamedRecordable() { name = "beta", value = 2 } };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsFalse(serialized.Contains(@"id=""beta"""));
            });

            Assert.AreEqual(2, deserialized.solo.value);
        }

        public class RefNamedPairRootRecordable : Dec.IRecordable
        {
            public RefNamedRecordable oneA;
            public RefNamedRecordable oneB;
            public RefNamedRecordable twoA;
            public RefNamedRecordable twoB;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref oneA, "oneA");
                record.Shared().Record(ref oneB, "oneB");
                record.Shared().Record(ref twoA, "twoA");
                record.Shared().Record(ref twoB, "twoB");
            }
        }

        [Test]
        public void RefNamedConflict([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var one = new RefNamedRecordable() { name = "dupe", value = 1 };
            var two = new RefNamedRecordable() { name = "dupe", value = 2 };
            var root = new RefNamedPairRootRecordable() { oneA = one, oneB = one, twoA = two, twoB = two };

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteWarnings: mode != RecorderMode.Clone && mode != RecorderMode.Checksum, warningValidator: wrn => wrn.Contains("dupe") && wrn.Contains("already in use"));

            Assert.AreSame(deserialized.oneA, deserialized.oneB);
            Assert.AreSame(deserialized.twoA, deserialized.twoB);
            Assert.AreNotSame(deserialized.oneA, deserialized.twoA);
            Assert.AreEqual(1, deserialized.oneA.value);
            Assert.AreEqual(2, deserialized.twoA.value);
        }

        public class RefNamedMixedRootRecordable : Dec.IRecordable
        {
            public RefNamedRecordable namedA;
            public RefNamedRecordable namedB;
            public RefValueRecordable plainA;
            public RefValueRecordable plainB;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref namedA, "namedA");
                record.Shared().Record(ref namedB, "namedB");
                record.Shared().Record(ref plainA, "plainA");
                record.Shared().Record(ref plainB, "plainB");
            }
        }

        [Test]
        public void RefNamedGeneratedCollision([ValuesExcept(RecorderMode.Simple, RecorderMode.RefEverything)] RecorderMode mode)
        {
            // A user name out of the generated namespace is fine; the generator just keeps looking until it finds a free slot. RefEverything is excluded because it hands generated names to objects this test doesn't control, which would make the user's name the duplicate instead.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var named = new RefNamedRecordable() { name = "ref00000", value = 1 };
            var plain = new RefValueRecordable() { value = 2 };
            var root = new RefNamedMixedRootRecordable() { namedA = named, namedB = named, plainA = plain, plainB = plain };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.AreEqual(1, Regex.Matches(serialized, @"id=""ref00000""").Count);
            });

            Assert.AreSame(deserialized.namedA, deserialized.namedB);
            Assert.AreSame(deserialized.plainA, deserialized.plainB);
            Assert.AreEqual(1, deserialized.namedA.value);
            Assert.AreEqual(2, deserialized.plainA.value);
        }

        [Test]
        public void RefNamedNull([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var shared = new RefNamedRecordable() { name = null, value = 1 };
            var root = new RefNamedRootRecordable() { sharedA = shared, sharedB = shared };

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreSame(deserialized.sharedA, deserialized.sharedB);
            Assert.AreEqual(1, deserialized.sharedA.value);
        }

        [Test]
        public void RefNamedEmpty([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            // The empty string is a legal, if unhelpful, ref name.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var shared = new RefNamedRecordable() { name = "", value = 1 };
            var root = new RefNamedRootRecordable() { sharedA = shared, sharedB = shared };

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreSame(deserialized.sharedA, deserialized.sharedB);
            Assert.AreEqual(1, deserialized.sharedA.value);
        }

        public class RefNamedChainRecordable : Dec.IRecordable, Dec.IRefName
        {
            public string name;
            public RefNamedChainRecordable next;

            public string RefName()
            {
                return name;
            }

            public void Record(Dec.Recorder record)
            {
                record.Record(ref name, "name");
                record.Shared().Record(ref next, "next");
            }
        }

        [Test]
        public void RefNamedDepth([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            // Nothing here is referenced twice; the refs block is entirely the work of the depth limiter, and it should be using the custom names too.
            const int depth = 60;

            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefNamedChainRecordable() { name = "node0" };
            {
                var current = root;
                for (int i = 1; i < depth; ++i)
                {
                    current = current.next = new RefNamedChainRecordable() { name = $"node{i}" };
                }
            }

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains(@"id=""node"));
                Assert.IsFalse(serialized.Contains(@"id=""ref"));
            });

            {
                var current = deserialized;
                for (int i = 0; i < depth; ++i)
                {
                    Assert.IsNotNull(current);
                    Assert.AreEqual($"node{i}", current.name);
                    current = current.next;
                }

                Assert.IsNull(current);
            }
        }

        public class RefForcedRecordable : Dec.IRecordable, Dec.IRefForce
        {
            public int value;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref value, "value");
            }
        }

        public class RefForcedNamedRecordable : Dec.IRecordable, Dec.IRefForce, Dec.IRefName
        {
            public string name;
            public int value;

            public string RefName()
            {
                return name;
            }

            public void Record(Dec.Recorder record)
            {
                record.Record(ref name, "name");
                record.Record(ref value, "value");
            }
        }

        public class RefForcedRootRecordable : Dec.IRecordable
        {
            public RefForcedRecordable shared;
            public RefForcedNamedRecordable sharedNamed;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref shared, "shared");
                record.Shared().Record(ref sharedNamed, "sharedNamed");
            }
        }

        public class RefForcedUnsharedRootRecordable : Dec.IRecordable
        {
            public RefForcedRecordable unshared;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref unshared, "unshared");
            }
        }

        [Test]
        public void RefForced([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefForcedRootRecordable() { shared = new RefForcedRecordable() { value = 1 }, sharedNamed = new RefForcedNamedRecordable() { name = "gamma", value = 2 } };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                // Both are referenced exactly once, so without forcing they'd be inline.
                Assert.IsTrue(serialized.Contains(@"<shared ref="));
                Assert.IsTrue(serialized.Contains(@"<sharedNamed ref=""gamma"""));
                Assert.IsTrue(serialized.Contains(@"id=""gamma"""));
            });

            Assert.AreEqual(1, deserialized.shared.value);
            Assert.AreEqual(2, deserialized.sharedNamed.value);
        }

        [Test]
        public void RefForcedUnshared([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            // A reference in a non-shared position can't be read back, so forcing has to give way to the recorder's sharing rules.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefForcedUnsharedRootRecordable() { unshared = new RefForcedRecordable() { value = 1 } };

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteWarnings: mode != RecorderMode.Clone && mode != RecorderMode.Checksum, warningValidator: wrn => wrn.Contains("IRefForce"), testSerializedResult: serialized =>
            {
                Assert.IsFalse(serialized.Contains(@"<unshared ref="));
            });

            Assert.AreEqual(1, deserialized.unshared.value);
        }

        public class RefForcedRootObjectRecordable : Dec.IRecordable, Dec.IRefForce
        {
            public int value;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref value, "value");
            }
        }

        [Test]
        public void RefForcedRootObject([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefForcedRootObjectRecordable() { value = 3 };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains(@"<data ref="));
            });

            Assert.AreEqual(3, deserialized.value);
        }

        public class RefForcedConvertedRecord : Dec.IRefForce
        {
            public int value;
        }

        public class RefForcedConvertedRecordConverter : Dec.ConverterRecord<RefForcedConvertedRecord>
        {
            public override void Record(ref RefForcedConvertedRecord input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
            }
        }

        public class RefForcedConvertedRecordRoot : Dec.IRecordable
        {
            public RefForcedConvertedRecord converted;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref converted, "converted");
            }
        }

        [Test]
        public void RefForcedConverterRecord([ValuesExcept(RecorderMode.Simple, RecorderMode.Validation)] RecorderMode mode)
        {
            // Validation is excluded for the same reason the other converter tests exclude it: the validation writer doesn't generate code for converters.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(RefForcedConvertedRecordConverter) } });

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new RefForcedConvertedRecordRoot() { converted = new RefForcedConvertedRecord() { value = 4 } };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains(@"<converted ref="));
            });

            Assert.AreEqual(4, deserialized.converted.value);
        }

        public class RefForcedConvertedString : Dec.IRefForce
        {
            public int value;
        }

        public class RefForcedConvertedStringConverter : Dec.ConverterString<RefForcedConvertedString>
        {
            public override RefForcedConvertedString Read(string input, Dec.Context context)
            {
                return new RefForcedConvertedString() { value = int.Parse(input) };
            }

            public override string Write(RefForcedConvertedString input)
            {
                return input.value.ToString();
            }
        }

        public class RefForcedConvertedStringRoot : Dec.IRecordable
        {
            public RefForcedConvertedString converted;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref converted, "converted");
            }
        }

        [Test]
        public void RefForcedConverterString([ValuesExcept(RecorderMode.Simple, RecorderMode.Validation)] RecorderMode mode)
        {
            // Validation is excluded for the same reason the other converter tests exclude it: the validation writer doesn't generate code for converters.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(RefForcedConvertedStringConverter) } });

            var parser = new Dec.Parser();
            parser.Finish();

            var root = new RefForcedConvertedStringRoot() { converted = new RefForcedConvertedString() { value = 5 } };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains(@"<converted ref="));
            });

            Assert.AreEqual(5, deserialized.converted.value);
        }

        // Hashes on its contents, so it can't be shared in a key position no matter what it asks for.
        public class RefForcedKey : Dec.IRecordable, Dec.IRefForce
        {
            public int id;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref id, "id");
            }

            public override bool Equals(object obj)
            {
                return obj is RefForcedKey rhs && rhs.id == id;
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        public class RefForcedKeyRoot : Dec.IRecordable
        {
            public Dictionary<RefForcedKey, int> dict;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref dict, "dict");
            }
        }

        [Test]
        public void RefForcedDictionaryKey([ValuesExcept(RecorderMode.Simple, RecorderMode.Validation)] RecorderMode mode)
        {
            // A content-hashed key isn't a shareable position, so forcing has nothing to work with here.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefForcedKeyRoot() { dict = new Dictionary<RefForcedKey, int>() };
            root.dict[new RefForcedKey() { id = 1 }] = 42;

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteWarnings: mode != RecorderMode.Clone && mode != RecorderMode.Checksum, warningValidator: wrn => wrn.Contains("IRefForce"));

            Assert.AreEqual(1, deserialized.dict.Count);
            Assert.AreEqual(42, deserialized.dict[new RefForcedKey() { id = 1 }]);
        }

        public class RefForcedListRoot : Dec.IRecordable
        {
            public List<RefForcedRecordable> list;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref list, "list");
            }
        }

        [Test]
        public void RefForcedList([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            // Collection elements inherit a Flexible sharing setting from the .Shared() container, which is enough to hold references.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefForcedListRoot() { list = new List<RefForcedRecordable>() { new RefForcedRecordable() { value = 1 }, new RefForcedRecordable() { value = 2 } } };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.IsTrue(serialized.Contains(@"<li ref="));
            });

            Assert.AreEqual(2, deserialized.list.Count);
            Assert.AreEqual(1, deserialized.list[0].value);
            Assert.AreEqual(2, deserialized.list[1].value);
        }

        public class RefForcedDictionaryValueRoot : Dec.IRecordable
        {
            public Dictionary<string, RefForcedRecordable> dict;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref dict, "dict");
            }
        }

        [Test]
        public void RefForcedDictionaryValue([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            // Values don't participate in hashing, so unlike keys they're free to arrive as references.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var root = new RefForcedDictionaryValueRoot() { dict = new Dictionary<string, RefForcedRecordable>() { ["a"] = new RefForcedRecordable() { value = 5 } } };

            var deserialized = DoRecorderRoundTrip(root, mode);

            Assert.AreEqual(1, deserialized.dict.Count);
            Assert.AreEqual(5, deserialized.dict["a"].value);
        }

        public class RefForcedPairRoot : Dec.IRecordable
        {
            public RefForcedNamedRecordable one;
            public RefForcedNamedRecordable two;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref one, "one");
                record.Shared().Record(ref two, "two");
            }
        }

        [Test]
        public void RefForcedDoubleReferenced([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            // Forcing names the object on its first appearance; the second reference has to reuse that name rather than mint another.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var shared = new RefForcedNamedRecordable() { name = "delta", value = 1 };
            var root = new RefForcedPairRoot() { one = shared, two = shared };

            var deserialized = DoRecorderRoundTrip(root, mode, testSerializedResult: serialized =>
            {
                Assert.AreEqual(1, Regex.Matches(serialized, @"id=""delta""").Count);
                Assert.AreEqual(2, Regex.Matches(serialized, @"ref=""delta""").Count);
            });

            Assert.AreSame(deserialized.one, deserialized.two);
            Assert.AreEqual(1, deserialized.one.value);
        }

        public class RefForcedMixedRoot : Dec.IRecordable
        {
            public RefForcedRecordable unshared;
            public RefForcedRecordable shared;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref unshared, "unshared");
                record.Shared().Record(ref shared, "shared");
            }
        }

        [Test]
        public void RefForcedUnsharedThenShared()
        {
            // Forcing doesn't get to paper over the recorder's own sharing mismatch.
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var obj = new RefForcedRecordable() { value = 1 };
            var root = new RefForcedMixedRoot() { unshared = obj, shared = obj };

            ExpectWarningsAndErrors(() => Dec.Recorder.Write(root), wrn => wrn.Contains("IRefForce"), err => err.Contains("previously-seen unshared object"));
        }
    }

    [TestFixture]
    public class RecorderRefDecPath : Base
    {
        public class RefPathDec : Dec.Dec
        {
            public RecorderRef.RefValueRecordable member;

            [Dec.Setup]
            internal void LookupEnable(Action<string> reporter)
            {
                Dec.Database.DecLookupEnable(member);
            }
        }

        [SetUp]
        public void Setup()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RefPathDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RefPathDec decName=""TestDec"">
                        <member><value>7</value></member>
                    </RefPathDec>
                </Decs>");
            parser.Finish();
        }

        [Test]
        public void RefIdShadowedByDecPath()
        {
            // Dec paths are resolved before the refs table, so a ref that shares a name with one would otherwise hand back the dec's object instead of the recorded one.
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""RefPathDec.TestDec.member"" class=""DecTest.RecorderRef.RefValueRecordable""><value>1</value></Ref>
                  </refs>
                  <data>
                    <one ref=""RefPathDec.TestDec.member"" />
                    <two ref=""RefPathDec.TestDec.member"" />
                  </data>
                </Record>";

            RecorderRef.RefValueRootRecordable deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<RecorderRef.RefValueRootRecordable>(serialized), err => err.Contains("names both a dec path"));

            Assert.AreSame(deserialized.one, deserialized.two);
            Assert.AreEqual(1, deserialized.one.value);
            Assert.AreNotSame(Dec.Database<RefPathDec>.Get("TestDec").member, deserialized.one);
        }

        [Test]
        public void RefNameCollidesWithDecPath([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            var shared = new RecorderRef.RefNamedRecordable() { name = "RefPathDec.TestDec.member", value = 1 };
            var root = new RecorderRef.RefNamedRootRecordable() { sharedA = shared, sharedB = shared };

            var deserialized = DoRecorderRoundTrip(root, mode, expectWriteWarnings: mode != RecorderMode.Clone && mode != RecorderMode.Checksum, warningValidator: wrn => wrn.Contains("is also a dec path"));

            Assert.AreSame(deserialized.sharedA, deserialized.sharedB);
            Assert.AreEqual(1, deserialized.sharedA.value);
            Assert.AreNotSame(Dec.Database<RefPathDec>.Get("TestDec").member, deserialized.sharedA);
        }
    }
}
