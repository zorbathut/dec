using System;
using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Threading : Base
    {
        // A plain class with no IRecordable - requires a converter to serialize.
        public class ConverterTargetType
        {
            public int value;
        }

        public class ConverterTargetTypeConverter : Dec.ConverterString<ConverterTargetType>
        {
            public override string Write(ConverterTargetType input)
            {
                return input.value.ToString();
            }

            public override ConverterTargetType Read(string input, Dec.Context context)
            {
                return new ConverterTargetType { value = int.Parse(input) };
            }
        }

        [Test]
        public void ParallelInitializeRace()
        {
            // Set up test parameters with our converter.
            UpdateTestParameters(new Dec.Config.UnitTestParameters
            {
                explicitTypes = new Type[] { },
                explicitConverters = new Type[] { typeof(ConverterTargetTypeConverter) },
            });

            // Verify it works single-threaded first.
            Dec.Recorder.Checksum(new ConverterTargetType { value = 42 });

            // Thread-safe error/exception collection for the parallel section.
            var errors = new ConcurrentBag<string>();
            var exceptions = new ConcurrentBag<Exception>();

            Dec.Config.ErrorHandler = str => errors.Add(str);
            Dec.Config.WarningHandler = str => { };
            Dec.Config.ExceptionHandler = e => exceptions.Add(e);

            int threadCount = Math.Max(Environment.ProcessorCount, 4);
            int iterations = 200;
            int failedIterations = 0;

            for (int iter = 0; iter < iterations; iter++)
            {
                errors = new ConcurrentBag<string>();
                exceptions = new ConcurrentBag<Exception>();

                // Reset serialization state so Initialize() must run again.
                // Database.Clear() calls Serialization.Clear() internally.
                Dec.Database.Clear();

                // Launch threads that all try to use a converter simultaneously.
                // The race: Thread A enters Initialize(), sets ConverterInitialized = true,
                // then creates a new empty ConverterObjects dict and starts populating it.
                // Thread B sees ConverterInitialized == true, skips Initialize(), and proceeds
                // to use the empty (not yet populated) ConverterObjects dictionary.
                var barrier = new Barrier(threadCount);
                var threads = new Thread[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        barrier.SignalAndWait();
                        try
                        {
                            Dec.Recorder.Checksum(new ConverterTargetType { value = 42 });
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        }
                    });
                    threads[i].Start();
                }

                foreach (var t in threads)
                {
                    t.Join();
                }

                if (!errors.IsEmpty || !exceptions.IsEmpty)
                {
                    failedIterations++;
                }
            }

            Assert.AreEqual(0, failedIterations, $"Serialization.Initialize() race condition detected in {failedIterations}/{iterations} iterations");
        }
    }
}
