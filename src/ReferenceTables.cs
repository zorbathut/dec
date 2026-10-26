using System;
using System.Collections.Generic;

namespace Dec
{
    // The identity tables Clone and Checksum fill as they walk an object graph. Each thread keeps one set between calls, so that a large graph doesn't regrow them, through the large object heap, on every call.
    internal sealed class ReferenceTables
    {
        // A table that grew past this is dropped rather than kept, so that one huge call doesn't hold its memory on the thread for good.
        internal const int RetainedMax = 100000;

        // Clear() wipes every bucket, so after a large call it would cost a small one more than the call itself; a call that used no more than this fraction of the most the tables have held removes its entries one at a time instead.
        private const int RemoveEachRatio = 64;

        // No initializer: one would run on the first thread only.
        [ThreadStatic] private static ReferenceTables Spare;

        internal readonly Dictionary<object, object> clones = new Dictionary<object, object>(ComparerIdentity.Instance);
        internal readonly Dictionary<object, int> ids = new Dictionary<object, int>(ComparerIdentity.Instance);
        internal readonly HashSet<object> unordered = new HashSet<object>(ComparerIdentity.Instance);

        private int peak;
        private object[] keys = Array.Empty<object>();

        // Empties the slot, so a call nested inside this one gets tables of its own.
        internal static ReferenceTables Take()
        {
            var tables = Spare;
            if (tables == null)
            {
                return new ReferenceTables();
            }

            Spare = null;
            return tables;
        }

        internal void Return()
        {
            int used = Math.Max(clones.Count, Math.Max(ids.Count, unordered.Count));
            if (used > RetainedMax)
            {
                return;
            }

            peak = Math.Max(peak, used);
            if (used * RemoveEachRatio <= peak)
            {
                RemoveEach(clones);
                RemoveEach(ids);
                RemoveEach(unordered);
            }
            else
            {
                clones.Clear();
                ids.Clear();
                unordered.Clear();
            }

            Spare = this;
        }

        private void RemoveEach<T>(Dictionary<object, T> table)
        {
            int count = table.Count;
            KeysReserve(count);
            table.Keys.CopyTo(keys, 0);
            for (int i = 0; i < count; ++i)
            {
                table.Remove(keys[i]);
                keys[i] = null;
            }
        }

        private void RemoveEach(HashSet<object> table)
        {
            int count = table.Count;
            KeysReserve(count);
            table.CopyTo(keys);
            for (int i = 0; i < count; ++i)
            {
                table.Remove(keys[i]);
                keys[i] = null;
            }
        }

        private void KeysReserve(int count)
        {
            if (keys.Length < count)
            {
                keys = new object[count];
            }
        }
    }
}
