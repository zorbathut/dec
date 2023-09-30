namespace RecorderEnumeratorTest
{
    using DecTest;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Linq : Base
    {
        [Test]
        public void AreEquivalentValidation()
        {
            // just to make sure AreEquivalent works, like, at all

            {
                var lhs = Enumerable.Range(5, 15).GetEnumerator();
                var rhs = Enumerable.Range(5, 15).GetEnumerator();

                Assert.IsTrue(Util.AreEquivalentEnumerators(lhs, rhs));
            }

            {
                var lhs = Enumerable.Range(5, 15).GetEnumerator();
                var rhs = Enumerable.Range(5, 15).GetEnumerator();

                lhs.MoveNext();
                rhs.MoveNext();

                Assert.IsTrue(Util.AreEquivalentEnumerators(lhs, rhs));
            }

            {
                var lhs = Enumerable.Range(5, 15).GetEnumerator();
                var rhs = Enumerable.Range(5, 15).GetEnumerator();

                lhs.MoveNext();

                Assert.IsFalse(Util.AreEquivalentEnumerators(lhs, rhs));
            }

            {
                var lhs = Enumerable.Range(5, 15).GetEnumerator();
                var rhs = Enumerable.Range(5, 15).GetEnumerator();

                rhs.MoveNext();

                Assert.IsFalse(Util.AreEquivalentEnumerators(lhs, rhs));
            }

            {
                var lhs = Enumerable.Range(5, 16).GetEnumerator();
                var rhs = Enumerable.Range(5, 15).GetEnumerator();

                Assert.IsFalse(Util.AreEquivalentEnumerators(lhs, rhs));
            }

            {
                var lhs = Enumerable.Range(5, 15).GetEnumerator();
                var rhs = Enumerable.Range(5, 16).GetEnumerator();

                Assert.IsFalse(Util.AreEquivalentEnumerators(lhs, rhs));
            }

            // once I find something that supports Reset(), gotta make sure that test works as well
        }

        [Test]
        public void Range([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var range = Enumerable.Range(5, 15).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void WhereEnumerable([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var range = Enumerable.Range(0, 20).Where(i => i % k == 0).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void WhereArray([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var array = Enumerable.Range(0, 20).ToArray();
            var range = array.Where(i => i % k == 0).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void WhereList([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var list = Enumerable.Range(0, 20).ToList();
            var range = list.Where(i => i % k == 0).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void WhereListOutOfDate([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var list = Enumerable.Range(0, 5).ToList();
            var range = list.GetEnumerator();
            list.Add(1);

            var result = DoRecorderRoundTrip((list, range), recorderMode);

            Assert.AreEqual(list, result.list);

            try
            {
                result.range.MoveNext();
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException)
            {
            }
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void WhereSelectEnumerable([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            int m = 2;
            var range = Enumerable.Range(0, 20).Where(i => i % k == 0).Select(i => i * m).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void WhereSelectArray([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            int m = 2;
            var array = Enumerable.Range(0, 20).ToArray();
            var range = array.Where(i => i % k == 0).Select(i => i * m).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void WhereSelectList([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            int m = 2;
            var list = Enumerable.Range(0, 20).ToList();
            var range = list.Where(i => i % k == 0).Select(i => i * m).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void ObjectEnumerable([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var list = new List<StubRecordable>();
            list.Add(new StubRecordable());
            var range = list.GetEnumerator();
            range.MoveNext();
            Assert.AreSame(range.Current, list[0]);

            var result = DoRecorderRoundTrip((list, range), recorderMode);

            Assert.AreSame(result.range.Current, result.list[0]);
        }
    }
}
