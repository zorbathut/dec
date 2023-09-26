namespace RecorderEnumeratorTest
{
    using DecTest;
    using NUnit.Framework;
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
    }
}
