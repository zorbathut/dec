using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class RecorderShared : Base
    {
        public class FSConflictPayload : Dec.IRecordable
        {
            public int unrecorded = 0;
            public int recorded = 0;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref recorded, "recorded");
            }
        }
        static Dictionary<Type, Func<Type, object>> FSConflictFactory = new Dictionary<Type, Func<Type, object>>()
        {
            { typeof(FSConflictPayload), t => new FSConflictPayload { unrecorded = 5 } }
        };

        public class FactoryThenSharedRecordable : Dec.IRecordable
        {
            public FSConflictPayload cargo;
            public FSConflictPayload cargoLink;

            public void Record(Dec.Recorder recorder)
            {
                recorder.WithFactory(FSConflictFactory).Shared().Record(ref cargo, "cargo");
                recorder.Shared().Record(ref cargoLink, "cargoLink");
            }
        }

        public class SharedThenFactoryRecordable : Dec.IRecordable
        {
            public FSConflictPayload cargo;
            public FSConflictPayload cargoLink;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().WithFactory(FSConflictFactory).Record(ref cargo, "cargo");
                recorder.Shared().Record(ref cargoLink, "cargoLink");
            }
        }

        [Test]
        public void FSCFactoryThenShared([Values] RecorderMode mode)
        {
            var rec = new FactoryThenSharedRecordable();

            rec.cargo = new FSConflictPayload();
            rec.cargo.recorded = 8;

            rec.cargoLink = rec.cargo;

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteErrors: true, expectReadErrors: true, errorValidator: err => err.Contains("Recorder.Shared() called on a WithFactory") || err.Contains("shared objects do not work in simple mode"));

            // In this case, we don't factory, but do share
            Assert.AreEqual(8, deserialized.cargo.recorded);
            Assert.AreEqual(0, deserialized.cargo.unrecorded);
            Assert.AreSame(rec.cargo, rec.cargoLink);
        }

        [Test]
        public void FSCSharedThenFactory([Values] RecorderMode mode)
        {
            var rec = new SharedThenFactoryRecordable();

            rec.cargo = new FSConflictPayload();
            rec.cargo.recorded = 8;

            rec.cargoLink = rec.cargo;

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteErrors: true, expectReadErrors: true, errorValidator: err => err.Contains("Recorder.WithFactory() called on a Shared") || err.Contains("previously-seen unshared object") || err.Contains("shared objects do not work in simple mode"));

            // In this case, we factory, and the link has nothing it can share
            Assert.AreEqual(8, deserialized.cargo.recorded);
            Assert.AreEqual(5, deserialized.cargo.unrecorded);

            if (mode != RecorderMode.Clone && mode != RecorderMode.Checksum)
            {
                Assert.IsNull(deserialized.cargoLink);
            }
            else
            {
                // clone currently doesn't care that much though
                Assert.AreEqual(8, deserialized.cargoLink.recorded);
                Assert.AreEqual(5, deserialized.cargoLink.unrecorded);
            }
        }

        public class NonNullRecordable : Dec.IRecordable
        {
            public List<int> cargo = new List<int> { 42 };

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref cargo, "cargo");
            }
        }

        [Test]
        public void NonNull([Values] RecorderMode mode)
        {
            var rec = new NonNullRecordable();

            rec.cargo = new List<int> { 100 };

            // clone probably *should* error on this, but right now it doesn't, it has unspecified behavior with the interaction of shared and non-null
            var deserialized = DoRecorderRoundTrip(rec, mode, expectReadErrors: mode != RecorderMode.Clone && mode != RecorderMode.Checksum, errorValidator: err => err.Contains("provided with non-null default object"));

            Assert.AreEqual(deserialized.cargo, rec.cargo);
        }

        class UnexpectedRefBase : Dec.IRecordable
        {
            public StubRecordable stub;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref stub, "stub");
            }
        }

        [Test]
        public void UnexpectedRefInFile([Values] RecorderMode mode)
        {
            // make a file with .shared(), remove the .shared(), splice the file in

            // This all needs to be changed
            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""DecTest.Base.StubRecordable"" />
                  </refs>
                  <data>
                    <stub ref=""ref00000"" />
                  </data>
                </Record>";
            UnexpectedRefBase deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<UnexpectedRefBase>(serialized), err => err.Contains("Found a reference in a non-.Shared() context"));

            Assert.IsNotNull(deserialized.stub);
        }

        public class SharingClassRecorder : Dec.IRecordable
        {
            public List<int> item;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref item, "item");
            }
        }
        [Test]
        public void SharingClass([Values] RecorderMode mode)
        {
            var rec = new SharingClassRecorder();

            var deserialized = DoRecorderRoundTrip(rec, mode);
        }

        public class SharingIntRecorder : Dec.IRecordable
        {
            public int item;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref item, "item");
            }
        }
        [Test]
        public void SharingInt([Values] RecorderMode mode)
        {
            var rec = new SharingIntRecorder();

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true, warningValidator: wrn => wrn.Contains("Value type") && wrn.Contains("tagged as Shared"));
        }

        public class SharingDecRecorder : Dec.IRecordable
        {
            public StubDec item;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref item, "item");
            }
        }
        [Test]
        public void SharingDecClass([Values] RecorderMode mode)
        {
            var rec = new SharingDecRecorder();

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true, warningValidator: wrn => wrn.Contains("Value type") && wrn.Contains("tagged as Shared"));
        }

        public class SharingStringRecorder : Dec.IRecordable
        {
            public string item;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref item, "item");
            }
        }
        [Test]
        public void SharingStringClass([Values] RecorderMode mode)
        {
            var rec = new SharingStringRecorder();

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true, warningValidator: wrn => wrn.Contains("Value type") && wrn.Contains("tagged as Shared"));
        }

        public class SharingTypeRecorder : Dec.IRecordable
        {
            public Type item;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref item, "item");
            }
        }
        [Test]
        public void SharingTypeClass([Values] RecorderMode mode)
        {
            var rec = new SharingTypeRecorder();

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true, warningValidator: wrn => wrn.Contains("Value type") && wrn.Contains("tagged as Shared"));
        }

        public class SharedRoot : Dec.IRecordable
        {
            public SharedRoot root;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref root, "root");
            }
        }
        [Test]
        public void SharedRootClass([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            var rec = new SharedRoot();
            rec.root = rec;

            var deserialized = DoRecorderRoundTrip(rec, mode);

            Assert.AreSame(deserialized, deserialized.root);
        }

        public struct BoxedRecordable : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }
        }

        [Test]
        public void BoxReachedTwice([Values] RecorderMode mode)
        {
            // A value type is copied at every position, so a box reached twice comes back as two.
            object box = new BoxedRecordable { value = 5 };
            var list = new List<object> { box, box };

            var deserialized = DoRecorderRoundTrip(list, mode);

            Assert.AreEqual(5, ((BoxedRecordable)deserialized[0]).value);
            Assert.AreEqual(5, ((BoxedRecordable)deserialized[1]).value);
            Assert.AreNotSame(deserialized[0], deserialized[1]);
        }

        public class EqualsAlwaysSharedHolder : Dec.IRecordable
        {
            public StubRecordableEqualsAlways a;
            public StubRecordableEqualsAlways b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, "a");
                recorder.Shared().Record(ref b, "b");
            }
        }

        [Test]
        public void DistinctEqualShared([Values] RecorderMode mode)
        {
            var holder = new EqualsAlwaysSharedHolder { a = new StubRecordableEqualsAlways { data = 1 }, b = new StubRecordableEqualsAlways { data = 2 } };

            var deserialized = DoRecorderRoundTrip(holder, mode);

            Assert.AreNotSame(deserialized.a, deserialized.b);
            Assert.AreEqual(1, deserialized.a.data);
            Assert.AreEqual(2, deserialized.b.data);
        }

        [Test]
        public void SameEqualShared([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            var item = new StubRecordableEqualsAlways { data = 1 };
            var holder = new EqualsAlwaysSharedHolder { a = item, b = item };

            var deserialized = DoRecorderRoundTrip(holder, mode);

            Assert.AreSame(deserialized.a, deserialized.b);
            Assert.AreEqual(1, deserialized.a.data);
        }

        public class TupleListHolder : Dec.IRecordable
        {
            public List<Tuple<int, int>> list;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref list, "list");
            }
        }

        [Test]
        public void DistinctEqualUnshared([Values] RecorderMode mode)
        {
            // Tuple compares by contents, so these two are Equal without being the same object, and neither position is shared.
            var holder = new TupleListHolder { list = new List<Tuple<int, int>> { Tuple.Create(1, 2), Tuple.Create(1, 2) } };

            var deserialized = DoRecorderRoundTrip(holder, mode);

            Assert.AreEqual(2, deserialized.list.Count);
            Assert.AreEqual(Tuple.Create(1, 2), deserialized.list[0]);
            Assert.AreEqual(Tuple.Create(1, 2), deserialized.list[1]);
            Assert.AreNotSame(deserialized.list[0], deserialized.list[1]);
        }

        public struct EqualsAlwaysStruct : Dec.IRecordable
        {
            public StubRecordableEqualsAlways item;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref item, "item");
            }
        }

        public class EqualsAlwaysStructHolder : Dec.IRecordable
        {
            public List<EqualsAlwaysStruct> list;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref list, "list");
            }
        }

        [Test]
        public void DistinctEqualInStructs([Values] RecorderMode mode)
        {
            // The structs compare equal by value, since their items do, but each item is still its own object.
            var holder = new EqualsAlwaysStructHolder { list = new List<EqualsAlwaysStruct>
            {
                new EqualsAlwaysStruct { item = new StubRecordableEqualsAlways { data = 1 } },
                new EqualsAlwaysStruct { item = new StubRecordableEqualsAlways { data = 2 } },
            } };

            var deserialized = DoRecorderRoundTrip(holder, mode);

            Assert.AreEqual(2, deserialized.list.Count);
            Assert.AreNotSame(deserialized.list[0].item, deserialized.list[1].item);
            Assert.AreEqual(1, deserialized.list[0].item.data);
            Assert.AreEqual(2, deserialized.list[1].item.data);
        }
    }
}
