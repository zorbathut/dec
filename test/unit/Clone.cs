using System.Collections.Generic;
using System.Linq;
using Dec;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Clone : Base
    {
        public class CloneWithRecordableClass : IRecordable
        {
            public void Record(Dec.Recorder recorder) { }
        }

        public struct CloneWithRecordableStruct : IRecordable
        {
            public List<int> list;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref list, "list");
            }
        }

        [Dec.CloneClassAsSharedRef]
        public class CloneWithAssignmentClass
        {

        }

        [Dec.CloneStructPiecewise]
        public struct CloneWithAssignmentStruct
        {
            public List<int> list;
        }


        [Dec.CloneClassAsSharedRef]
        public class CloneWithAssignmentResolutionClass : IRecordable
        {
            public void Record(Dec.Recorder recorder) { }
        }

        [Dec.CloneStructPiecewise]
        public struct CloneWithAssignmentResolutionStruct : IRecordable
        {
            public List<int> list;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref list, "list");
            }
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithRecordableClass()
        {
            var cwrc = new CloneWithRecordableClass();
            var cwrcClone = Dec.Recorder.Clone(cwrc);
            Assert.AreNotSame(cwrc, cwrcClone);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithRecordableStruct()
        {
            var cwrs = new CloneWithRecordableStruct();
            cwrs.list = new List<int>();
            var cwrsClone = Dec.Recorder.Clone(cwrs);
            Assert.AreNotSame(cwrs, cwrsClone);
            Assert.AreNotSame(cwrs.list, cwrsClone.list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentClass()
        {
            var cwac = new CloneWithAssignmentClass();
            var cwacClone = Dec.Recorder.Clone(cwac);
            Assert.AreSame(cwac, cwacClone);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentStruct()
        {
            var cwas = new CloneWithAssignmentStruct();
            cwas.list = new List<int>();
            var cwasClone = Dec.Recorder.Clone(cwas);
            Assert.AreNotSame(cwas, cwasClone);
            Assert.AreSame(cwas.list, cwasClone.list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentResolutionClass()
        {
            var cwarc = new CloneWithAssignmentResolutionClass();
            var cwarcClone = Dec.Recorder.Clone(cwarc);
            Assert.AreSame(cwarc, cwarcClone);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentResolutionStruct()
        {
            var cwars = new CloneWithAssignmentResolutionStruct();
            cwars.list = new List<int>();
            var cwarsClone = Dec.Recorder.Clone(cwars);
            Assert.AreNotSame(cwars, cwarsClone);
            Assert.AreSame(cwars.list, cwarsClone.list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithRecordableClass()
        {
            var cwrc = new CloneWithRecordableClass();
            var cwrcArray = new[] { cwrc };
            var cwrcArrayClone = Dec.Recorder.Clone(cwrcArray);
            Assert.AreNotSame(cwrcArray, cwrcArrayClone);
            Assert.AreNotSame(cwrc, cwrcArrayClone[0]);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithRecordableStruct()
        {
            var cwrs = new CloneWithRecordableStruct();
            cwrs.list = new List<int>();
            var cwrsArray = new[] { cwrs };
            var cwrsArrayClone = Dec.Recorder.Clone(cwrsArray);
            Assert.AreNotSame(cwrsArray, cwrsArrayClone);
            Assert.AreNotSame(cwrs, cwrsArrayClone[0]);
            Assert.AreNotSame(cwrs.list, cwrsArrayClone[0].list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentClass()
        {
            var cwac = new CloneWithAssignmentClass();
            var cwacArray = new[] { cwac };
            var cwacArrayClone = Dec.Recorder.Clone(cwacArray);
            Assert.AreNotSame(cwacArray, cwacArrayClone);
            Assert.AreSame(cwac, cwacArrayClone[0]);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentStruct()
        {
            var cwas = new CloneWithAssignmentStruct();
            cwas.list = new List<int>();
            var cwasArray = new[] { cwas };
            var cwasArrayClone = Dec.Recorder.Clone(cwasArray);
            Assert.AreNotSame(cwasArray, cwasArrayClone);
            Assert.AreNotSame(cwas, cwasArrayClone[0]);
            Assert.AreSame(cwas.list, cwasArrayClone[0].list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentResolutionClass()
        {
            var cwarc = new CloneWithAssignmentResolutionClass();
            var cwarcArray = new[] { cwarc };
            var cwarcArrayClone = Dec.Recorder.Clone(cwarcArray);
            Assert.AreNotSame(cwarcArray, cwarcArrayClone);
            Assert.AreSame(cwarc, cwarcArrayClone[0]);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentResolutionStruct()
        {
            var cwars = new CloneWithAssignmentResolutionStruct();
            cwars.list = new List<int>();
            var cwarsArray = new[] { cwars };
            var cwarsArrayClone = Dec.Recorder.Clone(cwarsArray);
            Assert.AreNotSame(cwarsArray, cwarsArrayClone);
            Assert.AreNotSame(cwars, cwarsArrayClone[0]);
            Assert.AreSame(cwars.list, cwarsArrayClone[0].list);
        }

        [Test]
        public void WithAssignmentAttribute_Dict11()
        {
            Dictionary<CloneWithAssignmentClass, CloneWithAssignmentClass> dict = new Dictionary<CloneWithAssignmentClass, CloneWithAssignmentClass>();
            dict[new CloneWithAssignmentClass()] = new CloneWithAssignmentClass();

            var dictClone = Dec.Recorder.Clone(dict);
            Assert.AreSame(dict.First().Key, dictClone.First().Key);
            Assert.AreSame(dict.First().Value, dictClone.First().Value);
        }

        [Test]
        public void WithAssignmentAttribute_Dict10()
        {
            Dictionary<CloneWithAssignmentClass, CloneWithRecordableClass> dict = new Dictionary<CloneWithAssignmentClass, CloneWithRecordableClass>();
            dict[new CloneWithAssignmentClass()] = new CloneWithRecordableClass();

            var dictClone = Dec.Recorder.Clone(dict);
            Assert.AreSame(dict.First().Key, dictClone.First().Key);
            Assert.AreNotSame(dict.First().Value, dictClone.First().Value);
        }

        [Test]
        public void WithAssignmentAttribute_Dict01()
        {
            Dictionary<CloneWithRecordableClass, CloneWithAssignmentClass> dict = new Dictionary<CloneWithRecordableClass, CloneWithAssignmentClass>();
            dict[new CloneWithRecordableClass()] = new CloneWithAssignmentClass();

            var dictClone = Dec.Recorder.Clone(dict);
            Assert.AreNotSame(dict.First().Key, dictClone.First().Key);
            Assert.AreSame(dict.First().Value, dictClone.First().Value);
        }

        class ListToArrayRecordable : IRecordable
        {
            public List<int> list;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Read)
                {
                    recorder.Record(ref list, "list");
                }
                else
                {
                    var array = list.ToArray();
                    recorder.Record(ref array, "list");
                    list = new List<int>(array);
                }
            }
        }

        [Test]
        public void ListToArray()
        {
            var listToArray = new ListToArrayRecordable();
            listToArray.list = new List<int> { 1, 2, 3 };

            ListToArrayRecordable listToArrayClone = null;
            ExpectErrors(() => listToArrayClone = Dec.Recorder.Clone(listToArray), errorValidator: err => err.Contains("Attempting to clone type"));

            Assert.IsNull(listToArrayClone.list);
        }

        public class ReadOnlyLegacyRecordable : IRecordable
        {
            public int legacy;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Read)
                {
                    recorder.Record(ref legacy, "legacy");
                }
            }
        }

        [Test]
        public void ReadWithNothingWritten()
        {
            var clone = Dec.Recorder.Clone(new ReadOnlyLegacyRecordable { legacy = 3 });

            Assert.IsNotNull(clone);
            Assert.AreEqual(0, clone.legacy);
        }

        public class NonConstructableRecordable : IRecordable
        {
            public NonConstructableRecordable(int value) { }

            public void Record(Dec.Recorder recorder) { }
        }

        public class DeepLink : IRecordable
        {
            public DeepLink next;
            public NonConstructableRecordable tail;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref next, "next");
                recorder.Record(ref tail, "tail");
            }
        }

        [Test]
        public void DeepNonConstructable()
        {
            var root = new DeepLink();
            var link = root;
            for (int i = 0; i < 25; ++i)
            {
                link.next = new DeepLink();
                link = link.next;
            }
            link.tail = new NonConstructableRecordable(1);

            DeepLink clone = null;
            ExpectWarningsAndErrors(() => clone = Dec.Recorder.Clone(root), warningValidator: wrn => wrn.Contains("cannot be constructed"), errorValidator: err => err.Contains("without a no-argument constructor"));

            for (int i = 0; i < 25; ++i)
            {
                Assert.IsNull(clone.tail);
                clone = clone.next;
            }
            Assert.IsNull(clone.tail);
        }

        private static long CloneAllocatedBytes<T>(T value)
        {
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            Dec.Recorder.Clone(value);
            return System.GC.GetAllocatedBytesForCurrentThread() - before;
        }

        public class CountedStrings : IRecordable
        {
            private static readonly string[] Labels = Enumerable.Range(0, 10).Select(i => $"field{i}").ToArray();

            public int count;
            public string[] values = new string[10];

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref count, nameof(count));
                for (int i = 0; i < count; ++i)
                {
                    recorder.Record(ref values[i], Labels[i]);
                }
            }
        }

        [Test]
        public void AllocationPerField()
        {
            // Every clone owns the same fixed-size array whatever it records, so both sides allocate the same output and the difference is what the extra fields cost.
            List<CountedStrings> Make(int count)
            {
                return Enumerable.Range(0, 1000).Select(i => new CountedStrings { count = count, values = Enumerable.Range(0, 10).Select(j => $"value{j}").ToArray() }).ToList();
            }
            var few = Make(5);
            var many = Make(10);

            // Warm up caches and the JIT so that only the traversal itself is measured.
            CloneAllocatedBytes(few);
            CloneAllocatedBytes(many);

            long extra = CloneAllocatedBytes(many) - CloneAllocatedBytes(few);
            Assert.Less(extra, 5000, "Clone should not allocate per recorded field");
        }

        [Test]
        public void AllocationPerElement()
        {
            // Both lists clone into a list of the same size; a string element goes through a clone node and a null doesn't.
            var strings = Enumerable.Range(0, 2000).Select(i => (object)$"element{i}").ToList();
            var nulls = Enumerable.Range(0, 2000).Select(i => (object)null).ToList();

            // Warm up caches and the JIT so that only the traversal itself is measured.
            CloneAllocatedBytes(strings);
            CloneAllocatedBytes(nulls);

            long extra = CloneAllocatedBytes(strings) - CloneAllocatedBytes(nulls);
            Assert.Less(extra, 2000, "Clone should not allocate per list element");
        }

        public struct StringRecordable : IRecordable
        {
            public string value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        [Test]
        public void AllocationPerRecord()
        {
            // Each extra struct element costs two boxes, a reference-table entry, and list growth, around 120 bytes in all; a recorder, reader, and read view of its own per Record() call would add several hundred more.
            List<StringRecordable> Make(int count)
            {
                return Enumerable.Range(0, count).Select(i => new StringRecordable { value = $"value{i}" }).ToList();
            }
            var small = Make(1000);
            var large = Make(2000);

            // Warm up caches and the JIT so that only the traversal itself is measured.
            CloneAllocatedBytes(small);
            CloneAllocatedBytes(large);

            long extra = CloneAllocatedBytes(large) - CloneAllocatedBytes(small);
            Assert.Less(extra, 1000 * 250, "Clone should not allocate recorders per Record() call");
        }

        public class ClonePathProbe : IRecordable
        {
            public static List<string> Seen = new List<string>();

            public ClonePathProbe child;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Write)
                {
                    Seen.Add(recorder.Context.PathString());
                }

                recorder.Record(ref child, nameof(child));
            }
        }

        public class ClonePathHolder : IRecordable
        {
            public ClonePathProbe a1;
            public ClonePathProbe a2;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref a1, nameof(a1));
                recorder.Record(ref a2, nameof(a2));
            }
        }

        [Test]
        public void ContextPaths()
        {
            var holder = new ClonePathHolder
            {
                a1 = new ClonePathProbe { child = new ClonePathProbe() },
                a2 = new ClonePathProbe { child = new ClonePathProbe() },
            };

            ClonePathProbe.Seen.Clear();
            Dec.Recorder.Clone(holder);

            CollectionAssert.AreEqual(new List<string> { "RECORD.a1", "RECORD.a1.child", "RECORD.a2", "RECORD.a2.child" }, ClonePathProbe.Seen);
        }

        public class CloneChainProbe : IRecordable
        {
            public static List<string> Seen = new List<string>();

            public CloneChainProbe next;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Write)
                {
                    Seen.Add(recorder.Context.PathString());
                }

                recorder.Shared().Record(ref next, nameof(next));
            }
        }

        [Test]
        public void ContextPathsDeferred()
        {
            // Deep enough that the tail of the chain is resolved later through pending writes.
            var root = new CloneChainProbe();
            var link = root;
            var expected = new List<string> { "RECORD" };
            for (int i = 0; i < 30; ++i)
            {
                link.next = new CloneChainProbe();
                link = link.next;
                expected.Add(expected[expected.Count - 1] + ".next");
            }

            CloneChainProbe.Seen.Clear();
            Dec.Recorder.Clone(root);

            CollectionAssert.AreEquivalent(expected, CloneChainProbe.Seen);
        }

        public class DeferredPathProbe : IRecordable
        {
            public static List<string> Seen = new List<string>();

            public int[] payload = new int[60];

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Write)
                {
                    Seen.Add(recorder.Context.PathString());
                }

                for (int i = 0; i < payload.Length; ++i)
                {
                    recorder.Record(ref payload[i], "f" + i);
                }
            }
        }

        public class DeferredPathElement : IRecordable
        {
            public DeferredPathProbe probe;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref probe, nameof(probe));
            }
        }

        public class DeferredPathChain : IRecordable
        {
            public DeferredPathChain next;
            public List<DeferredPathElement> items;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref next, nameof(next));
                recorder.Record(ref items, nameof(items));
            }
        }

        [Test]
        public void ContextPathsDeferredAfterParentReuse()
        {
            // Chain link k sits at depth k, its list at k+1, the list's elements at k+2, and their probes at k+3; 18 links put the probes just past the deferral depth. Both probes defer, their list elements are recycled, and the first probe's sixty fields dig those recycled nodes back out before the second probe runs.
            var root = new DeferredPathChain();
            var link = root;
            for (int i = 0; i < 18; ++i)
            {
                link.next = new DeferredPathChain();
                link = link.next;
            }
            link.items = new List<DeferredPathElement> { new DeferredPathElement { probe = new DeferredPathProbe() }, new DeferredPathElement { probe = new DeferredPathProbe() } };

            DeferredPathProbe.Seen.Clear();
            Dec.Recorder.Clone(root);

            string expected = "RECORD" + string.Concat(Enumerable.Repeat(".next", 18)) + ".items.probe";
            CollectionAssert.AreEqual(new List<string> { expected, expected }, DeferredPathProbe.Seen);
        }

        public class DeepTupleLink : IRecordable
        {
            public DeepTupleLink next;
            public System.Tuple<int, int> pair;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref next, nameof(next));
                recorder.Shared().Record(ref pair, nameof(pair));
            }
        }

        [Test]
        public void DeepTuple()
        {
            var root = new DeepTupleLink();
            var link = root;
            for (int i = 0; i < 25; ++i)
            {
                link.next = new DeepTupleLink();
                link = link.next;
            }
            link.pair = System.Tuple.Create(3, 4);

            var clone = Dec.Recorder.Clone(root);
            for (int i = 0; i < 25; ++i)
            {
                clone = clone.next;
            }

            Assert.AreEqual(3, clone.pair.Item1);
            Assert.AreEqual(4, clone.pair.Item2);
        }

        public class FactoryLink
        {
            public int value;
            public FactoryLink next;
        }

        public class FactoryLinkConverter : Dec.ConverterFactory<FactoryLink>
        {
            public override void Write(FactoryLink input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
                recorder.Shared().Record(ref input.next, "next");
            }

            public override FactoryLink Create(Dec.Recorder recorder)
            {
                var result = new FactoryLink();
                recorder.Record(ref result.value, "value");
                return result;
            }

            public override void Read(ref FactoryLink input, Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref input.next, "next");
            }
        }

        [Test]
        public void DeferredConverterFactoryChain()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new System.Type[] { typeof(FactoryLinkConverter) } });

            // Deep enough that the tail's factory bodies straddle a pending write: written and created in one pass, read in a later one.
            var root = new FactoryLink { value = 0 };
            var link = root;
            for (int i = 1; i < 30; ++i)
            {
                link.next = new FactoryLink { value = i };
                link = link.next;
            }

            var clone = Dec.Recorder.Clone(root);

            for (int i = 0; i < 30; ++i)
            {
                Assert.AreEqual(i, clone.value);
                clone = clone.next;
            }
            Assert.IsNull(clone);
        }

        public class AsThisRecordable : IRecordable
        {
            public int data;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref data);
            }
        }

        public class AsThisHolder : IRecordable
        {
            public AsThisRecordable first;
            public AsThisRecordable second;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref first, nameof(first));
                recorder.Record(ref second, nameof(second));
            }
        }

        [Test]
        public void ReusedNodesForgetAsThis()
        {
            // The first holder's fields leave their nodes flagged by RecordAsThis; the second holder's null field lands on one of those nodes again.
            var clone = Dec.Recorder.Clone(new List<AsThisHolder>
            {
                new AsThisHolder { first = new AsThisRecordable { data = 1 }, second = new AsThisRecordable { data = 2 } },
                new AsThisHolder { first = new AsThisRecordable { data = 3 }, second = null },
            });

            Assert.AreEqual(1, clone[0].first.data);
            Assert.AreEqual(2, clone[0].second.data);
            Assert.AreEqual(3, clone[1].first.data);
            Assert.IsNull(clone[1].second);
        }

        public class ThrowingInner
        {
            public int value;
        }

        public class ThrowingInnerConverter : Dec.ConverterFactory<ThrowingInner>
        {
            public static bool ThrowNext;

            public override void Write(ThrowingInner input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
            }

            public override ThrowingInner Create(Dec.Recorder recorder)
            {
                var result = new ThrowingInner();
                recorder.Record(ref result.value, "value");

                if (ThrowNext)
                {
                    ThrowNext = false;
                    throw new System.InvalidOperationException("Create failed on purpose");
                }

                return result;
            }

            public override void Read(ref ThrowingInner input, Dec.Recorder recorder) { }
        }

        public class ThrowingHolder : IRecordable
        {
            public ThrowingInner inner;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref inner, nameof(inner));
            }
        }

        [Test]
        public void ExceptionMidResolve()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new System.Type[] { typeof(ThrowingInnerConverter) } });

            var input = new List<ThrowingHolder> { new ThrowingHolder { inner = new ThrowingInner { value = 1 } }, new ThrowingHolder { inner = new ThrowingInner { value = 2 } } };

            ThrowingInnerConverter.ThrowNext = true;
            List<ThrowingHolder> clone = null;
            ExpectErrors(() => clone = Dec.Recorder.Clone(input), err => err.Contains("Create failed on purpose"));

            Assert.AreEqual(2, clone[1].inner.value);
        }
    }
}
