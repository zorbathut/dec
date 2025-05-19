using System.Collections.Generic;
using DecTest;
using NUnit.Framework;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    public class Enumer : Base
    {
        class DataReporter : Dec.IRecordable
        {
            public int data;
            public IEnumerator<int> reporter;

            [Dec.RecorderEnumerator.RecordableEnumerable]
            public IEnumerable<int> PrintTheNumber()
            {
                while (true)
                {
                    yield return data;
                }
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref data, nameof(data));
                recorder.Record(ref reporter, nameof(reporter));
            }
        }

        [Test]
        public void EnumerableMember([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var dataReporter = new DataReporter();
            dataReporter.reporter = dataReporter.PrintTheNumber().GetEnumerator();

            var clone = DoRecorderRoundTrip(dataReporter, recorderMode);

            clone.data = 42;
            clone.reporter.MoveNext();
            Assert.AreEqual(clone.data, clone.reporter.Current);

            clone.data = 100;
            clone.reporter.MoveNext();
            Assert.AreEqual(clone.data, clone.reporter.Current);

            clone.data = -10;
            clone.reporter.MoveNext();
            Assert.AreEqual(clone.data, clone.reporter.Current);
        }

        [Test]
        public void EmptyEnumerator([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var lienum = new List<int>.Enumerator();
            var clone = DoRecorderRoundTrip(lienum, recorderMode);

            // there's not much I can really test here
        }
    }
}
