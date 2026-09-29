using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dec
{
    internal class WriterChecksum
    {
        public bool AllowReflection { get => false; }
        public Recorder.IUserSettings UserSettings { get; }

        internal Dictionary<object, int> seenReferences = new Dictionary<object, int>();
        internal HashSet<object> seenReferencesUnordered = new HashSet<object>();

        // One node per depth, reused: a checksum walk is strictly depth-first and holds on to no children, so a node's subtree is always finished before its next sibling is created.
        private List<WriterNodeChecksum> nodes = new List<WriterNodeChecksum>();

        internal WriterNodeChecksum NodeAt(int depth)
        {
            if (depth == nodes.Count)
            {
                nodes.Add(new WriterNodeChecksum(this, depth));
            }

            return nodes[depth];
        }

        // FNV1a, 64-bit
        private ulong checksum = 14695981039346656037UL;
        private Path path;
        internal void AddChecksum(ulong value, WriterNodeChecksum node)
        {
            if (stopAt == 0)
            {
                // this currently behaves badly with PushChecksum/PopChecksum, but that's not a problem for now
                return;
            }

            // Only ChecksumDiff's stop mode reads the path back, and building it is most of the cost.
            if (stopAt > 0)
            {
                path = node.Path;
            }

            --stopAt;

            checksum ^= value;
            checksum *= 1099511628211UL;
        }
        internal ulong PushChecksum()
        {
            var result = checksum;
            checksum = 14695981039346656037UL;
            return result;
        }
        internal ulong PopChecksum(ulong push)
        {
            var result = checksum;
            checksum = push;
            return result;
        }
        internal ulong FinishChecksum()
        {
            return checksum;
        }

        // checksum-diff code
        private long stopAt = -1;
        internal void SetStopAt(long stopAt)
        {
            this.stopAt = stopAt;
        }
        internal long GetTokensIfNotStopped()
        {
            // yes this is a gnarly way of doing this
            return -stopAt - 1;
        }
        internal Path GetStopPath()
        {
            return path;
        }

        public WriterChecksum(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
        }

        public WriterNodeChecksum Start()
        {
            return WriterNodeChecksum.Start(this);
        }
    }

    internal class WriterNodeChecksum : WriterNodePooled
    {
        private readonly WriterChecksum writer;
        private readonly int depth;

        public override bool AllowReflection { get => writer.AllowReflection; }
        public override bool AllowDecPath { get => true; }
        public override bool AllowCloning { get => false;  }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Checksum; }
        public override Recorder.IUserSettings UserSettings { get => writer.UserSettings; }

        internal bool unordered;

        public static WriterNodeChecksum Start(WriterChecksum writer)
        {
            return Acquire(writer, 0, false, new Recorder.Settings(), PathPending.Built(new PathRoot("ROOT")));
        }

        private static WriterNodeChecksum Acquire(WriterChecksum writer, int depth, bool unordered, Recorder.Settings settings, PathPending path)
        {
            var node = writer.NodeAt(depth);
            node.Reuse(settings, path);
            node.unordered = unordered;
            return node;
        }

        internal WriterNodeChecksum(WriterChecksum writer, int depth)
        {
            this.writer = writer;
            this.depth = depth;
        }

        private enum NodeTag
        {
            Child,
            Primitive,
            Enum,
            String,
            Type,
            Dec,
            PathRef,
            Null,
            Reference,
            NotReference,
            Array,
            List,
            Dictionary,
            HashSet,
            Queue,
            Stack,
            Tuple,
            Record,
            Convertible,
            TagClass,
            ByteArray,
        }

        // this should be WriterNodeChecksum but this C# doesn't support that
        public override WriterNode CreateRecorderChild(string label, Recorder.Settings settings)
        {
            writer.AddChecksum((int)NodeTag.Child, this);
            return Acquire(writer, depth + 1, unordered, settings, PathPending.Member(this, label));
        }

        // this should be WriterNodeChecksum but this C# doesn't support that
        public override WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Settings settings)
        {
            // This currently doesn't happen ever.
            throw new NotImplementedException("Reflection child creation is not implemented in WriterNodeChecksum.");
        }

        private WriterNodeChecksum CreateNamedChild(bool unordered, Recorder.Settings settings, PathPending path)
        {
            writer.AddChecksum((int)NodeTag.Child, this);
            return Acquire(writer, depth + 1, this.unordered || unordered, settings, path);
        }

        public override void WritePrimitive(object value)
        {
            writer.AddChecksum((int)NodeTag.Primitive, this);

            if (value is double)
            {
                writer.AddChecksum((ulong)BitConverter.DoubleToInt64Bits((double)value), this);
            }
            else if (value is float)
            {
                writer.AddChecksum((ulong)BitConverter.SingleToInt32Bits((float)value), this);
            }
            else if (value is long)
            {
                writer.AddChecksum((ulong)(long)value, this);
            }
            else if (value is ulong)
            {
                writer.AddChecksum((ulong)value, this);
            }
            else if (value is int)
            {
                writer.AddChecksum((ulong)(int)value, this);
            }
            else if (value is uint)
            {
                writer.AddChecksum((uint)value, this);
            }
            else if (value is short)
            {
                writer.AddChecksum((ulong)(short)value, this);
            }
            else if (value is ushort)
            {
                writer.AddChecksum((ushort)value, this);
            }
            else if (value is sbyte)
            {
                writer.AddChecksum((ulong)(sbyte)value, this);
            }
            else if (value is byte)
            {
                writer.AddChecksum((ulong)(byte)value, this);
            }
            else
            {
                // optimize later maybe
                WriteString(value.ToString());
            }
        }

        public override void WriteEnum(object value)
        {
            writer.AddChecksum((int)NodeTag.Enum, this);
            writer.AddChecksum((ulong)(int)value, this);
        }

        public override void WriteString(string value)
        {
            writer.AddChecksum((int)NodeTag.String, this);
            writer.AddChecksum((ulong)value.Length, this);
            foreach (char c in value)
            {
                writer.AddChecksum((ulong)c, this);
            }
        }

        public override void WriteType(Type value)
        {
            writer.AddChecksum((int)NodeTag.Type, this);
            WriteString(value.ComposeDecFormatted());   // cache this for less string manipulation?
        }

        public override void WriteDec(Dec value)
        {
            writer.AddChecksum((int)NodeTag.Dec, this);
            WriteString(value?.DecName ?? "");
        }

        public override void WriteDecPathRef(object value)
        {
            writer.AddChecksum((int)NodeTag.PathRef, this);
            WriteString(Database.GetDecPathFromObj(value));
        }

        public override void WriteExplicitNull()
        {
            FlagAsNull();

            writer.AddChecksum((int)NodeTag.Null, this);
        }

        public override bool WriteReference(object value)
        {
            if (writer.seenReferencesUnordered.Contains(value))
            {
                Dbg.Err("Attempting to reference object first seen in an unordered context; this will cause problems. Come to Discord and pester me if you need this fixed.");
                writer.AddChecksum((int)NodeTag.Reference, this);
                writer.AddChecksum(~0UL, this); // welp
                return true;
            }

            if (writer.seenReferences.ContainsKey(value))
            {
                writer.AddChecksum((int)NodeTag.Reference, this);
                writer.AddChecksum((ulong)writer.seenReferences[value], this);
                return true;
            }

            // not previously seen
            writer.AddChecksum((int)NodeTag.NotReference, this);
            if (unordered)
            {
                writer.seenReferencesUnordered.Add(value);
            }
            else
            {
                writer.seenReferences[value] = writer.seenReferences.Count;
            }

            return false;
        }

        private void WriteArrayRank(WriterNodeChecksum node, Array value, Type referencedType, int rank, int[] indices)
        {
            if (rank == value.Rank)
            {
                Serialization.ComposeElement(node, value.GetValue(indices), referencedType);
            }
            else
            {
                for (int i = 0; i < value.GetLength(rank); ++i)
                {
                    indices[rank] = i;

                    var child = node.CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.IndexMultidim(this, indices, rank + 1));
                    WriteArrayRank(child, value, referencedType, rank + 1, indices);
                }
            }
        }

        public override void WriteArray(Array value)
        {
            Type referencedType = value.GetType().GetElementType();

            writer.AddChecksum((int)NodeTag.Array, this);
            writer.AddChecksum((ulong)value.Rank, this);

            if (value.Rank == 1)
            {
                writer.AddChecksum((ulong)value.Length, this);

                // fast path
                for (int i = 0; i < value.Length; ++i)
                {
                    var child = CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.Index(this, i));
                    Serialization.ComposeElement(child, value.GetValue(i), referencedType);
                }
            }
            else
            {
                for (int i = 0; i < value.Rank; ++i)
                {
                    writer.AddChecksum((ulong)value.GetLength(i), this);
                }

                // slow path
                int[] indices = new int[value.Rank];
                WriteArrayRank(this, value, referencedType, 0, indices);
            }
        }

        public override void WriteByteArray(byte[] value)
        {
            writer.AddChecksum((int)NodeTag.ByteArray, this);
            writer.AddChecksum((ulong)value.Length, this);

            // a reinterpret might make this faster
            foreach (byte b in value)
            {
                writer.AddChecksum((ulong)b, this);
            }
        }

        public override void WriteList(IList value)
        {
            Type referencedType = value.GetType().GetGenericInterfaceArguments(typeof(IList<>))[0];

            writer.AddChecksum((int)NodeTag.List, this);
            writer.AddChecksum((ulong)value.Count, this);

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.Index(this, i)), value[i], referencedType);
            }
        }

        public override void WriteDictionary(IDictionary value)
        {
            Type referencedType = value.GetType().GetGenericInterfaceArguments(typeof(IDictionary<,>))[1];

            writer.AddChecksum((int)NodeTag.Dictionary, this);
            writer.AddChecksum((ulong)value.Count, this);

            // Dictionary iteration order is non-deterministic, which breaks reference
            // tracking (we can't assign stable reference IDs if encounter order varies).
            //
            // Solution: checksum each key in unordered mode, then sort entries by key
            // checksum to get a canonical order. Values are then serialized in that
            // sorted order as ordered children, where reference tracking works normally.
            //
            // If two keys produce the same checksum (hash collision), we can't
            // canonically order their values, so those fall back to unordered
            // accumulation — no worse than the previous fully-unordered behavior.
            //
            // Key checksums are always paired with their values in the same checksum
            // block. Without pairing, {[A,A],[B,B]} and {[A,B],[B,A]} produce the same
            // checksum since both key and value sums are independently commutative.

            // Phase 1: Compute key checksums in unordered mode for sorting. Any key
            // references are added to seenReferencesUnordered, same as before.
            var entries = new List<(ulong keyChecksum, object key, object val)>();

            // Key and Value rather than foreach, which boxes a DictionaryEntry per element; each read of a value-type Key boxes it again, so it's read once.
            IDictionaryEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                object key = iterator.Key;

                ulong push = writer.PushChecksum();
                Serialization.ComposeElement(CreateNamedChild(true, RecorderSettings.CreateChild(), PathPending.DictionaryKey(this)), key, typeof(object));
                ulong keyChecksum = writer.PopChecksum(push);

                entries.Add((keyChecksum, key, iterator.Value));
            }

            // Phase 2: Sort by key checksum for canonical value ordering.
            entries.Sort((a, b) => a.keyChecksum.CompareTo(b.keyChecksum));

            // Phase 3: Serialize paired (key checksum + value) blocks in sorted order.
            int i = 0;
            while (i < entries.Count)
            {
                int groupStart = i;
                ulong groupKey = entries[i].keyChecksum;

                // Find extent of entries sharing this key checksum.
                while (i < entries.Count && entries[i].keyChecksum == groupKey)
                {
                    i++;
                }

                if (i - groupStart == 1)
                {
                    // Unique key checksum: this entry has a deterministic position.
                    // Pair the key checksum with the value, serialized as ordered.
                    writer.AddChecksum(entries[groupStart].keyChecksum, this);
                    Serialization.ComposeElement(CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.DictionaryValue(this, entries[groupStart].key)), entries[groupStart].val, referencedType);
                }
                else
                {
                    // Colliding key checksums: can't determine canonical order among
                    // these entries. Pair each key checksum with its value, then
                    // accumulate pairs order-independently (addition, not XOR, so
                    // identical pairs don't cancel out).
                    ulong groupAccumulator = 0;
                    for (int j = groupStart; j < i; j++)
                    {
                        ulong push = writer.PushChecksum();
                        writer.AddChecksum(entries[j].keyChecksum, this);
                        Serialization.ComposeElement(CreateNamedChild(true, RecorderSettings.CreateChild(), PathPending.DictionaryValue(this, entries[j].key)), entries[j].val, referencedType);
                        ulong result = writer.PopChecksum(push);
                        groupAccumulator += result;
                    }
                    writer.AddChecksum(groupAccumulator, this);
                }
            }
        }

        public override void WriteHashSet(IEnumerable value)
        {
            Type referencedType = value.GetType().GetGenericInterfaceArguments(typeof(ISet<>))[0];

            writer.AddChecksum((int)NodeTag.HashSet, this);
            writer.AddChecksum((ulong)value.Cast<object>().Count(), this);

            // This is a weird setup.
            // We want to be order-independent here, but we don't know what order hashSet keys are in.
            // My current solution is to accumulate the checksum of the keys and XOR them.
            // Because HashSet can't have duplicates, we . . . are maybe okay against people having duplicate elements?
            ulong accumulator = 0;
            foreach (var entry in value)
            {
                ulong push = writer.PushChecksum();
                Serialization.ComposeElement(CreateNamedChild(true, RecorderSettings.CreateChild(), PathPending.HashSetElement(this)), entry, referencedType);
                ulong result = writer.PopChecksum(push);

                // this is a weird way to combine, but this avoids issues where pairs of identical items cancel out, and I haven't found a good case where this doesn't work
                accumulator += result;
            }

            writer.AddChecksum(accumulator, this);
        }

        public override void WriteQueue(IEnumerable value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = UtilCollectionReflect.QueueToArray(keyType)(value);

            writer.AddChecksum((int)NodeTag.Queue, this);
            writer.AddChecksum((ulong)array.Length, this);

            for (int i = 0; i < array.Length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.QueueElement(this, i)), array.GetValue(i), keyType);
            }
        }

        public override void WriteStack(IEnumerable value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = UtilCollectionReflect.StackToArray(keyType)(value);

            writer.AddChecksum((int)NodeTag.Stack, this);
            writer.AddChecksum((ulong)array.Length, this);

            for (int i = 0; i < array.Length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.StackElement(this, i)), array.GetValue(i), keyType);
            }
        }

        public override void WriteTuple(object value, TupleElementNamesAttribute names)
        {
            writer.AddChecksum((int)NodeTag.Tuple, this);

            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.TupleItem(this, i)), value.GetType().GetProperty(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteValueTuple(object value, TupleElementNamesAttribute names)
        {
            writer.AddChecksum((int)NodeTag.Tuple, this);

            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(false, RecorderSettings.CreateChild(), PathPending.TupleItem(this, i)), value.GetType().GetField(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteRecord(IRecordable value)
        {
            writer.AddChecksum((int)NodeTag.Record, this);

            RecorderRun(value);
        }

        public override void WriteConvertible(Converter converter, object value)
        {
            try
            {
                writer.AddChecksum((int)NodeTag.Convertible, this);

                if (converter is ConverterString converterString)
                {
                    WriteString(converterString.WriteObj(value));
                }
                else if (converter is ConverterRecord converterRecord)
                {
                    RecorderRun(converterRecord, value);
                }
                else if (converter is ConverterFactory converterFactory)
                {
                    RecorderRun(converterFactory, value);
                }
                else
                {
                    Dbg.Err($"Somehow ended up with an unsupported converter {converter.GetType()}");
                }
            }
            catch (Exception e)
            {
                Dbg.Ex(e);
            }
        }

        public override void TagClass(Type type)
        {
            writer.AddChecksum((int)NodeTag.TagClass, this);

            WriteType(type);
        }
    }
}
