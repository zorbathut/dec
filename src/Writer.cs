using System;
using System.Collections;
using System.Linq;

namespace Dec
{
    // A pooled writer node's position, described relative to the node whose path it extends and built into a Path only when something asks for it; most traversals never do.
    internal struct PathPending
    {
        private enum Kind : byte
        {
            Built,
            Same,
            Member,
            Index,
            IndexMultidim,
            QueueElement,
            StackElement,
            TupleItem,
            DictionaryKey,
            DictionaryValue,
            HashSetElement,
        }

        private WriterNodePooled parent;
        private object detail;
        private int index;
        private Kind kind;

        // Truncated to keep this struct at 24 bytes, one per pooled node; a reused parent would have to come back after an exact multiple of 65536 reuses to slip past the check.
        private ushort parentGeneration;

        private PathPending(Kind kind, WriterNodePooled parent, object detail, int index)
        {
            this.kind = kind;
            this.parent = parent;
            this.parentGeneration = (ushort)(parent?.generation ?? 0);
            this.detail = detail;
            this.index = index;
        }

        public static PathPending Built(Path path) { return new PathPending(Kind.Built, null, path, 0); }
        public static PathPending Same(WriterNodePooled parent) { return new PathPending(Kind.Same, parent, null, 0); }
        public static PathPending Member(WriterNodePooled parent, string label) { return new PathPending(Kind.Member, parent, label, 0); }
        public static PathPending Index(WriterNodePooled parent, int index) { return new PathPending(Kind.Index, parent, null, index); }
        public static PathPending QueueElement(WriterNodePooled parent, int index) { return new PathPending(Kind.QueueElement, parent, null, index); }
        public static PathPending StackElement(WriterNodePooled parent, int index) { return new PathPending(Kind.StackElement, parent, null, index); }
        public static PathPending TupleItem(WriterNodePooled parent, int index) { return new PathPending(Kind.TupleItem, parent, null, index); }
        public static PathPending DictionaryKey(WriterNodePooled parent) { return new PathPending(Kind.DictionaryKey, parent, null, 0); }
        public static PathPending DictionaryValue(WriterNodePooled parent, object key) { return new PathPending(Kind.DictionaryValue, parent, key, 0); }
        public static PathPending HashSetElement(WriterNodePooled parent) { return new PathPending(Kind.HashSetElement, parent, null, 0); }

        // Takes the live index array rather than a copy; the entries up to this position's rank don't change while it's being written.
        public static PathPending IndexMultidim(WriterNodePooled parent, int[] indices, int rank) { return new PathPending(Kind.IndexMultidim, parent, indices, rank); }

        public Path Build()
        {
            if (parent != null && (ushort)parent.generation != parentGeneration)
            {
                // Following the parent now would describe some other position, and can loop back on itself.
                Dbg.Err("Internal error: a pending path outlived its parent node");
                return new PathRoot("UNKNOWN");
            }

            switch (kind)
            {
                case Kind.Built: return (Path)detail;
                case Kind.Same: return parent.Path;
                case Kind.Member: return new PathMember(parent.Path, (string)detail);
                case Kind.Index: return new PathIndex(parent.Path, index);
                case Kind.IndexMultidim: return new PathIndexMultidim(parent.Path, ((int[])detail).Take(index).ToArray());
                case Kind.QueueElement: return new PathQueueElement(parent.Path, index);
                case Kind.StackElement: return new PathStackElement(parent.Path, index);
                case Kind.TupleItem: return new PathTupleItem(parent.Path, index);
                case Kind.DictionaryKey: return new PathDictionaryKey(parent.Path);
                case Kind.DictionaryValue: return new PathDictionaryValue(parent.Path, detail.ToString());
                case Kind.HashSetElement: return new PathHashSetElement(parent.Path);
                default: Dbg.Err($"Internal error: unknown path kind {kind}"); return null;
            }
        }
    }

    internal abstract class WriterNode
    {
        private Recorder.Settings settings;

        public WriterNode(Recorder.Settings settings)
        {
            this.settings = settings;
        }

        public Recorder.Settings RecorderSettings { get => settings; }
        public abstract bool AllowReflection { get; }
        public abstract bool AllowDecPath { get; }
        public virtual bool AllowAsThis { get => true; }
        public virtual bool AllowCloning { get => false; }
        public abstract Recorder.Purpose Intent { get; }
        public abstract Recorder.IUserSettings UserSettings { get; }

        public abstract Path Path { get; }

        // I'm not real happy with the existence of this function; it's kind of a hack so that a shared Converter that writes a string or an int can avoid errors
        public void MakeRecorderContextChild()
        {
            settings = settings.CreateChild();
        }

        public abstract WriterNode CreateRecorderChild(string label, Recorder.Settings settings);
        public abstract WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Settings settings);

        // Take record of the declared type; currently used only by the introspection system.
        internal virtual void NoteDeclaredType(Type fieldType, object value) { }

        // Consulted before the node's fields are walked by reflection; a backend that trims or depth-limits its traversal declines here.
        internal virtual bool ShouldReflectFields() { return true; }

        public abstract void WritePrimitive(object value);
        public abstract void WriteEnum(object value);
        public abstract void WriteString(string value);
        public abstract void WriteType(Type value);
        public abstract void WriteDec(Dec value);
        public abstract void WriteDecPathRef(object value);
        public abstract void WriteExplicitNull();
        public abstract bool WriteReference(object value);
        public abstract void WriteArray(Array value);
        public abstract void WriteByteArray(byte[] value);
        public abstract void WriteList(IList value);
        public abstract void WriteDictionary(IDictionary value);
        public abstract void WriteHashSet(IEnumerable value);
        public abstract void WriteQueue(IEnumerable value);
        public abstract void WriteStack(IEnumerable value);
        public abstract void WriteTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names);
        public abstract void WriteValueTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names);
        public abstract void WriteRecord(IRecordable value);
        public abstract void WriteConvertible(Converter converter, object value);
        public virtual void WriteCloneCopy(object value) { Dbg.Err("Internal error, attempting to clone an object without being in clone mode"); }
        public virtual void WriteError() { }  // "this should be a thing, but it isn't, sorry"

        public abstract void TagClass(Type type);

        // Clears per-position state so a writer that reuses its nodes can hand this one out again.
        protected void Reset(Recorder.Settings settings)
        {
            this.settings = settings;
            flaggedAsClass = false;
            flaggedAsThis = false;
        }

        protected virtual RecorderWriter RecorderAcquire()
        {
            return new RecorderWriter(this);
        }

        // Every user Record() body on the write side runs through one of these, so its recorder is in scope for exactly as long as the body is running.
        internal void RecorderRun(IRecordable value)
        {
            var recorder = RecorderAcquire();
            var outer = recorder.Open();
            try
            {
                value.Record(recorder);
            }
            finally
            {
                recorder.Close(outer);
            }
        }

        internal void RecorderRun(ConverterRecord converter, object value)
        {
            var recorder = RecorderAcquire();
            var outer = recorder.Open();
            try
            {
                converter.RecordObj(value, recorder);
            }
            finally
            {
                recorder.Close(outer);
            }
        }

        internal void RecorderRun(ConverterFactory converter, object value)
        {
            var recorder = RecorderAcquire();
            var outer = recorder.Open();
            try
            {
                converter.WriteObj(value, recorder);
            }
            finally
            {
                recorder.Close(outer);
            }
        }

        // general behavior that polymorphics should not reimplement (so far at least?)
        protected bool flaggedAsClass = false;
        protected bool flaggedAsThis = false;

        // attempts to flag as self, posts error if it can't
        public bool FlagAsThis()
        {
            flaggedAsThis = true;
            return true;
        }
        protected bool FlagAsClass()
        {
            if (flaggedAsThis)
            {
                Dbg.Err("Polymorphic Record() detected after a RecordAsThis(); this does not work, polymorphic Record() must be the first item in a This() chain");
                return false;
            }

            flaggedAsClass = true;
            return true;
        }

        protected bool FlagAsNull()
        {
            if (flaggedAsClass || flaggedAsThis)
            {
                Dbg.Err("Null tag detected after a class tag or a RecordAsThis(); this currently does not work, RecordAsThis() must not be used on a null value");
                return false;
            }

            return true;
        }

        // An object met again at a position that can't share it with its first encounter; worded once for every backend that tracks references.
        internal static string ErrReferenceMismatch(bool priorWasShared, bool currentIsShared, Path path, Path priorPath, object referenced)
        {
            // The key position can be either side of this, depending on whether the container or the shared field was recorded first; whichever it is, it's the unshared side.
            Path unsharedPath = priorWasShared ? path : priorPath;

            string advice;
            if ((unsharedPath is PathDictionaryKey || unsharedPath is PathHashSetElement) && !Util.HashesByIdentity(referenced.GetType()))
            {
                // The unshared side is one Dec chose, not one the user wrote, so the usual advice would just send them looking for a decorator that can't help.
                advice = $"The dictionary key or hash set element here is unshared because {referenced.GetType()} overrides GetHashCode(), which cannot be .Shared() reliably.";
            }
            else
            {
                advice = "If this is coming from a Recorder setup, it's likely you either need a .Shared() decorator, or you need to ensure that this object is not serialized elsewhere.";
            }

            if (referenced is Array array && array.Length == 0)
            {
                advice += " (Note: C# empty arrays often refer to a single shared instance, and it's unclear what Dec should do about this. Come talk to us in Discord if you think you have a good solution. Lists do not have this behavior.)";
            }

            string attempted;
            if (priorWasShared)
            {
                attempted = "a new unshared reference";
            }
            else if (currentIsShared)
            {
                attempted = "a new shared reference";
            }
            else
            {
                attempted = "a second unshared reference";
            }

            string message = $"Attempted to create {attempted} at [{path.Serialize()}] to a previously-seen {(priorWasShared ? "shared" : "unshared")} object at [{priorPath.Serialize()}]. This cannot be serialized faithfully. {advice}";
            Dbg.Err(message);
            return message;
        }
    }

    // A node its writer hands out again once the position it described is finished. Its path is built only on request, and it keeps one recorder for every body it runs.
    internal abstract class WriterNodePooled : WriterNode
    {
        private PathPending pathPending;
        private RecorderWriter recorder;

        // Bumped on every reuse, so a pending path can tell that its parent has moved on to another position.
        internal int generation;

        protected WriterNodePooled() : base(new Recorder.Settings())
        {
        }

        // One recorder per node is only enough because a pooled node is never re-entered through RecordAsThis.
        public sealed override bool AllowAsThis
        {
            get
            {
                return false;
            }
        }

        public override Path Path
        {
            get
            {
                // Once built, the path replaces its description, which then no longer needs the parent.
                var path = pathPending.Build();
                pathPending = PathPending.Built(path);
                return path;
            }
        }

        protected void Reuse(Recorder.Settings settings, PathPending path)
        {
            Reset(settings);
            ++generation;
            pathPending = path;
        }

        protected override RecorderWriter RecorderAcquire()
        {
            if (recorder == null)
            {
                recorder = new RecorderWriter(this);
            }

            recorder.Reset();
            return recorder;
        }
    }
}
