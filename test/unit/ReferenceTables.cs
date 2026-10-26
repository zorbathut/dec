using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dec;
using NUnit.Framework;

namespace DecTest
{
    // Clone and Checksum keep their reference tables per thread between calls; nothing one call puts in them may reach another.
    [TestFixture]
    public class ReferenceTables : Base
    {
        public class Node : IRecordable
        {
            public int value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        public class Pair : IRecordable
        {
            public Node a;
            public Node b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, nameof(a));
                recorder.Shared().Record(ref b, nameof(b));
            }
        }

        public class SetHolder : IRecordable
        {
            public HashSet<Node> set;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref set, nameof(set));
            }
        }

        public class Thrower : IRecordable
        {
            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Mode == Dec.Recorder.Direction.Write)
                {
                    throw new InvalidOperationException("thrown by the test");
                }
            }
        }

        public class ThrowingPair : IRecordable
        {
            public Node a;
            public Thrower thrower = new Thrower();

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, nameof(a));
                recorder.Record(ref thrower, nameof(thrower));
            }
        }

        private static Dec.ReferenceTables Spare()
        {
            var tables = Dec.ReferenceTables.Take();
            tables.Return();
            return tables;
        }

        private static void AssertSpareIs(Dec.ReferenceTables expected)
        {
            var tables = Dec.ReferenceTables.Take();
            try
            {
                Assert.AreSame(expected, tables);
                Assert.AreEqual(0, tables.clones.Count);
                Assert.AreEqual(0, tables.ids.Count);
                Assert.AreEqual(0, tables.unordered.Count);
            }
            finally
            {
                tables.Return();
            }
        }

        [Test]
        public void TakeAfterReturn()
        {
            var tables = Dec.ReferenceTables.Take();
            tables.clones[new object()] = new object();
            tables.ids[new object()] = 0;
            tables.unordered.Add(new object());
            tables.Return();

            AssertSpareIs(tables);
        }

        [Test]
        public void TakeWhileTaken()
        {
            var outer = Dec.ReferenceTables.Take();
            var inner = Dec.ReferenceTables.Take();
            Assert.AreNotSame(outer, inner);

            inner.Return();
            outer.Return();

            AssertSpareIs(outer);
        }

        public enum Table
        {
            Clones,
            Ids,
            Unordered,
        }

        [Test]
        public void ReturnRetainedMax([Values] Table table, [Values] bool over)
        {
            var tables = Dec.ReferenceTables.Take();
            int count = Dec.ReferenceTables.RetainedMax + (over ? 1 : 0);
            for (int i = 0; i < count; ++i)
            {
                var key = new object();
                switch (table)
                {
                    case Table.Clones:
                        tables.clones[key] = key;
                        break;
                    case Table.Ids:
                        tables.ids[key] = i;
                        break;
                    case Table.Unordered:
                        tables.unordered.Add(key);
                        break;
                }
            }
            tables.Return();

            var next = Dec.ReferenceTables.Take();
            next.Return();
            if (over)
            {
                Assert.AreNotSame(tables, next);
            }
            else
            {
                Assert.AreSame(tables, next);
            }
        }

        [Test]
        public void ReturnSmallAfterLarge()
        {
            var tables = Dec.ReferenceTables.Take();
            for (int i = 0; i < 10000; ++i)
            {
                tables.clones[new object()] = null;
            }
            tables.Return();

            Assert.AreSame(tables, Dec.ReferenceTables.Take());
            for (int i = 0; i < 3; ++i)
            {
                tables.clones[new object()] = null;
                tables.ids[new object()] = i;
                tables.unordered.Add(new object());
            }
            tables.Return();

            AssertSpareIs(tables);
        }

        public enum Operation
        {
            Clone,
            Checksum,
            ChecksumDiff,
        }

        [Test]
        public void CallReturnsTables([Values] Operation operation)
        {
            var shared = new Node { value = 1 };
            var pair = new Pair { a = shared, b = shared };
            var set = new SetHolder { set = new HashSet<Node> { new Node { value = 2 } } };

            var spare = Spare();
            switch (operation)
            {
                case Operation.Clone:
                    Dec.Recorder.Clone(pair);
                    AssertSpareIs(spare);
                    Dec.Recorder.Clone(set);
                    break;
                case Operation.Checksum:
                    Dec.Recorder.Checksum(pair);
                    AssertSpareIs(spare);
                    Dec.Recorder.Checksum(set);
                    break;
                case Operation.ChecksumDiff:
                    Dec.Recorder.ChecksumDiff(pair, new Pair { a = shared, b = new Node { value = 3 } }, report => { });
                    break;
            }

            AssertSpareIs(spare);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RunOnUnreferenced(Operation operation)
        {
            var shared = new Node { value = 1 };
            var pair = new Pair { a = shared, b = shared };
            if (operation == Operation.Clone)
            {
                Dec.Recorder.Clone(pair);
            }
            else
            {
                Dec.Recorder.Checksum(pair);
            }

            return new WeakReference(shared);
        }

        [Test]
        public void TablesReleaseObjects([Values(Operation.Clone, Operation.Checksum)] Operation operation, [Values] bool afterLarge)
        {
            // Dropping the spare gives the call fresh tables, which it clears with Clear(); after a large call, a small one removes its entries one at a time instead.
            Dec.ReferenceTables.Take();
            if (afterLarge)
            {
                var tables = Dec.ReferenceTables.Take();
                for (int i = 0; i < 10000; ++i)
                {
                    tables.clones[new object()] = null;
                }
                tables.Return();
            }

            var weak = RunOnUnreferenced(operation);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(weak.IsAlive);
        }

        [Test]
        public void CloneRepeated()
        {
            var shared = new Node { value = 1 };
            var pair = new Pair { a = shared, b = shared };

            var first = Dec.Recorder.Clone(pair);
            shared.value = 2;
            var second = Dec.Recorder.Clone(pair);

            Assert.AreNotSame(first.a, second.a);
            Assert.AreSame(second.a, second.b);
            Assert.AreEqual(2, second.a.value);
            Assert.AreEqual(1, first.a.value);
        }

        [Test]
        public void ChecksumRepeated()
        {
            var shared = new Node { value = 1 };
            var pair = new Pair { a = shared, b = shared };
            ulong expected = Dec.Recorder.Checksum(pair);

            Dec.Recorder.Checksum(new Pair { a = shared, b = new Node { value = 2 } });
            Assert.AreEqual(expected, Dec.Recorder.Checksum(pair));

            // A node first seen in an unordered context and then by reference is an error, so a stale unordered table would report one here.
            Dec.Recorder.Checksum(new SetHolder { set = new HashSet<Node> { shared } });
            Assert.AreEqual(expected, Dec.Recorder.Checksum(pair));
        }

        public class Nesting : IRecordable
        {
            public static bool Enabled;
            public static Node Inner;
            public static object InnerResult;
            public static bool InnerClone;

            public Node first;
            public Node second;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref first, nameof(first));

                // Clone resolves `first` on its read pass and Checksum on its only pass, so by now it's in the outer call's table.
                if (Enabled && recorder.Mode == (InnerClone ? Dec.Recorder.Direction.Read : Dec.Recorder.Direction.Write))
                {
                    InnerResult = InnerClone ? (object)Dec.Recorder.Clone(Inner) : Dec.Recorder.Checksum(Inner);
                }

                recorder.Shared().Record(ref second, nameof(second));
            }
        }

        [Test]
        public void NestedClone()
        {
            var shared = new Node { value = 4 };
            var nesting = new Nesting { first = shared, second = shared };

            Nesting.Enabled = true;
            Nesting.InnerClone = true;
            Nesting.Inner = shared;
            Nesting.InnerResult = null;
            try
            {
                var clone = Dec.Recorder.Clone(nesting);
                var inner = (Node)Nesting.InnerResult;

                Assert.AreSame(clone.first, clone.second);
                Assert.AreNotSame(clone.first, inner);
                Assert.AreNotSame(shared, inner);
                Assert.AreEqual(4, inner.value);
            }
            finally
            {
                Nesting.Enabled = false;
                Nesting.Inner = null;
                Nesting.InnerResult = null;
            }
        }

        [Test]
        public void NestedChecksum()
        {
            var shared = new Node { value = 4 };
            var nesting = new Nesting { first = shared, second = shared };
            ulong expectedOuter = Dec.Recorder.Checksum(nesting);
            ulong expectedInner = Dec.Recorder.Checksum(shared);

            Nesting.Enabled = true;
            Nesting.InnerClone = false;
            Nesting.Inner = shared;
            Nesting.InnerResult = null;
            try
            {
                Assert.AreEqual(expectedOuter, Dec.Recorder.Checksum(nesting));
                Assert.AreEqual(expectedInner, (ulong)Nesting.InnerResult);
            }
            finally
            {
                Nesting.Enabled = false;
                Nesting.Inner = null;
                Nesting.InnerResult = null;
            }
        }

        [Test]
        public void CloneAfterThrow()
        {
            var shared = new Node { value = 1 };
            var spare = Spare();

            Assert.Catch(() => Dec.Recorder.Clone(new ThrowingPair { a = shared }));
            AssertSpareIs(spare);

            shared.value = 2;
            var clone = Dec.Recorder.Clone(new Pair { a = shared, b = shared });
            Assert.AreEqual(2, clone.a.value);
            Assert.AreSame(clone.a, clone.b);
        }

        [Test]
        public void ChecksumAfterThrow()
        {
            var shared = new Node { value = 1 };
            var pair = new Pair { a = shared, b = shared };
            ulong expected = Dec.Recorder.Checksum(pair);
            var spare = Spare();

            Assert.Catch(() => Dec.Recorder.Checksum(new ThrowingPair { a = shared }));
            AssertSpareIs(spare);

            Assert.AreEqual(expected, Dec.Recorder.Checksum(pair));
        }
    }
}
