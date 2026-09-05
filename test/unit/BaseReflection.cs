using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DecTest
{
    // The Reflection round-trip modes, and the entry-tree helpers they and the Reflection golden tests share.
    public partial class Base
    {
        // A value distinguishable from the one already at a position, of the same runtime type; null where none can be made, meaning the existing value is written back unchanged.
        protected static object Sentinel(object value)
        {
            if (value is Enum)
            {
                foreach (var member in Enum.GetValues(value.GetType()))
                {
                    if (!Equals(member, value))
                    {
                        return member;
                    }
                }

                return null;
            }

            object candidate;
            switch (value)
            {
                case bool v:
                    candidate = !v;
                    break;
                case byte v:
                    candidate = unchecked((byte)(v + 1));
                    break;
                case sbyte v:
                    candidate = unchecked((sbyte)(v + 1));
                    break;
                case short v:
                    candidate = unchecked((short)(v + 1));
                    break;
                case ushort v:
                    candidate = unchecked((ushort)(v + 1));
                    break;
                case int v:
                    candidate = unchecked(v + 1);
                    break;
                case uint v:
                    candidate = unchecked(v + 1);
                    break;
                case long v:
                    candidate = unchecked(v + 1);
                    break;
                case ulong v:
                    candidate = unchecked(v + 1);
                    break;
                case float v:
                    candidate = v + 1;
                    break;
                case double v:
                    candidate = v + 1;
                    break;
                case char v:
                    candidate = unchecked((char)(v + 1));
                    break;
                case string v:
                    candidate = v + "!";
                    break;
                default:
                    return null;
            }

            // Extremes such as float.MaxValue or NaN absorb the change, and a sentinel equal to the original would verify nothing.
            return Equals(candidate, value) ? null : candidate;
        }

        protected static Dec.Reflection.Entry FindByPath(Dec.Reflection.Entry entry, Dec.Path path)
        {
            if (entry.Path.Equals(path))
            {
                return entry;
            }

            foreach (var child in entry.Children)
            {
                var found = FindByPath(child, path);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        // Asserts two entry trees describe the same structure, comparing values only as far as a write/read preserves them: scalars by equality, byte arrays by content, everything else by null-ness.
        private static void AssertTreesEquivalent(Dec.Reflection.Entry lhs, Dec.Reflection.Entry rhs)
        {
            string where = lhs.Path.Serialize();
            Assert.AreEqual(lhs.Label, rhs.Label, where);
            Assert.AreEqual(lhs.DeclaredType, rhs.DeclaredType, where);
            Assert.AreEqual(lhs.Shared, rhs.Shared, where);
            Assert.AreEqual(lhs.Writable, rhs.Writable, where);
            Assert.AreEqual(lhs.Path, rhs.Path, where);

            if (lhs.Value is byte[] lhsBytes)
            {
                Assert.AreEqual(lhsBytes, rhs.Value as byte[], where);
            }
            else if (IsScalar(lhs.Value))
            {
                Assert.AreEqual(lhs.Value, rhs.Value, where);
            }
            else
            {
                Assert.AreEqual(lhs.Value == null, rhs.Value == null, where);
            }

            // The label sequence first, so a mismatch reports both whole sequences rather than the first differing pair.
            Assert.AreEqual(lhs.Children.Select(c => c.Label).ToArray(), rhs.Children.Select(c => c.Label).ToArray(), where);
            for (int i = 0; i < lhs.Children.Count; ++i)
            {
                AssertTreesEquivalent(lhs.Children[i], rhs.Children[i]);
            }
        }

        private static bool IsScalar(object value)
        {
            if (value == null)
            {
                return false;
            }

            var type = value.GetType();
            return type.IsPrimitive || type.IsEnum || value is string || value is Type || value is Dec.Dec;
        }

        // Roots the sweep can write back into: a reference type resolved through Record(), reflection, or an index. Not a value type, and not a converter's instance, whose body may hand back a replacement the root has no slot for.
        private static bool CanSweep(object input)
        {
            return !input.GetType().IsValueType && (input is Dec.IRecordable || input is IList);
        }

        private static Dec.Reflection.Entry ReflectionBeforeWrite<T>(T input, bool sweep, Action<Action> underWriteExpectations)
        {
            Dec.Reflection.Entry before = null;
            underWriteExpectations(() => before = Dec.Reflection.Enumerate(input));

            if (sweep && CanSweep(input))
            {
                underWriteExpectations(() => ReflectionSweep(input, before));
            }

            return before;
        }

        // Unlike WritableEntriesAccept in the Reflection goldens, which re-enumerates after each write, this verifies once after all writes and then restores, because the fixture goes on to a real round trip. Only recorded positions are restored, so a Record() body's side effects outside its Record calls persist.
        private static void ReflectionSweep<T>(T input, Dec.Reflection.Entry before)
        {
            // Parent before child, so a struct written back whole never lands on top of a sentinel already placed in one of its fields.
            var writes = new List<(Dec.Path path, object sentinel, object original)>();
            var paths = new HashSet<Dec.Path>();
            var pending = new Stack<Dec.Reflection.Entry>();
            pending.Push(before);
            while (pending.Count > 0)
            {
                var entry = pending.Pop();
                if (entry.Writable)
                {
                    Assert.IsTrue(paths.Add(entry.Path), $"duplicate writable path {entry.Path.Serialize()}");

                    var sentinel = Sentinel(entry.Value);
                    Dec.Reflection.SetByPath(input, entry.Path, sentinel ?? entry.Value);
                    writes.Add((entry.Path, sentinel, entry.Value));
                }

                foreach (var child in entry.Children)
                {
                    pending.Push(child);
                }
            }

            var during = Dec.Reflection.Enumerate(input);
            foreach (var write in writes)
            {
                if (write.sentinel == null)
                {
                    continue;
                }

                var landed = FindByPath(during, write.path);
                Assert.IsNotNull(landed, $"{write.path.Serialize()}: this position vanished after the sweep. A Record() body whose shape depends on a recorded value cannot be swept; exclude the test from ReflectionSet with [ValuesExcept].");
                Assert.AreEqual(write.sentinel, landed.Value, $"{write.path.Serialize()}: the sentinel did not land. A Record() body that records a write-path temporary discards writes (see Reflection.SetByPath); if that is intended, exclude the test from ReflectionSet with [ValuesExcept].");
            }

            foreach (var write in writes)
            {
                Dec.Reflection.SetByPath(input, write.path, write.original);
            }

            AssertTreesEquivalent(before, Dec.Reflection.Enumerate(input));
        }

        private static void ReflectionAfterRead<T>(T deserialized, Dec.Reflection.Entry before, Action<Action> underWriteExpectations)
        {
            Dec.Reflection.Entry readBack = null;
            underWriteExpectations(() => readBack = Dec.Reflection.Enumerate(deserialized));

            AssertTreesEquivalent(before, readBack);
        }
    }
}
