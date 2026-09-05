using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dec
{
    // Coordinator for a single Reflection.Enumerate pass.
    internal class WriterIntrospect
    {
        public Recorder.IUserSettings UserSettings { get; }
        public Func<Reflection.Entry, bool> ShouldDescend { get; }

        // A Dec root traverses the way the Composer writes it (WriterXmlCompose) rather than the way Recorder.Write does (WriterXmlRecord): fields by reflection, no dec-path references, and no reference system at all.
        public bool Compose { get; }

        // Mirrors WriterXmlRecord's reference bookkeeping: first-encounter position sharedness per object, giving the same multiplicity errors a real save emits and terminating repeat/cyclic encounters.
        internal Dictionary<object, (bool sharedPosition, Path path)> seenObjects = new Dictionary<object, (bool, Path)>();

        public WriterIntrospect(Recorder.IUserSettings userSettings, Func<Reflection.Entry, bool> shouldDescend, bool compose)
        {
            this.UserSettings = userSettings;
            this.ShouldDescend = shouldDescend;
            this.Compose = compose;
        }
    }

    internal sealed class WriterNodeIntrospect : WriterNode
    {
        private readonly WriterIntrospect writer;
        private readonly Reflection.Entry entry;
        private readonly bool isRoot;
        private readonly int depth;
        private const int MaxRecursionDepth = 100;

        // Set beneath a position with no channel to carry a write back (a ConverterFactory body, or a value-type root), so Writable stays honest below it.
        private bool writableSuppressed;

        private bool noted;
        private bool descendConsulted;
        private bool descendDecision;

        public override bool AllowReflection
        {
            get
            {
                return writer.Compose;
            }
        }

        public override bool AllowDecPath
        {
            get
            {
                return !writer.Compose;
            }
        }

        public override Recorder.Purpose Intent
        {
            get
            {
                return Recorder.Purpose.Serialization;
            }
        }

        public override Recorder.IUserSettings UserSettings
        {
            get
            {
                return writer.UserSettings;
            }
        }

        private WriterNodeIntrospect(WriterIntrospect writer, Reflection.Entry entry, bool isRoot, int depth, bool writableSuppressed, Recorder.Settings settings, Path path) : base(settings, path)
        {
            this.writer = writer;
            this.entry = entry;
            this.isRoot = isRoot;
            this.depth = depth;
            this.writableSuppressed = writableSuppressed;

            entry.Path = path;
            entry.Writable = !isRoot && !writableSuppressed && path.IsSettable();
        }

        public static WriterNodeIntrospect StartRecord(WriterIntrospect writer, Reflection.Entry rootEntry, Path path, bool writableSuppressed)
        {
            return new WriterNodeIntrospect(writer, rootEntry, isRoot: true, 0, writableSuppressed, new Recorder.Settings() { shared = Recorder.Settings.Shared.Flexible }, path);
        }

        public static WriterNodeIntrospect StartDec(WriterIntrospect writer, Reflection.Entry rootEntry, Dec dec)
        {
            // The composer names a generic Dec's element after its first non-generic base and tags the concrete class; the path follows the element name.
            Type type = dec.GetType();
            while (type.IsGenericType)
            {
                type = type.BaseType;
            }

            return new WriterNodeIntrospect(writer, rootEntry, isRoot: true, 0, writableSuppressed: false, new Recorder.Settings(), new PathDec(type.ComposeDecFormatted(), dec.DecName));
        }

        internal override void NoteDeclaredType(Type fieldType, object value)
        {
            // RecordAsThis fires ComposeElement twice on the same node; the outer static type is the declared type, so the first call wins.
            if (noted)
            {
                return;
            }
            noted = true;

            entry.DeclaredType = fieldType;
            entry.Value = value;

            // A live Type value's GetType() is the private System.RuntimeType; the writer normalizes it before deciding ref-ness, so we must too or Types report as sharable. The `is Type` test subsumes ComposeElement's exact-RuntimeType comparison.
            var effectiveType = value is Type ? typeof(Type) : (value?.GetType() ?? fieldType);

            // Compose mode has no reference system, so nothing under a Dec root is ever a reference, whatever a Record() body's .Shared() asks for.
            entry.Shared = !isRoot && !writer.Compose && RecorderSettings.shared != Recorder.Settings.Shared.Deny && Util.CanBeShared(effectiveType);
        }

        private WriterNodeIntrospect CreateChildEntry(string label, Recorder.Settings settings, Path path)
        {
            var childEntry = new Reflection.Entry() { Label = label, Parent = entry };
            entry.ChildrenMutable.Add(childEntry);
            return new WriterNodeIntrospect(writer, childEntry, isRoot: false, depth + 1, writableSuppressed, settings, path);
        }

        public override WriterNode CreateRecorderChild(string label, Recorder.Settings settings)
        {
            return CreateChildEntry(label, settings, new PathMember(Path, label));
        }

        public override WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Settings settings)
        {
            return CreateChildEntry(field.Name, settings, new PathMember(Path, field.Name));
        }

        internal override bool ShouldReflectFields()
        {
            return ShouldReplayBody();
        }

        // Every descent passes through here, whether into a record body, a converter body, a reflection walk, or a container, so the depth cap holds for all of them; compose mode has no reference system to terminate a cycle otherwise.
        private bool ConsultDescend()
        {
            if (depth >= MaxRecursionDepth)
            {
                Dbg.Err($"[{Path.Serialize()}]: Depth limiter ran into an unshareable node stack that's too deep. Recommend using more `.Shared()` calls to allow for stack splitting. Enumeration stops here.");
                return false;
            }

            if (isRoot)
            {
                return true;
            }

            // RecordAsThis can route two descents through one node; the predicate is consulted once per entry.
            if (!descendConsulted)
            {
                descendConsulted = true;
                descendDecision = writer.ShouldDescend?.Invoke(entry) ?? true;
            }

            return descendDecision;
        }

        // Leaves: the value is already captured via NoteDeclaredType, and no serialized form is produced.
        public override void WritePrimitive(object value) { }
        public override void WriteEnum(object value) { }
        public override void WriteString(string value) { }
        public override void WriteType(Type value) { }
        public override void WriteDec(Dec value) { }
        public override void WriteByteArray(byte[] value) { }

        public override void WriteDecPathRef(object value)
        {
            // This position serializes as a dec-path reference, which is a reference no matter what the settings say.
            entry.Shared = true;
        }

        public override void WriteConvertible(Converter converter, object value)
        {
            // A string-form converter records no positions beneath it.
            if (converter is ConverterString)
            {
                return;
            }

            if (converter is ConverterFactory)
            {
                // ConverterFactory.Write has no way to hand back a modified value (a struct conversion or a replaced instance would be lost), so nothing beneath it is settable.
                writableSuppressed = true;
            }

            if (!ShouldReplayBody())
            {
                return;
            }

            // The XML writer's MakeRecorderContextChild() has no counterpart here: converter children take their settings from the Record() parameters, and this entry's sharedness is already decided.

            // Converter bodies are user code at a system boundary; the serialization writers report their exceptions the same way.
            try
            {
                if (converter is ConverterRecord converterRecord)
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

        public override void WriteExplicitNull()
        {
            FlagAsNull();
        }

        public override void WriteError()
        {
            entry.Writable = false;
        }

        public override void TagClass(Type type)
        {
            // Diagnostic parity only; the entry already carries the value, so the actual type is discoverable.
            FlagAsClass();
        }

        public override bool WriteReference(object value, Path path)
        {
            // The composer never registers references (WriterXmlCompose.RegisterReference), so every encounter is written in full.
            if (writer.Compose)
            {
                return false;
            }

            bool sharedPosition = RecorderSettings.shared != Recorder.Settings.Shared.Deny;

            if (!writer.seenObjects.TryGetValue(value, out var prior))
            {
                writer.seenObjects[value] = (sharedPosition, path);
                return false;
            }

            // Either mismatch means the entry tree cannot match the file, and both fire on a real save of the same object.
            if (!prior.sharedPosition || !sharedPosition)
            {
                ErrReferenceMismatch(prior.sharedPosition, path, prior.path, value);
                return true;
            }

            // A genuine repeat encounter: the file emits a ref here, so the entry stays a leaf.
            return true;
        }

        // The gates before replaying a Record-style body, whether a recordable's, a converter's, or a reflection walk. Shared object references are leaves: the file contains (at most a ref to) this object here, and descending through one would enumerate the entire reachable document. The asThis flag matches the writer's own ref gate, which skips reference handling for RecordAsThis values.
        private bool ShouldReplayBody()
        {
            if (!isRoot && entry.Shared && !flaggedAsThis)
            {
                return false;
            }

            return ConsultDescend();
        }

        public override void WriteRecord(IRecordable value)
        {
            if (!ShouldReplayBody())
            {
                return;
            }

            value.Record(new RecorderWriter(this));
        }

        public override void WriteArray(Array value)
        {
            if (!ConsultDescend())
            {
                return;
            }

            WriteArrayContents(value, null);
        }

        private void WriteArrayContents(Array value, Func<int, Path> pathForIndex)
        {
            Type referencedType = value.GetType().GetElementType();

            if (value.Rank == 1)
            {
                for (int i = 0; i < value.Length; ++i)
                {
                    Serialization.ComposeElement(CreateChildEntry(i.ToString(CultureInfo.InvariantCulture), RecorderSettings.CreateChild(), pathForIndex != null ? pathForIndex(i) : new PathIndex(Path, i)), value.GetValue(i), referencedType);
                }
            }
            else
            {
                int[] indices = new int[value.Rank];
                WriteArrayRank(this, value, referencedType, 0, indices);
            }
        }

        private void WriteArrayRank(WriterNodeIntrospect node, Array value, Type referencedType, int rank, int[] indices)
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

                    // Every rank level builds its path from the array node, as the XML writer does, so an intermediate entry's Parent runs one level ahead of its path's parent; inert, since multidim positions are unsettable.
                    var child = node.CreateChildEntry(i.ToString(CultureInfo.InvariantCulture), RecorderSettings.CreateChild(), new PathIndexMultidim(Path, indices.Take(rank + 1).ToArray()));

                    WriteArrayRank(child, value, referencedType, rank + 1, indices);
                }
            }
        }

        public override void WriteList(IList value)
        {
            if (!ConsultDescend())
            {
                return;
            }

            Type referencedType = value.GetType().GetGenericInterfaceArguments(typeof(IList<>))[0];

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(CreateChildEntry(i.ToString(CultureInfo.InvariantCulture), RecorderSettings.CreateChild(), new PathIndex(Path, i)), value[i], referencedType);
            }
        }

        public override void WriteDictionary(IDictionary value)
        {
            if (!ConsultDescend())
            {
                return;
            }

            var dictArgs = value.GetType().GetGenericInterfaceArguments(typeof(IDictionary<,>));
            Type keyType = dictArgs[0];
            Type valueType = dictArgs[1];
            Type pairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);

            IDictionaryEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                // Pair entries have no ComposeElement pass of their own, so their type and value are filled in here; the label is a display-only rendering of the key. The key/value children take the XML writer's paths, which hang off the dictionary itself, so a Record() body sees the same Context under introspection as on a real write; their entry Parent is the pair while their path parent is the dictionary, which is inert since neither position is settable.
                string keyString = iterator.Key?.ToString() ?? "";
                var pair = CreateChildEntry(keyString, RecorderSettings, new PathDictionaryPair(Path, keyString));
                pair.noted = true;
                pair.entry.DeclaredType = pairType;
                pair.entry.Value = Activator.CreateInstance(pairType, iterator.Key, iterator.Value);

                Serialization.ComposeElement(pair.CreateChildEntry("key", RecorderSettings.CreateChild(), new PathDictionaryKey(Path)), iterator.Key, keyType);
                Serialization.ComposeElement(pair.CreateChildEntry("value", RecorderSettings.CreateChild(), new PathDictionaryValueUnpathable(Path)), iterator.Value, valueType);
            }
        }

        public override void WriteHashSet(IEnumerable value)
        {
            if (!ConsultDescend())
            {
                return;
            }

            Type keyType = value.GetType().GetGenericInterfaceArguments(typeof(ISet<>))[0];

            int i = 0;
            IEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                Serialization.ComposeElement(CreateChildEntry(i.ToString(CultureInfo.InvariantCulture), RecorderSettings.CreateChild(), new PathHashSetElement(Path)), iterator.Current, keyType);
                ++i;
            }
        }

        public override void WriteQueue(IEnumerable value)
        {
            if (!ConsultDescend())
            {
                return;
            }

            // Mirrors the XML writer, which serializes queues as arrays.
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = UtilCollectionReflect.QueueToArray(keyType)(value);

            WriteArrayContents(array, i => new PathQueueElement(Path, i));
        }

        public override void WriteStack(IEnumerable value)
        {
            if (!ConsultDescend())
            {
                return;
            }

            // Mirrors the XML writer, reversal included, so entry order matches file order.
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = UtilCollectionReflect.StackToArray(keyType)(value);
            Array.Reverse(array);

            WriteArrayContents(array, i => new PathStackElement(Path, i));
        }

        public override void WriteTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            if (!ConsultDescend())
            {
                return;
            }

            var args = value.GetType().GenericTypeArguments;

            for (int i = 0; i < args.Length; ++i)
            {
                Serialization.ComposeElement(CreateChildEntry(i.ToString(CultureInfo.InvariantCulture), RecorderSettings.CreateChild(), new PathTupleItem(Path, i)), value.GetType().GetProperty(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteValueTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            if (!ConsultDescend())
            {
                return;
            }

            var args = value.GetType().GenericTypeArguments;

            for (int i = 0; i < args.Length; ++i)
            {
                Serialization.ComposeElement(CreateChildEntry(i.ToString(CultureInfo.InvariantCulture), RecorderSettings.CreateChild(), new PathTupleItem(Path, i)), value.GetType().GetField(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }
    }
}
