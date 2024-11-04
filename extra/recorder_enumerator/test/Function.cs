using DecTest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public class Function : Base
    {
        static int beef() { return 42; }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private static IEnumerable<int> PrintSomeNumbers()
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

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private static IEnumerable<int> PrintMoreNumbers<T, U, V>()
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

        class RecordableLocalClosureClass : Dec.IRecordable
        {
            public Func<bool> func;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref func, nameof(func));
            }
        }

        private static bool RecordableLocalClosureVal = false;
        [Test]
        public void RecordableLocalClosure([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            RecordableLocalClosureClass rlcc = new RecordableLocalClosureClass();
            Func<bool> func = () => RecordableLocalClosureVal;
            rlcc.func = func;

            var dupe = DoRecorderRoundTrip(rlcc, recorderMode);

            RecordableLocalClosureVal = false;
            Assert.AreEqual(rlcc.func(), dupe.func());
            RecordableLocalClosureVal = true;
            Assert.AreEqual(rlcc.func(), dupe.func());
        }

        public static bool ReturnFalse()
        {
            return false;
        }
        public struct DoubleFunctionStruct : Dec.IRecordable
        {
            public Func<bool> one;
            public Func<bool> two;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref one, nameof(one));
                recorder.Record(ref two, nameof(two));
            }
        }

        [Test]
        public void DoubleFunction([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            // this is mostly to ensure that delegates are handled as value types; they're doing GetHashCode()/Equals() magic to make them seem like it, even if they're not
            // and I'm just gonna play along and pretend they're value types
            // hopefully I don't regret this but seriously if you're using `unsafe` pointers to test equality then I don't know what you were expecting
            var val = new DoubleFunctionStruct();
            val.one = ReturnFalse;
            val.two = ReturnFalse;

            var result = DoRecorderRoundTrip(val, recorderMode);
        }

        [Dec.RecorderEnumerator.RecordableClosures]
        static class ClosureHolder
        {
            public static Func<int> GenerateClosure<T>(T val)
            {
                return () => val.GetHashCode();
            }
        }

        [Test]
        public void GenericClosure([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var val = ClosureHolder.GenerateClosure(42);
            var result = DoRecorderRoundTrip(val, recorderMode);

            Assert.AreEqual(val(), result());
        }
    }
}
