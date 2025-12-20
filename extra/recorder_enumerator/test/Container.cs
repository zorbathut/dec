#if NET6_0_OR_GREATER

using DecTest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    public class Container : Base
    {
        [Test]
        public void Array([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var data = new int[] { 1, 2, 3, 4, 5 };
            var source = ((IEnumerable<int>)data).GetEnumerator();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }

        [Test]
        public void List([ValuesExcept(RecorderMode.Validation)] RecorderMode recorderMode)
        {
            var data = new List<int> { 1, 2, 3, 4, 5 };
            var source = data.GetEnumerator();
            source.MoveNext();
            var result = DoRecorderRoundTrip(source, recorderMode);
            Assert.IsTrue(Util.AreEquivalentEnumerators(source, result));
        }
    }
}
#endif
