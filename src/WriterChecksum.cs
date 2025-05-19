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

        // FNV1a, 64-bit
        private ulong checksum = 14695981039346656037UL;
        internal void AddChecksum(ulong value)
        {
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

        public WriterChecksum(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
        }

        public WriterNodeChecksum Start()
        {
            return WriterNodeChecksum.Start(this);
        }
    }

    internal class WriterNodeChecksum : WriterNode
    {
        private WriterChecksum writer;

        public override bool AllowReflection { get => writer.AllowReflection; }
        public override bool AllowDecPath { get => true; }
        public override bool AllowAsThis { get => false; }
        public override bool AllowCloning { get => false;  }
        public override Recorder.IUserSettings UserSettings { get => writer.UserSettings; }

        internal bool unordered;

        public static WriterNodeChecksum Start(WriterChecksum writer)
        {
            return new WriterNodeChecksum(writer, false, new Recorder.Settings(), new PathRoot("CHECKSUM"));
        }

        private WriterNodeChecksum(WriterChecksum writer, bool unordered, Recorder.Settings settings, Path path) : base(settings, path)
        {
            this.writer = writer;
            this.unordered = unordered;
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
        }

        // this should be WriterNodeChecksum but this C# doesn't support that
        public override WriterNode CreateRecorderChild(string label, Recorder.Settings settings)
        {
            writer.AddChecksum((int)NodeTag.Child);
            return new WriterNodeChecksum(writer, unordered, settings, new PathMember(Path, label));
        }

        // this should be WriterNodeChecksum but this C# doesn't support that
        public override WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Settings settings)
        {
            // This currently doesn't happen ever.
            throw new NotImplementedException("Reflection child creation is not implemented in WriterNodeChecksum.");
        }

        private WriterNode CreateNamedChild(string label, bool unordered, Recorder.Settings settings, Path path)
        {
            writer.AddChecksum((int)NodeTag.Child);
            return new WriterNodeChecksum(writer, this.unordered || unordered, settings, new PathMember(path, label));
        }

        public override void WritePrimitive(object value)
        {
            writer.AddChecksum((int)NodeTag.Primitive);

            if (value is double)
            {
                writer.AddChecksum((ulong)BitConverter.DoubleToInt64Bits((double)value));
            }
            else if (value is float)
            {
                writer.AddChecksum((ulong)BitConverter.SingleToInt32Bits((float)value));
            }
            else if (value is long)
            {
                writer.AddChecksum((ulong)(long)value);
            }
            else if (value is ulong)
            {
                writer.AddChecksum((ulong)value);
            }
            else if (value is int)
            {
                writer.AddChecksum((ulong)(int)value);
            }
            else if (value is uint)
            {
                writer.AddChecksum((uint)value);
            }
            else if (value is short)
            {
                writer.AddChecksum((ulong)(short)value);
            }
            else if (value is ushort)
            {
                writer.AddChecksum((ushort)value);
            }
            else if (value is sbyte)
            {
                writer.AddChecksum((ulong)(sbyte)value);
            }
            else if (value is byte)
            {
                writer.AddChecksum((ulong)(byte)value);
            }
            else
            {
                // optimize later maybe
                WriteString(value.ToString());
            }
        }

        public override void WriteEnum(object value)
        {
            writer.AddChecksum((int)NodeTag.Enum);
            writer.AddChecksum((ulong)(int)value);
        }

        public override void WriteString(string value)
        {
            writer.AddChecksum((int)NodeTag.String);
            writer.AddChecksum((ulong)value.Length);
            foreach (char c in value)
            {
                writer.AddChecksum((ulong)c);
            }
        }

        public override void WriteType(Type value)
        {
            writer.AddChecksum((int)NodeTag.Type);
            WriteString(value.ComposeDecFormatted());   // cache this for less string manipulation?
        }

        public override void WriteDec(Dec value)
        {
            writer.AddChecksum((int)NodeTag.Dec);
            WriteString(value?.DecName ?? "");
        }

        public override void WriteDecPathRef(object value)
        {
            writer.AddChecksum((int)NodeTag.PathRef);
            WriteString(Database.GetDecPath(value));
        }

        public override void WriteExplicitNull()
        {
            writer.AddChecksum((int)NodeTag.Null);
        }

        public override bool WriteReference(object value, Path path)
        {
            if (writer.seenReferencesUnordered.Contains(value))
            {
                Dbg.Err("Attempting to reference object first seen in an unordered context; this will cause problems. Come to Discord and pester me if you need this fixed.");
                writer.AddChecksum((int)NodeTag.Reference);
                writer.AddChecksum(~0UL); // welp
                return true;
            }

            if (writer.seenReferences.ContainsKey(value))
            {
                writer.AddChecksum((int)NodeTag.Reference);
                writer.AddChecksum((ulong)writer.seenReferences[value]);
                return true;
            }

            // not previously seen
            writer.AddChecksum((int)NodeTag.NotReference);
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

        private void WriteArrayRank(Array value, Type referencedType, int rank, int[] indices)
        {
            if (rank == value.Rank)
            {
                Serialization.ComposeElement(this, value.GetValue(indices), referencedType);
            }
            else
            {
                for (int i = 0; i < value.GetLength(rank); ++i)
                {
                    indices[rank] = i;

                    var child = CreateNamedChild("li", false, RecorderSettings.CreateChild(), new PathIndexMultidim(Path, indices.ToArray()));
                    WriteArrayRank(value, referencedType, rank + 1, indices);
                }
            }
        }

        public override void WriteArray(Array value)
        {
            Type referencedType = value.GetType().GetElementType();

            writer.AddChecksum((int)NodeTag.Array);
            writer.AddChecksum((ulong)value.Rank);

            if (value.Rank == 1)
            {
                writer.AddChecksum((ulong)value.Length);

                // fast path
                for (int i = 0; i < value.Length; ++i)
                {
                    var child = CreateNamedChild("li", false, RecorderSettings.CreateChild(), new PathIndex(Path, i));
                    Serialization.ComposeElement(child, value.GetValue(i), referencedType);
                }
            }
            else
            {
                for (int i = 0; i < value.Rank; ++i)
                {
                    writer.AddChecksum((ulong)value.GetLength(i));
                }

                // slow path
                int[] indices = new int[value.Rank];
                WriteArrayRank(value, referencedType, 0, indices);
            }
        }

        public override void WriteList(IList value)
        {
            Type referencedType = value.GetType().GetGenericArguments()[0];

            writer.AddChecksum((int)NodeTag.List);
            writer.AddChecksum((ulong)value.Count);

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild("li", false, RecorderSettings.CreateChild(), new PathIndex(Path, i)), value[i], referencedType);
            }
        }

        public override void WriteDictionary(IDictionary value)
        {
            Type referencedType = value.GetType().GetGenericArguments()[1];

            writer.AddChecksum((int)NodeTag.Dictionary);
            writer.AddChecksum((ulong)value.Count);

            // This is a weird setup.
            // We want to be order-independent here, but we don't know what order dictionary keys are in.
            // My current solution is to accumulate the checksum of the keys and XOR them.
            // These are full key/value checksums, not independent, so this should be immune to [A,A], [B,B] vs [A,B], [B,A] issues.
            ulong accumulator = 0;
            foreach (DictionaryEntry entry in value)
            {
                ulong push = writer.PushChecksum();
                Serialization.ComposeElement(CreateNamedChild("key", true, RecorderSettings.CreateChild(), new PathMember(Path, "key")), entry.Key, typeof(object));
                Serialization.ComposeElement(CreateNamedChild("val", true, RecorderSettings.CreateChild(), new PathMember(Path, "val")), entry.Value, referencedType);
                ulong result = writer.PopChecksum(push);

                // this is a weird way to combine, but this avoids issues where pairs of identical items cancel out, and I haven't found a good case where this doesn't work
                accumulator += result;
            }

            writer.AddChecksum(accumulator);
        }

        public override void WriteHashSet(IEnumerable value)
        {
            Type referencedType = value.GetType().GetGenericArguments()[0];

            writer.AddChecksum((int)NodeTag.HashSet);
            writer.AddChecksum((ulong)value.Cast<object>().Count());

            // This is a weird setup.
            // We want to be order-independent here, but we don't know what order hashSet keys are in.
            // My current solution is to accumulate the checksum of the keys and XOR them.
            // Because HashSet can't have duplicates, we . . . are maybe okay against people having duplicate elements?
            ulong accumulator = 0;
            foreach (var entry in value)
            {
                ulong push = writer.PushChecksum();
                Serialization.ComposeElement(CreateNamedChild("val", true, RecorderSettings.CreateChild(), new PathMember(Path, "val")), entry, referencedType);
                ulong result = writer.PopChecksum(push);

                // this is a weird way to combine, but this avoids issues where pairs of identical items cancel out, and I haven't found a good case where this doesn't work
                accumulator += result;
            }

            writer.AddChecksum(accumulator);
        }

        public override void WriteQueue(IEnumerable value)
        {
            writer.AddChecksum((int)NodeTag.Queue);

            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = value.GetType().GetMethod("ToArray").Invoke(value, new object[] { }) as Array;

            WriteArray(array);
        }

        public override void WriteStack(IEnumerable value)
        {
            writer.AddChecksum((int)NodeTag.Stack);

            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = value.GetType().GetMethod("ToArray").Invoke(value, new object[] { }) as Array;

            WriteArray(array);
        }

        public override void WriteTuple(object value, TupleElementNamesAttribute names)
        {
            writer.AddChecksum((int)NodeTag.Tuple);

            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild("li", false, RecorderSettings.CreateChild(), new PathIndex(Path, i)), value.GetType().GetProperty(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteValueTuple(object value, TupleElementNamesAttribute names)
        {
            writer.AddChecksum((int)NodeTag.Tuple);

            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild("li", false, RecorderSettings.CreateChild(), new PathIndex(Path, i)), value.GetType().GetField(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteRecord(IRecordable value)
        {
            writer.AddChecksum((int)NodeTag.Record);

            value.Record(new RecorderWriter(this));
        }

        public override void WriteConvertible(Converter converter, object value)
        {
            try
            {
                writer.AddChecksum((int)NodeTag.Convertible);

                if (converter is ConverterString converterString)
                {
                    WriteString(converterString.WriteObj(value));
                }
                else if (converter is ConverterRecord converterRecord)
                {
                    converterRecord.RecordObj(value, new RecorderWriter(this));
                }
                else if (converter is ConverterFactory converterFactory)
                {
                    converterFactory.WriteObj(value, new RecorderWriter(this));
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
            writer.AddChecksum((int)NodeTag.TagClass);

            WriteType(type);
        }
    }
}
