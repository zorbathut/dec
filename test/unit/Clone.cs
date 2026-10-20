using System;
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

        // How Clone treats one side of a dictionary: copied as-is, or cloned as a value type or as a reference type.
        public enum DictionarySide
        {
            Copied,
            ClonedValueType,
            ClonedClass,
        }

        [Dec.CloneStructPiecewise]
        public struct DictionaryKeyCopied : IEquatable<DictionaryKeyCopied>
        {
            public int id;

            public bool Equals(DictionaryKeyCopied other)
            {
                return id == other.id;
            }

            public override bool Equals(object obj)
            {
                return obj is DictionaryKeyCopied other && Equals(other);
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        // Enums aren't primitive, so an enum key is cloned rather than copied.
        public enum DictionaryKeyEnum
        {
        }

        public class DictionaryKeyCloned : IRecordable
        {
            public int id;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref id, nameof(id));
            }
        }

        [Dec.CloneClassAsSharedRef]
        public class DictionaryValueCopied
        {
            public int id;
        }

        public struct DictionaryValueClonedStruct : IRecordable
        {
            public int id;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref id, nameof(id));
            }
        }

        public class DictionaryValueCloned : IRecordable
        {
            public int id;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref id, nameof(id));
            }
        }

        [Test]
        public void CloneDictionary([Values] DictionarySide key, [Values] DictionarySide value, [Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            switch (key)
            {
                case DictionarySide.Copied:
                    CloneDictionaryWithKey(i => new DictionaryKeyCopied { id = i }, k => k.id, key, value);
                    break;
                case DictionarySide.ClonedValueType:
                    CloneDictionaryWithKey(i => (DictionaryKeyEnum)i, k => (int)k, key, value);
                    break;
                case DictionarySide.ClonedClass:
                    CloneDictionaryWithKey(i => new DictionaryKeyCloned { id = i }, k => k.id, key, value);
                    break;
            }
        }

        private static void CloneDictionaryWithKey<K>(Func<int, K> makeKey, Func<K, int> keyId, DictionarySide key, DictionarySide value)
        {
            switch (value)
            {
                case DictionarySide.Copied:
                    AssertDictionaryClone(makeKey, keyId, key, i => new DictionaryValueCopied { id = i }, v => v.id, value);
                    break;
                case DictionarySide.ClonedValueType:
                    AssertDictionaryClone(makeKey, keyId, key, i => new DictionaryValueClonedStruct { id = i }, v => v.id, value);
                    break;
                case DictionarySide.ClonedClass:
                    AssertDictionaryClone(makeKey, keyId, key, i => new DictionaryValueCloned { id = i }, v => v.id, value);
                    break;
            }
        }

        private static void AssertDictionaryClone<K, V>(Func<int, K> makeKey, Func<K, int> keyId, DictionarySide key, Func<int, V> makeValue, Func<V, int> valueId, DictionarySide value)
        {
            var original = new Dictionary<K, V>();
            for (int i = 0; i < 20; ++i)
            {
                original[makeKey(i)] = makeValue(i);
            }

            // One value in two entries.
            var aliased = original.First(entry => keyId(entry.Key) == 0).Value;
            original[makeKey(20)] = aliased;

            var clone = Dec.Recorder.Clone(original);

            // Enumeration order isn't part of the contract, so entries are matched by key.
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.Count, clone.Count);
            var cloneEntries = clone.ToDictionary(entry => keyId(entry.Key));
            foreach (var originalEntry in original)
            {
                var cloneEntry = cloneEntries[keyId(originalEntry.Key)];
                Assert.AreEqual(valueId(originalEntry.Value), valueId(cloneEntry.Value));

                if (key == DictionarySide.ClonedClass)
                {
                    Assert.AreNotSame(originalEntry.Key, cloneEntry.Key);
                }

                if (value == DictionarySide.Copied)
                {
                    Assert.AreSame(originalEntry.Value, cloneEntry.Value);
                }
                else if (value == DictionarySide.ClonedClass)
                {
                    Assert.AreNotSame(originalEntry.Value, cloneEntry.Value);
                }
            }

            // A value reached from two entries clones once, the way any shared reference does.
            if (value == DictionarySide.ClonedClass)
            {
                Assert.AreSame(cloneEntries[0].Value, cloneEntries[20].Value);
            }
        }

        [Test]
        public void CloneDictionaryPresized([Values] bool cloneValues)
        {
            int capacity;
            if (cloneValues)
            {
                var original = Enumerable.Range(0, 100).ToDictionary(i => i, i => new DictionaryValueCloned { id = i });
                var clone = Dec.Recorder.Clone(original);
                CollectionAssert.AreEquivalent(original.Select(entry => (entry.Key, entry.Value.id)), clone.Select(entry => (entry.Key, entry.Value.id)));
                capacity = clone.EnsureCapacity(0);
            }
            else
            {
                var original = Enumerable.Range(0, 100).ToDictionary(i => i, i => i * 2);
                var clone = Dec.Recorder.Clone(original);
                CollectionAssert.AreEquivalent(original, clone);
                capacity = clone.EnsureCapacity(0);
            }

            // EnsureCapacity(0) reports the capacity; one grown entry by entry overshoots a presized one.
            Assert.AreEqual(new Dictionary<int, int>(100).EnsureCapacity(0), capacity);
        }

        public class DictionaryComparerHolder : IRecordable
        {
            public Dictionary<string, int> dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref dict, nameof(dict));
            }
        }

        [Test]
        public void CloneDictionaryModelKeepsComparer([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var holder = new DictionaryComparerHolder();
            holder.dict["Key"] = 1;

            // The clone's own constructor builds the dictionary it's filled into, and that one carries the comparer.
            var clone = Dec.Recorder.Clone(holder);

            Assert.AreNotSame(holder.dict, clone.dict);
            Assert.AreEqual(1, clone.dict.Count);
            Assert.AreEqual(1, clone.dict["KEY"]);
        }

        public class DictionaryCycleValue : IRecordable
        {
            public Dictionary<int, DictionaryCycleValue> owner;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref owner, nameof(owner));
            }
        }

        [Test]
        public void CloneDictionaryCycle([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var original = new Dictionary<int, DictionaryCycleValue>();
            original[1] = new DictionaryCycleValue { owner = original };

            var clone = Dec.Recorder.Clone(original);

            Assert.AreNotSame(original, clone);
            Assert.AreNotSame(original[1], clone[1]);
            Assert.AreSame(clone, clone[1].owner);
        }

        public class DictionaryChainLink : IRecordable
        {
            public int id;
            public Dictionary<int, DictionaryChainLink> next;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref id, nameof(id));
                recorder.Shared().Record(ref next, nameof(next));
            }
        }

        [Test]
        public void CloneDictionaryDeferred([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            // Deep enough that Clone defers the shared dictionaries past the depth limit, which resolves them from a fresh stack.
            var head = new DictionaryChainLink { id = 0 };
            var link = head;
            for (int i = 1; i < 50; ++i)
            {
                var following = new DictionaryChainLink { id = i };
                link.next = new Dictionary<int, DictionaryChainLink> { [i] = following };
                link = following;
            }

            var clone = Dec.Recorder.Clone(head);

            var originalLink = head;
            var cloneLink = clone;
            for (int i = 0; i < 50; ++i)
            {
                Assert.AreNotSame(originalLink, cloneLink);
                Assert.AreEqual(i, cloneLink.id);
                if (i == 49)
                {
                    Assert.IsNull(cloneLink.next);
                    break;
                }

                Assert.AreNotSame(originalLink.next, cloneLink.next);
                CollectionAssert.AreEquivalent(originalLink.next.Keys, cloneLink.next.Keys);
                originalLink = originalLink.next[i + 1];
                cloneLink = cloneLink.next[i + 1];
            }
        }

        [Test]
        public void CloneDictionarySorted()
        {
            var original = new SortedDictionary<int, DictionaryValueCloned>();
            foreach (var i in new[] { 3, 1, 2 })
            {
                original[i] = new DictionaryValueCloned { id = i * 10 };
            }

            var clone = Dec.Recorder.Clone(original);

            Assert.AreNotSame(original, clone);
            CollectionAssert.AreEqual(original.Keys, clone.Keys);
            CollectionAssert.AreEqual(original.Values.Select(v => v.id), clone.Values.Select(v => v.id));
            Assert.IsFalse(original.Values.Intersect(clone.Values).Any());
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

        public class CountedValues<T> : IRecordable
        {
            private static readonly string[] Labels = Enumerable.Range(0, 10).Select(i => $"field{i}").ToArray();

            public int count;
            public T[] values = new T[10];

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref count, nameof(count));
                for (int i = 0; i < count; ++i)
                {
                    recorder.Record(ref values[i], Labels[i]);
                }
            }
        }

        private static void AssertNoAllocationPerValueField<T>(Func<int, T> make)
        {
            // As AllocationPerField, over value types: every clone owns the same fixed-size array, so the difference is what the extra fields cost.
            List<CountedValues<T>> Make(int count)
            {
                return Enumerable.Range(0, 1000).Select(i => new CountedValues<T> { count = count, values = Enumerable.Range(0, 10).Select(make).ToArray() }).ToList();
            }
            var few = Make(5);
            var many = Make(10);

            CloneAllocatedBytes(few);
            CloneAllocatedBytes(many);

            long extra = CloneAllocatedBytes(many) - CloneAllocatedBytes(few);
            Assert.Less(extra, 5000, "Clone should not allocate per recorded value-type field");
        }

        [Test]
        public void AllocationPerValueField([Values(ValueFieldKind.Int, ValueFieldKind.Long, ValueFieldKind.Enum, ValueFieldKind.PiecewiseStruct, ValueFieldKind.ValuelikeStruct)] ValueFieldKind kind)
        {
            switch (kind)
            {
                case ValueFieldKind.Int:
                    AssertNoAllocationPerValueField(i => i);
                    break;
                case ValueFieldKind.Long:
                    AssertNoAllocationPerValueField(i => (long)i << 33);
                    break;
                case ValueFieldKind.Enum:
                    AssertNoAllocationPerValueField(i => (ValueFieldEnum)(i % 3));
                    break;
                case ValueFieldKind.PiecewiseStruct:
                    AssertNoAllocationPerValueField(i => new ValueFieldPiecewise { x = i });
                    break;
                case ValueFieldKind.ValuelikeStruct:
                    UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ValueFieldValuelikeConverter) } });
                    AssertNoAllocationPerValueField(i => new ValueFieldValuelike { x = i });
                    break;
                default:
                    Assert.Fail($"No allocation case for {kind}");
                    break;
            }
        }

        // Records `count` fields cycling through four value types, so consecutive fields never share one.
        public class CountedMixedValues : IRecordable
        {
            private static readonly string[] Labels = Enumerable.Range(0, 12).Select(i => $"field{i}").ToArray();

            public int count;
            public int[] ints = new int[3];
            public long[] longs = new long[3];
            public ValueFieldEnum[] enums = new ValueFieldEnum[3];
            public ValueFieldPiecewise[] structs = new ValueFieldPiecewise[3];

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref count, nameof(count));
                for (int i = 0; i < count; ++i)
                {
                    switch (i % 4)
                    {
                        case 0:
                            recorder.Record(ref ints[i / 4], Labels[i]);
                            break;
                        case 1:
                            recorder.Record(ref longs[i / 4], Labels[i]);
                            break;
                        case 2:
                            recorder.Record(ref enums[i / 4], Labels[i]);
                            break;
                        default:
                            recorder.Record(ref structs[i / 4], Labels[i]);
                            break;
                    }
                }
            }
        }

        [Test]
        public void AllocationPerMixedValueField()
        {
            // Guards the order children go back to the node pool: each object's fields come back to the next object of the same type in the same order, so a field of one type never lands on a node holding another's.
            List<CountedMixedValues> Make(int count)
            {
                return Enumerable.Range(0, 1000).Select(i => new CountedMixedValues { count = count, ints = new[] { i, i + 1, i + 2 }, longs = new[] { (long)i, i + 1L, i + 2L }, enums = new[] { ValueFieldEnum.First, ValueFieldEnum.Second, ValueFieldEnum.Third }, structs = new[] { new ValueFieldPiecewise { x = i }, new ValueFieldPiecewise { x = i + 1 }, new ValueFieldPiecewise { x = i + 2 } } }).ToList();
            }
            var few = Make(4);
            var many = Make(12);

            CloneAllocatedBytes(few);
            CloneAllocatedBytes(many);

            long extra = CloneAllocatedBytes(many) - CloneAllocatedBytes(few);
            Assert.Less(extra, 5000, "Clone should not allocate per recorded value-type field");
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
            // Each extra struct element costs two boxes and list growth, around 64 bytes in all. An entry in the reference table would add 50 to 150 more, depending on where the table's growth lands, and a recorder, reader, and read view of its own per Record() call would add several hundred.
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
            Assert.Less(extra, 1000 * 90, "Clone should allocate nothing per struct element beyond its boxes and list growth");
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

        // Every kind of value-type field Clone copies as-is, and the paths around it that must keep today's behavior.
        public enum ValueFieldKind
        {
            Int,
            Long,
            Double,
            Bool,
            Enum,
            PiecewiseStruct,
            ValuelikeStruct,
        }

        public enum ValueFieldEnum
        {
            First,
            Second,
            Third,
        }

        [Dec.CloneStructPiecewise]
        public struct ValueFieldPiecewise
        {
            public int x;
            public List<int> list;
        }

        public struct ValueFieldValuelike
        {
            public int x;
        }

        public class ValueFieldValuelikeConverter : Dec.ConverterString<ValueFieldValuelike>
        {
            public override string Write(ValueFieldValuelike input)
            {
                return input.x.ToString();
            }

            public override ValueFieldValuelike Read(string input, Dec.Context context)
            {
                return new ValueFieldValuelike { x = int.Parse(input) };
            }

            public override bool TreatAsValuelike()
            {
                return true;
            }
        }

        public class ValueFieldHolder<T> : IRecordable
        {
            public T value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        private static ValueFieldHolder<T> CloneValueFieldOf<T>(T value)
        {
            var clone = Dec.Recorder.Clone(new ValueFieldHolder<T> { value = value });
            Assert.AreEqual(value, clone.value);
            return clone;
        }

        [Test]
        public void CloneValueField([Values] ValueFieldKind kind)
        {
            switch (kind)
            {
                case ValueFieldKind.Int:
                    CloneValueFieldOf(42);
                    break;
                case ValueFieldKind.Long:
                    CloneValueFieldOf(1L << 40);
                    break;
                case ValueFieldKind.Double:
                    CloneValueFieldOf(2.5);
                    break;
                case ValueFieldKind.Bool:
                    CloneValueFieldOf(true);
                    break;
                case ValueFieldKind.Enum:
                    CloneValueFieldOf(ValueFieldEnum.Third);
                    break;
                case ValueFieldKind.PiecewiseStruct:
                {
                    // A piecewise struct is copied shallowly, references and all.
                    var original = new ValueFieldPiecewise { x = 7, list = new List<int> { 1, 2 } };
                    var clone = CloneValueFieldOf(original);
                    Assert.AreSame(original.list, clone.value.list);
                    break;
                }
                case ValueFieldKind.ValuelikeStruct:
                    UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ValueFieldValuelikeConverter) } });
                    CloneValueFieldOf(new ValueFieldValuelike { x = 9 });
                    break;
                default:
                    Assert.Fail($"No clone case for {kind}");
                    break;
            }
        }

        // Records one label as an int on one pass and as an object on the other.
        public class ValueFieldAsObject : IRecordable
        {
            public static bool ObjectOnWrite;

            public int value;
            public object readBack;

            public void Record(Dec.Recorder recorder)
            {
                if ((recorder.Mode == Dec.Recorder.Direction.Write) == ObjectOnWrite)
                {
                    object boxed = value;
                    recorder.Record(ref boxed, nameof(value));
                    readBack = boxed;
                }
                else
                {
                    recorder.Record(ref value, nameof(value));
                }
            }
        }

        [Test]
        public void CloneValueFieldReadAsObject()
        {
            ValueFieldAsObject.ObjectOnWrite = false;

            var clone = Dec.Recorder.Clone(new ValueFieldAsObject { value = 5 });

            Assert.AreEqual(5, clone.readBack);
        }

        [Test]
        public void CloneValueFieldWrittenAsObject()
        {
            ValueFieldAsObject.ObjectOnWrite = true;

            // The int field cloned first leaves its value behind on a node the object field's clone can reuse; it mustn't be read back.
            var clone = Dec.Recorder.Clone(new List<object> { new ValueFieldHolder<int> { value = 1 }, new ValueFieldAsObject { value = 5 } });

            Assert.AreEqual(1, ((ValueFieldHolder<int>)clone[0]).value);
            Assert.AreEqual(5, ((ValueFieldAsObject)clone[1]).value);
        }

        [Dec.CloneStructPiecewise]
        public struct ValueFieldOtherPiecewise
        {
            public int y;
        }

        // Writes a label as one struct and reads it as another, then records a field after it.
        public class ValueFieldAsOtherType : IRecordable
        {
            public ValueFieldPiecewise written;
            public ValueFieldOtherPiecewise read;
            public int after;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Write)
                {
                    recorder.Record(ref written, "point");
                }
                else
                {
                    recorder.Record(ref read, "point");
                }

                recorder.Record(ref after, nameof(after));
            }
        }

        [Test]
        public void CloneValueFieldReadAsOtherType()
        {
            ValueFieldAsOtherType clone = null;
            ExpectErrors(() => clone = Dec.Recorder.Clone(new ValueFieldAsOtherType { written = new ValueFieldPiecewise { x = 3 }, read = new ValueFieldOtherPiecewise { y = 4 }, after = 6 }), err => true);

            Assert.AreEqual(0, clone.read.y);
            Assert.AreEqual(6, clone.after);
        }

        public class ValueFieldShared : IRecordable
        {
            public int value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref value, nameof(value));
            }
        }

        [Test]
        public void CloneValueFieldShared()
        {
            // As in CloneValueFieldWrittenAsObject, the first object's int field leaves a value behind on a node the shared one can reuse.
            List<object> clone = null;
            ExpectWarnings(() => clone = Dec.Recorder.Clone(new List<object> { new ValueFieldHolder<int> { value = 1 }, new ValueFieldShared { value = 8 } }), wrn => true);

            Assert.AreEqual(1, ((ValueFieldHolder<int>)clone[0]).value);
            Assert.AreEqual(8, ((ValueFieldShared)clone[1]).value);
        }

        [Dec.CloneStructPiecewise]
        public struct ValueFieldHookNew : IPostCloneNew
        {
            public int x;
            public bool hooked;

            public void PostCloneNew()
            {
                hooked = true;
            }
        }

        [Dec.CloneStructPiecewise]
        public struct ValueFieldHookOriginal : IPostCloneOriginal
        {
            public int x;
            public bool hooked;

            public void PostCloneOriginal()
            {
                hooked = true;
            }
        }

        [Test]
        public void CloneValueFieldPostClone([Values] bool hookNew)
        {
            // Both hooks run on the copy Clone hands back, so the clone's field shows either one.
            if (hookNew)
            {
                var clone = Dec.Recorder.Clone(new ValueFieldHolder<ValueFieldHookNew> { value = new ValueFieldHookNew { x = 1 } });
                Assert.AreEqual(1, clone.value.x);
                Assert.IsTrue(clone.value.hooked);
            }
            else
            {
                var clone = Dec.Recorder.Clone(new ValueFieldHolder<ValueFieldHookOriginal> { value = new ValueFieldHookOriginal { x = 1 } });
                Assert.AreEqual(1, clone.value.x);
                Assert.IsTrue(clone.value.hooked);
            }
        }

        public class ValueFieldDuplicateRead : IRecordable
        {
            public int value;
            public int again;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
                if (recorder.Mode == Dec.Recorder.Direction.Read)
                {
                    recorder.Record(ref again, nameof(value));
                }
            }
        }

        [Test]
        public void CloneValueFieldTypeChange()
        {
            // Each holder's field reuses the node the one before it released, which last carried a value of a different type.
            var clone = Dec.Recorder.Clone(new List<object> { new ValueFieldHolder<int> { value = 1 }, new ValueFieldHolder<long> { value = 1L << 40 }, new ValueFieldHolder<ValueFieldPiecewise> { value = new ValueFieldPiecewise { x = 3 } }, new ValueFieldHolder<int> { value = 4 } });

            Assert.AreEqual(1, ((ValueFieldHolder<int>)clone[0]).value);
            Assert.AreEqual(1L << 40, ((ValueFieldHolder<long>)clone[1]).value);
            Assert.AreEqual(3, ((ValueFieldHolder<ValueFieldPiecewise>)clone[2]).value.x);
            Assert.AreEqual(4, ((ValueFieldHolder<int>)clone[3]).value);
        }

        [Test]
        public void CloneValueFieldDuplicateRead()
        {
            ValueFieldDuplicateRead clone = null;
            ExpectWarnings(() => clone = Dec.Recorder.Clone(new ValueFieldDuplicateRead { value = 11 }), wrn => true);

            Assert.AreEqual(11, clone.value);
            Assert.AreEqual(11, clone.again);
        }

        public class ValueFieldFactoried
        {
            public int value;
            public int readAgain;
            public ValueFieldFactoried next;
        }

        // Create and Read both record the same value, so both read it from the same written field.
        public class ValueFieldFactoriedConverter : Dec.ConverterFactory<ValueFieldFactoried>
        {
            public override void Write(ValueFieldFactoried input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.value, "value");
                recorder.Shared().Record(ref input.next, "next");
            }

            public override ValueFieldFactoried Create(Dec.Recorder recorder)
            {
                int value = 0;
                recorder.Record(ref value, "value");
                return new ValueFieldFactoried { value = value };
            }

            public override void Read(ref ValueFieldFactoried input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.readAgain, "value");
                recorder.Shared().Record(ref input.next, "next");
            }
        }

        [Test]
        public void CloneValueFieldFactory([Values(1, 50)] int length)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ValueFieldFactoriedConverter) } });

            // A long chain is deep enough that Clone defers the later links' reads past their Creates.
            var head = new ValueFieldFactoried { value = 0 };
            var link = head;
            for (int i = 1; i < length; ++i)
            {
                link.next = new ValueFieldFactoried { value = i };
                link = link.next;
            }

            var clone = Dec.Recorder.Clone(new ValueFieldHolder<ValueFieldFactoried> { value = head }).value;

            for (int i = 0; i < length; ++i)
            {
                Assert.AreNotSame(head, clone);
                Assert.AreEqual(i, clone.value);
                Assert.AreEqual(i, clone.readAgain);
                head = head.next;
                clone = clone.next;
            }
            Assert.IsNull(clone);
        }

        // A clone of something else, run partway through a clone's read pass.
        public class ValueFieldNested : IRecordable
        {
            public static ValueFieldHolder<long> Other;

            public int first;
            public int second;
            public ValueFieldHolder<long> otherClone;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref first, nameof(first));
                if (recorder.Mode == Dec.Recorder.Direction.Read)
                {
                    otherClone = Dec.Recorder.Clone(Other);
                }
                recorder.Record(ref second, nameof(second));
            }
        }

        [Test]
        public void CloneValueFieldNested()
        {
            ValueFieldNested.Other = new ValueFieldHolder<long> { value = 77 };

            var clone = Dec.Recorder.Clone(new ValueFieldNested { first = 1, second = 2 });

            Assert.AreEqual(1, clone.first);
            Assert.AreEqual(2, clone.second);
            Assert.AreEqual(77, clone.otherClone.value);
        }

        [Test]
        public void ExceptionMidResolveValueFields()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new System.Type[] { typeof(ThrowingInnerConverter) } });

            // The throw abandons the first element's value field mid-read; every later element has to get its own value back.
            var input = Enumerable.Range(0, 20).Select(i => new ThrowingHolder { inner = new ThrowingInner { value = i * 10 } }).ToList();

            ThrowingInnerConverter.ThrowNext = true;
            List<ThrowingHolder> clone = null;
            ExpectErrors(() => clone = Dec.Recorder.Clone(input), err => err.Contains("Create failed on purpose"));

            for (int i = 1; i < 20; ++i)
            {
                Assert.AreEqual(i * 10, clone[i].inner.value);
            }
        }
    }
}
