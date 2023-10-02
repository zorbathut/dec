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

        [Dec.RecorderEnumerator.RecordableEnumerable]
        public IEnumerable<int> FakeRange()
        {
            for (int i = 0; i < 20; ++i)
            {
                yield return i;
            }
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

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void SelectEnumerable([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var range = FakeRange().Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void SelectRange([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var range = Enumerable.Range(0, 20).Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void SelectArray([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var array = Enumerable.Range(0, 20).ToArray();
            var range = array.Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void SelectList([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var list = Enumerable.Range(0, 20).ToList();
            var range = list.Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void SelectMany([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            int k = 3;
            var range = Enumerable.Range(0, 5).SelectMany(i => Enumerable.Range(0, i)).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Ignore("This is hard to support due to Join using Lookup and Grouping.")]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void JoinEnumeratorTest([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var outer = Enumerable.Range(1, 10);
            var inner = Enumerable.Range(1, 20);
            Func<int, int> outerKeySelector = i => i;
            Func<int, int> innerKeySelector = i => i % 3;
            Func<int, int, string> resultSelector = (a, b) => $"({a}, {b})";

            var joinEnumerator = outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector).GetEnumerator();
            joinEnumerator.MoveNext();
            joinEnumerator.MoveNext();
            joinEnumerator.MoveNext();

            var result = DoRecorderRoundTrip(joinEnumerator, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(joinEnumerator, result));
        }

        [Test]
        [Ignore("This is hard to support due to Join using Lookup and Grouping.")]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void GroupJoinEnumeratorTest([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var outer = Enumerable.Range(1, 3);
            var inner = Enumerable.Range(1, 5);
            Func<int, int> outerKeySelector = i => i;
            Func<int, int> innerKeySelector = i => i % 3;
            Func<int, IEnumerable<int>, string> resultSelector = (key, group) => $"({key}, {string.Join(", ", group)})";

            var groupJoinEnumerator = outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector).GetEnumerator();
            groupJoinEnumerator.MoveNext();
            groupJoinEnumerator.MoveNext();
            groupJoinEnumerator.MoveNext();

            var result = DoRecorderRoundTrip(groupJoinEnumerator, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(groupJoinEnumerator, result));
        }
    }
}
