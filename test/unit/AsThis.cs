using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class AsThis : Base
    {
        public class ListAsThisRecordable : Dec.IRecordable
        {
            public List<int> data;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref data);
            }
        }

        [Test]
        public void Basic([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var lat = new ListAsThisRecordable();
            lat.data = new List<int>() { 1, 1, 2, 3, 5, 8, 13, 21 };

            var deserialized = DoRecorderRoundTrip(lat, mode);

            Assert.AreEqual(lat.data, deserialized.data);
        }

        public class ListAsThisMultiRecordable : Dec.IRecordable
        {
            public List<int> data;
            public int data2;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref data);
                recorder.RecordAsThis(ref data2);
            }
        }

        [Test]
        public void AsThisMulti([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var lat = new ListAsThisMultiRecordable();
            lat.data = new List<int>() { 1, 1, 2, 3, 5, 8, 13, 21 };
            lat.data2 = 19;

            var deserialized = DoRecorderRoundTrip(lat, mode, expectReadErrors: true, expectWriteErrors: true);

            Assert.AreEqual(lat.data, deserialized.data);
        }

        public class ListAsThisPreRecordable : Dec.IRecordable
        {
            public List<int> data;
            public int data2;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref data, "data");
                recorder.RecordAsThis(ref data2);
            }
        }

        [Test]
        public void AsThisPre([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var lat = new ListAsThisPreRecordable();
            lat.data = new List<int>() { 1, 1, 2, 3, 5, 8, 13, 21 };
            lat.data2 = 19;

            var deserialized = DoRecorderRoundTrip(lat, mode, expectReadErrors: true, expectWriteErrors: true);

            Assert.AreEqual(lat.data, deserialized.data);
        }

        public class ListAsThisPostRecordable : Dec.IRecordable
        {
            public List<int> data;
            public int data2;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref data);
                recorder.Record(ref data2, "data2");
            }
        }

        [Test]
        public void AsThisPost([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var lat = new ListAsThisPostRecordable();
            lat.data = new List<int>() { 1, 1, 2, 3, 5, 8, 13, 21 };
            lat.data2 = 19;

            var deserialized = DoRecorderRoundTrip(lat, mode, expectReadErrors: true, expectWriteErrors: true);

            Assert.AreEqual(lat.data, deserialized.data);
        }

        public class ThisThenClassOuter : Dec.IRecordable
        {
            public ThisThenClassInnerBase data;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref data);
            }
        }
        public class ThisThenClassInnerBase : Dec.IRecordable
        {
            public void Record(Dec.Recorder recorder) { }
        }
        public class ThisThenClassInnerDerived : ThisThenClassInnerBase { }

        // This test, as well as ClassThenThis, test for an issue that happens when using RecordAsThis()
        // RecordAsThis() effectively smooshes multiple parts of a hierarchy together into a single class
        // however, this precludes class overrides since it wouldn't know which step to apply it to
        // in theory this can be fixed by using `class` for the last one and inventing new tags, class-1 class-2 etc, for previous steps
        // in practice I currently do not think this is important enough to mess with.
        // or maybe doing this in some chained format, so you have `class,class,class`?
        // eugh that sucks too
        [Test]
        public void ThisThenClass([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var item = new ThisThenClassOuter();
            item.data = new ThisThenClassInnerDerived();

            var deserialized = DoRecorderRoundTrip(item, mode, expectWriteErrors: mode != RecorderMode.Clone);
        }

        public class ClassThenThisOuterBase : Dec.IRecordable
        {
            public Stub data;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref data);
            }
        }
        public class ClassThenThisOuterDerived : ClassThenThisOuterBase { }

        [Test]
        public void InheritedMember([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var item = new ClassThenThisOuterDerived();
            ClassThenThisOuterBase itemBase = item;

            var deserialized = DoRecorderRoundTrip(itemBase, mode, expectReadErrors: mode != RecorderMode.Clone, expectWriteErrors: mode != RecorderMode.Clone);
        }

        public class Inner : Dec.IRecordable
        {
            public void Record(Dec.Recorder recorder)
            {
            }
        }

        public class Mid : Dec.IRecordable
        {
            public Inner inner;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().RecordAsThis(ref inner);
            }
        }

        public class Outer : Dec.IRecordable
        {
            public Mid mid;
            public Inner inner;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref mid, "mid");
                recorder.Shared().Record(ref inner, "inner");
            }
        }

        [Test]
        public void MultiInner([Values] RecorderMode mode)
        {
            var item = new Outer();
            item.mid = new Mid();
            item.mid.inner = item.inner = new Inner();

            // be sweet if this worked, wouldn't it?
            // doesn't though
            var deserialized = DoRecorderRoundTrip(item, mode, expectWriteErrors: true, expectReadErrors: true);
        }
    }
}
