using System;
using System.Collections.Generic;
using System.Linq;
using Dec;
using NUnit.Framework;

namespace DecTest
{
    // Clone of IRecordable structs that aren't copied as-is: each test runs with and without the path that records them in place, and both must agree.
    [TestFixture]
    public class CloneRecordedStruct : Base
    {
        public class Target : IRecordable
        {
            public int id;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref id, nameof(id));
            }
        }

        public struct Inner : IRecordable
        {
            public int number;
            public Target target;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref number, nameof(number));
                recorder.Shared().Record(ref target, nameof(target));
            }
        }

        public struct Outer : IRecordable
        {
            public int number;
            public string text;
            public Target target;
            public Inner inner;

            // Never recorded, so a clone keeps whatever its destination held.
            public int unrecorded;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref number, nameof(number));
                recorder.Record(ref text, nameof(text));
                recorder.Shared().Record(ref target, nameof(target));
                recorder.Record(ref inner, nameof(inner));
            }
        }

        public class Holder : IRecordable
        {
            public Outer a = new Outer { unrecorded = 99 };
            public Outer b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref a, nameof(a));
                recorder.Record(ref b, nameof(b));
            }
        }

        private class Outcome
        {
            public object result;
            public string written;
            public string exception;
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
        }

        // Errors, warnings, any exception, and the clone as Recorder.Write sees it; never through Checksum, which the fallback switch also changes.
        private Outcome Run<T>(T value, bool fallback)
        {
            var outcome = new Outcome();
            Dec.Config.TestForceFallbackEmit = fallback;
            try
            {
                ExpectGeneral(() =>
                {
                    try
                    {
                        outcome.result = Dec.Recorder.Clone(value);
                    }
                    catch (Exception e)
                    {
                        outcome.exception = e.GetType().Name;
                    }

                    if (outcome.result != null)
                    {
                        outcome.written = Dec.Recorder.Write((T)outcome.result, pretty: false);
                    }
                }, "clone", ExpectationType.Tolerate, wrn => { outcome.warnings.Add(wrn); return true; }, ExpectationType.Tolerate, err => { outcome.errors.Add(err); return true; });
            }
            finally
            {
                Dec.Config.TestForceFallbackEmit = false;
            }

            return outcome;
        }

        private (Outcome fast, Outcome general) RunBoth<T>(T value)
        {
            var fast = Run(value, fallback: false);
            var general = Run(value, fallback: true);

            Assert.AreEqual(general.exception, fast.exception);
            Assert.AreEqual(general.errors.Count, fast.errors.Count, string.Join("\n", fast.errors.Concat(general.errors)));
            Assert.AreEqual(general.warnings.Count, fast.warnings.Count, string.Join("\n", fast.warnings.Concat(general.warnings)));
            Assert.AreEqual(general.written, fast.written);

            return (fast, general);
        }

        private static Holder MakeHolder()
        {
            var shared = new Target { id = 1 };
            return new Holder
            {
                a = new Outer { number = 2, text = "a", target = shared, inner = new Inner { number = 3, target = shared }, unrecorded = 5 },
                b = new Outer { number = 4, text = "b", target = shared, inner = new Inner { number = 6, target = new Target { id = 7 } } },
            };
        }

        [Test]
        public void Fields()
        {
            var original = MakeHolder();
            var (fast, general) = RunBoth(original);

            foreach (var clone in new[] { (Holder)fast.result, (Holder)general.result })
            {
                Assert.AreEqual(2, clone.a.number);
                Assert.AreEqual("b", clone.b.text);
                Assert.AreEqual(6, clone.b.inner.number);
                Assert.AreEqual(7, clone.b.inner.target.id);

                // One clone of the shared target, through both fields and the nested struct.
                Assert.AreNotSame(original.a.target, clone.a.target);
                Assert.AreSame(clone.a.target, clone.b.target);
                Assert.AreSame(clone.a.target, clone.a.inner.target);

                // The destination's value, as Holder's initializer left it.
                Assert.AreEqual(99, clone.a.unrecorded);
                Assert.AreEqual(0, clone.b.unrecorded);
            }
        }

        public struct Mutating : IRecordable
        {
            public int number;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Write)
                {
                    ++number;
                }

                recorder.Record(ref number, nameof(number));
            }
        }

        public class MutatingHolder : IRecordable
        {
            public Mutating value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        [Test]
        public void WriteChangesOnlyItsCopy()
        {
            var original = new MutatingHolder { value = new Mutating { number = 10 } };
            var (fast, _) = RunBoth(original);

            Assert.AreEqual(10, original.value.number);
            Assert.AreEqual(11, ((MutatingHolder)fast.result).value.number);
        }

        public class Factoried
        {
            public Outer created;
            public Outer read;
        }

        // Create and Read both read the same written field.
        public class FactoriedConverter : Dec.ConverterFactory<Factoried>
        {
            public override void Write(Factoried input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.read, "value");
            }

            public override Factoried Create(Dec.Recorder recorder)
            {
                var result = new Factoried();
                recorder.Record(ref result.created, "value");
                return result;
            }

            public override void Read(ref Factoried input, Dec.Recorder recorder)
            {
                // A model the second read would start from, if it cloned again.
                input.read.unrecorded = 55;
                recorder.Record(ref input.read, "value");
            }
        }

        [Test]
        public void ReadInCreateAndRead()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(FactoriedConverter) } });

            var original = new Factoried { read = MakeHolder().a };
            var (fast, general) = RunBoth(original);

            foreach (var clone in new[] { (Factoried)fast.result, (Factoried)general.result })
            {
                Assert.AreEqual(2, clone.created.number);
                Assert.AreEqual(2, clone.read.number);

                // Both reads get the one clone, the first, rather than cloning again.
                Assert.AreEqual(0, clone.read.unrecorded);
                Assert.AreNotSame(original.read.target, clone.read.target);
                Assert.AreSame(clone.created.target, clone.read.target);
                Assert.AreSame(clone.created.inner.target, clone.read.inner.target);
            }
        }

        public class ReadTwice : IRecordable
        {
            public bool objectFirst;
            public Outer typed;
            public object boxed;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref objectFirst, nameof(objectFirst));
                if (recorder.Mode == Dec.Recorder.Direction.Write)
                {
                    recorder.Record(ref typed, nameof(typed));
                    return;
                }

                if (objectFirst)
                {
                    recorder.Record(ref boxed, nameof(typed));
                    recorder.Record(ref typed, nameof(typed));
                }
                else
                {
                    recorder.Record(ref typed, nameof(typed));
                    recorder.Record(ref boxed, nameof(typed));
                }
            }
        }

        [Test]
        public void ReadTwiceAsObject([Values] bool objectFirst)
        {
            var original = new ReadTwice { objectFirst = objectFirst, typed = MakeHolder().a };
            var fast = Run(original, fallback: false);
            var general = Run(original, fallback: true);

            // Write doesn't see the object read, so compare the clones directly.
            foreach (var outcome in new[] { fast, general })
            {
                Assert.IsNull(outcome.exception);
                Assert.AreEqual(0, outcome.errors.Count);
                Assert.AreEqual(1, outcome.warnings.Count);

                var clone = (ReadTwice)outcome.result;
                var boxed = (Outer)clone.boxed;
                Assert.AreEqual(2, clone.typed.number);
                Assert.AreEqual(2, boxed.number);
                Assert.AreNotSame(original.typed.target, clone.typed.target);
                Assert.AreSame(clone.typed.target, boxed.target);
            }
        }

        public struct Throwing : IRecordable
        {
            public int before;
            public int after;
            public Dec.Recorder.Direction throwOn;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref throwOn, nameof(throwOn));
                recorder.Record(ref before, nameof(before));
                if (recorder.Mode == throwOn)
                {
                    throw new InvalidOperationException("thrown by the test");
                }

                recorder.Record(ref after, nameof(after));
            }
        }

        public class ThrowingHolder : IRecordable
        {
            public Throwing value;
            public int next;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
                recorder.Record(ref next, nameof(next));
            }
        }

        [Test]
        public void Throws([Values(Dec.Recorder.Direction.Read, Dec.Recorder.Direction.Write)] Dec.Recorder.Direction throwOn)
        {
            var original = new ThrowingHolder { value = new Throwing { before = 1, after = 2, throwOn = throwOn }, next = 3 };
            var (fast, _) = RunBoth(original);

            Assert.IsTrue(fast.exception != null || fast.errors.Count > 0);
        }

        public class FactoryHolder : IRecordable
        {
            public Outer value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.WithFactory(new Dictionary<Type, Func<Type, object>> { { typeof(Outer), _ => new Outer { unrecorded = 7 } } }).Record(ref value, nameof(value));
            }
        }

        [Test]
        public void Factory()
        {
            var original = new FactoryHolder { value = MakeHolder().a };
            var (fast, _) = RunBoth(original);

            Assert.AreEqual(7, ((FactoryHolder)fast.result).value.unrecorded);
            Assert.AreEqual(2, ((FactoryHolder)fast.result).value.number);
        }

        public struct Conditional : IConditionalRecordable
        {
            public int number;

            public bool ShouldRecord(Dec.Recorder.IUserSettings userSettings)
            {
                return true;
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref number, nameof(number));
            }
        }

        public class ConditionalHolder : IRecordable
        {
            public Conditional value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        [Test]
        public void ConditionalStruct()
        {
            var (fast, _) = RunBoth(new ConditionalHolder { value = new Conditional { number = 4 } });

            Assert.AreEqual(4, ((ConditionalHolder)fast.result).value.number);
        }

        public class Chain : IRecordable
        {
            public Chain next;
            public Outer value;
            public List<Outer> list;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref next, nameof(next));
                recorder.Record(ref value, nameof(value));
                recorder.Record(ref list, nameof(list));
            }
        }

        [Test]
        public void PastDepthWarning([Values(40, 60)] int length)
        {
            // Unshared, so every link nests one deeper; past 50 the general path warns about it.
            var head = new Chain();
            var link = head;
            for (int i = 1; i < length; ++i)
            {
                link.value = new Outer { number = i, target = new Target { id = i } };
                link.list = new List<Outer> { link.value };
                link.next = new Chain();
                link = link.next;
            }

            var (fast, _) = RunBoth(head);

            Assert.AreEqual(length > 50, fast.warnings.Count > 0);
        }

        private static List<Outer> MakeList()
        {
            var holder = MakeHolder();
            return new List<Outer> { holder.a, holder.b, holder.a };
        }

        [Test]
        public void ListElements()
        {
            var original = MakeList();
            var (fast, general) = RunBoth(original);

            foreach (var clone in new[] { (List<Outer>)fast.result, (List<Outer>)general.result })
            {
                Assert.AreEqual(3, clone.Count);
                Assert.AreEqual(4, clone[1].number);
                Assert.AreNotSame(original[0].target, clone[0].target);
                Assert.AreSame(clone[0].target, clone[1].target);
                Assert.AreSame(clone[0].target, clone[2].inner.target);

                // Elements are created fresh, not from a model.
                Assert.AreEqual(0, clone[0].unrecorded);
            }
        }

        public class ListFactoryHolder : IRecordable
        {
            public List<Outer> values;

            public void Record(Dec.Recorder recorder)
            {
                recorder.WithFactory(new Dictionary<Type, Func<Type, object>> { { typeof(Outer), _ => new Outer { unrecorded = 7 } } }).Record(ref values, nameof(values));
            }
        }

        [Test]
        public void ListElementsFactory()
        {
            var (fast, _) = RunBoth(new ListFactoryHolder { values = MakeList() });

            Assert.IsTrue(((ListFactoryHolder)fast.result).values.All(value => value.unrecorded == 7));
        }

        [Test]
        public void DictionaryValues()
        {
            var list = MakeList();
            var original = new Dictionary<int, Outer> { { 1, list[0] }, { 2, list[1] } };
            var (fast, general) = RunBoth(original);

            foreach (var clone in new[] { (Dictionary<int, Outer>)fast.result, (Dictionary<int, Outer>)general.result })
            {
                Assert.AreEqual(4, clone[2].number);
                Assert.AreNotSame(original[1].target, clone[1].target);
                Assert.AreSame(clone[1].target, clone[2].target);
            }
        }

        public class SharedHolder : IRecordable
        {
            public Outer value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref value, nameof(value));
            }
        }

        [Test]
        public void SharedField()
        {
            var (fast, _) = RunBoth(new SharedHolder { value = MakeHolder().a });

            Assert.Greater(fast.warnings.Count, 0);
            Assert.AreEqual(2, ((SharedHolder)fast.result).value.number);
        }

        public struct Key : IRecordable, IEquatable<Key>
        {
            public int id;
            public Target target;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref id, nameof(id));
                recorder.Shared().Record(ref target, nameof(target));
            }

            public bool Equals(Key other)
            {
                return id == other.id;
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        [Test]
        public void DictionaryKeys()
        {
            var shared = new Target { id = 9 };
            var original = new Dictionary<Key, Outer> { { new Key { id = 1, target = shared }, MakeHolder().a }, { new Key { id = 2, target = shared }, MakeHolder().b } };
            var (fast, general) = RunBoth(original);

            foreach (var clone in new[] { (Dictionary<Key, Outer>)fast.result, (Dictionary<Key, Outer>)general.result })
            {
                var keys = clone.Keys.OrderBy(key => key.id).ToArray();
                Assert.AreEqual(2, keys.Length);
                Assert.AreNotSame(shared, keys[0].target);
                Assert.AreSame(keys[0].target, keys[1].target);
                Assert.AreEqual(4, clone[keys[1]].number);
            }
        }

        public class AsThisHolder : IRecordable
        {
            public Outer value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref value);
            }
        }

        [Test]
        public void AsThis()
        {
            var (fast, _) = RunBoth(new AsThisHolder { value = MakeHolder().a });

            Assert.AreEqual(2, ((AsThisHolder)fast.result).value.number);
        }

        // Elements of a struct with a parameterless constructor of its own take the general path, which runs it; that isn't tested, as netcoreapp3.1 compiles the tests as C# 8, which can't declare one.
    }
}
