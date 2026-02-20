using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
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

        // Dec hierarchy for cache race tests.
        [Dec.Abstract] public abstract class CacheBaseDec : Dec.Dec { }
        public class CacheDec1 : CacheBaseDec { }
        public class CacheDec2 : CacheBaseDec { }
        public class CacheDec3 : CacheBaseDec { }
        public class CacheDec4 : CacheBaseDec { }

        // Recordable that holds Dec references — serializing this triggers GetDecRootType.
        public class DecRefHolder : Dec.IRecordable
        {
            public CacheDec1 ref1;
            public CacheDec2 ref2;
            public CacheDec3 ref3;
            public CacheDec4 ref4;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref ref1, "ref1");
                recorder.Record(ref ref2, "ref2");
                recorder.Record(ref ref3, "ref3");
                recorder.Record(ref ref4, "ref4");
            }
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

            int threadCount = Math.Max(Environment.ProcessorCount, 4);
            int iterations = 20;
            int failedIterations = 0;

            for (int iter = 0; iter < iterations; iter++)
            {
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
                var exceptions = new ConcurrentBag<Exception>();

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

                if (!exceptions.IsEmpty)
                {
                    failedIterations++;
                }
            }

            Assert.AreEqual(0, failedIterations, $"Serialization.Initialize() race condition detected in {failedIterations}/{iterations} iterations");
        }

        [Test]
        public void ParallelDecDatabaseStatusCacheRace()
        {
            int threadCount = Math.Max(Environment.ProcessorCount, 4);
            int iterations = 20;
            int failedIterations = 0;

            for (int iter = 0; iter < iterations; iter++)
            {
                // Set up Dec types and parse instances into the database.
                UpdateTestParameters(new Dec.Config.UnitTestParameters
                {
                    explicitTypes = new Type[] { typeof(CacheDec1), typeof(CacheDec2), typeof(CacheDec3), typeof(CacheDec4) },
                });

                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <CacheDec1 decName=""D1"" />
                        <CacheDec2 decName=""D2"" />
                        <CacheDec3 decName=""D3"" />
                        <CacheDec4 decName=""D4"" />
                    </Decs>");
                parser.Finish();

                var holder = new DecRefHolder
                {
                    ref1 = Dec.Database<CacheDec1>.Get("D1"),
                    ref2 = Dec.Database<CacheDec2>.Get("D2"),
                    ref3 = Dec.Database<CacheDec3>.Get("D3"),
                    ref4 = Dec.Database<CacheDec4>.Get("D4"),
                };

                // Clear the type-hierarchy caches so every thread races to re-populate them.
                // The race: multiple threads call GetDecRootType → GetDecDatabaseStatus simultaneously,
                // all see cache misses, and concurrently TryGetValue/Add on a plain Dictionary —
                // which can corrupt its internal state.
                Dec.UtilType.ClearStaticCachesForTest();

                var barrier = new Barrier(threadCount);
                var threads = new Thread[threadCount];
                var exceptions = new ConcurrentBag<Exception>();

                for (int i = 0; i < threadCount; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        barrier.SignalAndWait();
                        try
                        {
                            Dec.Recorder.Checksum(holder);
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

                if (!exceptions.IsEmpty)
                {
                    failedIterations++;
                }

                Dec.Database.Clear();
            }

            Assert.AreEqual(0, failedIterations, $"GetDecDatabaseStatus/GetDecRootType cache race detected in {failedIterations}/{iterations} iterations");
        }
    }
}
