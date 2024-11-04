using NUnit.Framework;
using System;
using System.Collections.Generic;

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

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteErrors: true, expectReadErrors: true);

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

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteErrors: true, expectReadErrors: true);

            // In this case, we factory, then kinda fuck up the sharing weirdly
            Assert.AreEqual(8, deserialized.cargo.recorded);
            Assert.AreEqual(5, deserialized.cargo.unrecorded);

            if (mode != RecorderMode.Clone)
            {
                Assert.AreEqual(0, deserialized.cargoLink.recorded);
                Assert.AreEqual(0, deserialized.cargoLink.unrecorded);
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
            var deserialized = DoRecorderRoundTrip(rec, mode, expectReadErrors: mode != RecorderMode.Clone);

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
            ExpectErrors(() => deserialized = Dec.Recorder.Read<UnexpectedRefBase>(serialized));

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

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true);
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

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true);
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

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true);
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

            var deserialized = DoRecorderRoundTrip(rec, mode, expectWriteWarnings: true, expectReadWarnings: true);
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
    }
}
