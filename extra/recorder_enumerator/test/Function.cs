namespace RecorderEnumeratorTest
{
    using DecTest;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    public class Function : Base
    {
        static int beef() { return 42; }

        [Dec.RecorderEnumerator.Recordable]
        private IEnumerable<int> PrintSomeNumbers()
        {
            for (int i = 0; i < 10; ++i)
            {
                yield return i;
            }

            yield return 1;
            yield return 1;
            yield return 2;
            yield return 3;
            yield return 5;
            yield return 8;

            for (int j = 0; j < 20; j += 2)
            {
                yield return j;
            }
        }

        [Test]
        public void LocalFunction([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode, [Values(0, 4, 13, 25, 36)] int index)
        {
            var val = PrintSomeNumbers().GetEnumerator();
            for (int i = 0; i < index; ++i)
            {
                val.MoveNext();
            }

            var result = DoRecorderRoundTrip(val, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(val, result));
        }

        [Dec.RecorderEnumerator.Recordable]
        private IEnumerable<int> PrintMoreNumbers<T, U, V>()
        {
            yield return typeof(T).GetHashCode();
            yield return typeof(U).GetHashCode();
            yield return typeof(V).GetHashCode();
        }

        [Test]
        public void LocalGenericFunction([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode, [Values(0, 1, 2, 3, 4)] int index)
        {
            var val = PrintMoreNumbers<string, int, Function>().GetEnumerator();
            for (int i = 0; i < index; ++i)
            {
                val.MoveNext();
            }

            var result = DoRecorderRoundTrip(val, recorderMode);

            Assert.IsTrue(Util.AreEquivalentEnumerators(val, result));
        }
    }
}
