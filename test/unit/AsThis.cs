namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
    }
}
