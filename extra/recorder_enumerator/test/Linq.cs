#if NET6_0_OR_GREATER

using DecTest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
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
        public static IEnumerable<int> FakeRange()
        {
            for (int i = 0; i < 20; ++i)
            {
                yield return i;
            }
        }

        [Test]
        public void Range([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var range = Enumerable.Range(5, 15).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        public void WhereEnumerable([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void WhereArray([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void WhereList([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void WhereListOutOfDate([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void WhereSelectEnumerable([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void WhereSelectArray([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void WhereSelectList([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void ObjectEnumerable([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void SelectEnumerable([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var range = FakeRange().Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        public void SelectRange([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var range = Enumerable.Range(0, 20).Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        public void SelectArray([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var array = Enumerable.Range(0, 20).ToArray();
            var range = array.Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        public void SelectList([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var list = Enumerable.Range(0, 20).ToList();
            var range = list.Select(i => i.ToString()).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        public void SelectMany([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var range = Enumerable.Range(0, 5).SelectMany(i => Enumerable.Range(0, i)).GetEnumerator();
            range.MoveNext();
            range.MoveNext();
            range.MoveNext();

            var result = DoRecorderRoundTrip(range, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(range, result));
        }

        [Test]
        [Ignore("This is hard to support due to Join using Lookup and Grouping.")]
        public void JoinEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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
        public void GroupJoinEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
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

        [Test]
        public void DistinctEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 40).Select(i => i % 5).Distinct().GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void DistinctByEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 40).Select(i => i % 5).DistinctBy(x => x % 4).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void UnionEnumerator2Test([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20).Select(x => x * 2);
            var second = Enumerable.Range(0, 20).Select(x => x * 3);
            var unionEnumerator = first.Union(second).GetEnumerator();
            unionEnumerator.MoveNext();
            unionEnumerator.MoveNext();
            unionEnumerator.MoveNext();
            var result = DoRecorderRoundTrip(unionEnumerator, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(unionEnumerator, result));
        }

        [Test]
        public void UnionEnumerator3Test([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20).Select(x => x * 2);
            var second = Enumerable.Range(0, 20).Select(x => x * 3);
            var third = Enumerable.Range(0, 20).Select(x => x * 5);
            var unionEnumerator = first.Union(second).Union(third).GetEnumerator();
            unionEnumerator.MoveNext();
            unionEnumerator.MoveNext();
            unionEnumerator.MoveNext();
            var result = DoRecorderRoundTrip(unionEnumerator, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(unionEnumerator, result));
        }

        [Test]
        public void IntersectEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20);
            var second = Enumerable.Range(15, 20);
            var intersectEnumerator = first.Intersect(second).GetEnumerator();
            intersectEnumerator.MoveNext();
            intersectEnumerator.MoveNext();
            intersectEnumerator.MoveNext();
            var result = DoRecorderRoundTrip(intersectEnumerator, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(intersectEnumerator, result));
        }

        [Test]
        public void IntersectByEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20);
            var second = Enumerable.Range(15, 20);
            var intersectEnumerator = first.IntersectBy(second, x => x % 17).GetEnumerator();
            intersectEnumerator.MoveNext();
            intersectEnumerator.MoveNext();
            intersectEnumerator.MoveNext();
            var result = DoRecorderRoundTrip(intersectEnumerator, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(intersectEnumerator, result));
        }

        [Test]
        public void ExceptEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20);
            var second = Enumerable.Range(15, 20);
            var exceptEnumerator = first.Except(second).GetEnumerator();
            exceptEnumerator.MoveNext();
            exceptEnumerator.MoveNext();
            exceptEnumerator.MoveNext();
            var result = DoRecorderRoundTrip(exceptEnumerator, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(exceptEnumerator, result));
        }

        [Test]
        public void ExceptByEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20);
            var second = Enumerable.Range(15, 20);
            var exceptEnumerator = first.ExceptBy(second, x => x % 3).GetEnumerator();
            exceptEnumerator.MoveNext();
            exceptEnumerator.MoveNext();
            exceptEnumerator.MoveNext();
            var result = DoRecorderRoundTrip(exceptEnumerator, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(exceptEnumerator, result));
        }

        [Test]
        public void OrderByEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).OrderBy(i => i % 3).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void OrderByDescendingEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).OrderByDescending(i => i % 3).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void ThenByEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).OrderBy(i => i % 3).ThenBy(i => i).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void ThenByDescendingEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).OrderBy(i => i % 3).ThenByDescending(i => i).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void ReverseEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).Reverse().GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        [Ignore("This is hard to support due to Grouping.")]
        public void GroupByEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).GroupBy(i => i % 3).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void OfTypeEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var mixedSource = new List<object> { 0, 1, "two", 3, "four", 5 };
            var source = mixedSource.OfType<int>().GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void CastEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var objectSource = new List<object> { 0, 1, 2, 3, 4, 5 };
            var source = objectSource.Cast<int>().GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void TakeEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).Take(10).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void SkipEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).Skip(10).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void TakeWhileEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).TakeWhile(i => i < 10).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void SkipWhileEnumeratorTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var source = Enumerable.Range(0, 20).SkipWhile(i => i < 10).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void ConcatEnumerator2Test([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20);
            var second = Enumerable.Range(20, 20);
            var source = first.Concat(second).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void ConcatEnumeratorNTest([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode recorderMode)
        {
            var first = Enumerable.Range(0, 20);
            var second = Enumerable.Range(20, 20);
            var third = Enumerable.Range(40, 20);
            var source = first.Concat(second).Concat(third).GetEnumerator();
            source.MoveNext();
            source.MoveNext();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }
    }
}
#endif
