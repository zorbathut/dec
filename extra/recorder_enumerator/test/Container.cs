#if NET6_0_OR_GREATER

using DecTest;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private static IEnumerable<string> IterateList(List<string> data)
        {
            foreach (var item in data)
            {
                yield return item;
            }
        }

        [Test]
        public void ListEnumeratorInStateMachineConcurrent()
        {
            // Pre-initialize serialization so the concurrent test only races on ConverterFor, not on Initialize()
            Dec.Recorder.Checksum(42);

            var errors = new ConcurrentBag<string>();

            // Install thread-safe error "handlers"
            Dec.Config.ErrorHandler = str => errors.Add(str);
            Dec.Config.ExceptionHandler = ex => errors.Add(ex.ToString());

            int threadCount = 32;
            var barrier = new Barrier(threadCount);

            Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, i =>
            {
                var data = new List<string> { "one", "two", "three", "four", "five" };
                var source = IterateList(data).GetEnumerator();
                source.MoveNext();

                // Synchronize all threads to maximize chance of concurrent ConverterFor access
                barrier.SignalAndWait();

                Dec.Recorder.Checksum(source);
            });

            var compositionErrors = errors.Where(e => e.Contains("Couldn't find a composition method")).ToList();
            Assert.IsEmpty(compositionErrors, $"Found {compositionErrors.Count} converter lookup failures due to concurrent cache access");
        }
    }
}
#endif
