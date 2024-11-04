#if NET7_0_OR_GREATER

using DecTest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    public class DelegateTest : Base
    {
        static int ReturnNumber() { return 42; }
        static int ReturnNumber2() { return 100; }

        [Test]
        public void FuncMultipleExternal([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            // I'm slightly worried that the way I'm generating functions could create *one* function, then overwrite it, so here's a test for that.
            var fa = new Func<int>(ReturnNumber);
            var fb = new Func<int>(ReturnNumber2);

            var pair = (fa, fb);

            var result = DoRecorderRoundTrip(pair, recorderMode);

            Assert.AreEqual(42, result.fa());
            Assert.AreEqual(100, result.fb());

            Assert.AreNotSame(pair.fa, result.fa);
            Assert.AreNotSame(pair.fb, result.fb);
        }

        [Test]
        [Dec.RecorderEnumerator.RecordableClosures]
        public void FuncMultipleInternal([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            // I'm slightly worried that the way I'm generating functions could create *one* function, then overwrite it, so here's a test for that.
            Func<int> fa = () => 42;
            Func<int> fb = () => 100;

            var pair = (fa, fb);

            var result = DoRecorderRoundTrip(pair, recorderMode);

            Assert.AreEqual(42, result.fa());
            Assert.AreEqual(100, result.fb());

            Assert.AreNotSame(pair.fa, result.fa);
            Assert.AreNotSame(pair.fb, result.fb);
        }

        class ActionSideEffectModule : Dec.IRecordable
        {
            public int value = 0;
            public Action action = null;

            public ActionSideEffectModule()
            {
                action = () => value = 42;
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
                recorder.Record(ref action, "action");
            }
        }

        [Test]
        public void ActionWithClosureSideEffects([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var asem = new ActionSideEffectModule();

            var result = DoRecorderRoundTrip(asem, recorderMode);

            Assert.AreNotSame(asem, result);

            Assert.AreEqual(0, result.value);
            result.action();
            Assert.AreEqual(42, result.value);

            Assert.AreEqual(0, asem.value);
            asem.action();
            Assert.AreEqual(42, asem.value);
        }
    }
}
#endif
