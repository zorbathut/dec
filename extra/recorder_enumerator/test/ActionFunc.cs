namespace RecorderEnumeratorTest
{
    using DecTest;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    public class ActionFunc : Base
    {
        static int ReturnNumber() { return 42; }
        static int ReturnNumber2() { return 100; }

        [Test]
        public void MultipleExternal([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
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
        public void MultipleInternal([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            // I'm slightly worried that the way I'm generating functions could create *one* function, then overwrite it, so here's a test for that.
            var fa = () => 42;
            var fb = () => 100;

            var pair = (fa, fb);

            var result = DoRecorderRoundTrip(pair, recorderMode);

            Assert.AreEqual(42, result.fa());
            Assert.AreEqual(100, result.fb());

            Assert.AreNotSame(pair.fa, result.fa);
            Assert.AreNotSame(pair.fb, result.fb);
        }
    }
}
