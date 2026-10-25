using System;
using System.Collections.Generic;

namespace Dec
{
    /// <summary>
    /// Base class for recordable elements.
    /// </summary>
    /// <remarks>
    /// Inheriting from this is the easiest way to support Recorder serialization.
    ///
    /// If you need to record a class that you can't modify the definition of, see the Converter system.
    /// </remarks>
    public interface IRecordable
    {
        /// <summary>
        /// Serializes or deserializes this object to Recorder.
        /// </summary>
        /// <remarks>
        /// This function is called both for serialization and deserialization. In most cases, you can simply call Recorder.Record functions to do the right thing.
        ///
        /// For more complicated requirements, check out Recorder's interface.
        ///
        /// The recorder is valid only on the calling thread and only until this call returns. Don't keep it, and don't record through it while a nested object's Record() is running.
        /// </remarks>
        /// <example>
        /// <code>
        ///     public void Record(Recorder recorder)
        ///     {
        ///         // The Recorder interface figures out the right thing based on context.
        ///         // Any members that are referenced elsewhere will be turned into refs automatically.
        ///         // Members that don't show up in the saved data will be left at their default value.
        ///         recorder.Record(ref integerMember, "integerMember");
        ///         recorder.Record(ref classMember, "classMember");
        ///         recorder.Record(ref structMember, "structMember");
        ///         recorder.Record(ref collectionMember, "collectionMember");
        ///     }
        /// </code>
        /// </example>
        void Record(Recorder recorder);
    }

    /// <summary>
    /// Allows the Recordable aspect to be disabled based on local properties, usersettings, or other factors.
    /// </summary>
    public interface IConditionalRecordable : IRecordable
    {
        /// <summary>
        /// Indicates whether this should be treated like an IRecordable.
        /// </summary>
        /// <remarks>
        /// If this function returns false, Dec will either use reflection (if available) or return an error as if the object was not serializable.
        /// </remarks>
        bool ShouldRecord(Recorder.IUserSettings userSettings);
    }

    // This exists solely to ensure I always remember to add the right functions both to Parameter and Recorder.
    internal interface IRecorder
    {
        void Record<T>(ref T value, string label);
        void RecordAsThis<T>(ref T value);

        Recorder.Parameters WithFactory(Dictionary<Type, Func<Type, object>> factories);
        Recorder.Parameters Bespoke_KeyTypeDict();
    }

    /// <summary>
    /// Main class for the serialization/deserialization system.
    /// </summary>
    /// <remarks>
    /// Recorder is used to call the main functions for serialization/deserialization. This includes both the static initiation functions (Read, Write) and the per-element status functions.
    ///
    /// To start serializing or deserializing an object, see Recorder.Read and Recorder.Write.
    ///
    /// A Recorder handed to a Record() function is valid only on the calling thread and only until that call returns.
    /// </remarks>
    public abstract partial class Recorder : IRecorder
    {
        public interface IUserSettings { }

        // The recorder whose Record() body is running on this thread, reader or writer. Only it may record; anything else is a recorder kept past its call, or an enclosing object's recorder reached from inside a nested Record().
        [ThreadStatic] private static Recorder Innermost;

        // Open and Close bracket a user Record() body; Open returns the recorder that was innermost before, for the matching Close to restore.
        internal Recorder Open()
        {
            if (Innermost == this)
            {
                Dbg.Err("Internal error: Recorder opened while it was already the innermost open recorder");
            }

            var outer = Innermost;
            Innermost = this;
            return outer;
        }

        internal void Close(Recorder outer)
        {
            // Restore before reporting; a throwing error handler would otherwise leave this thread pointing at a finished recorder.
            bool wasInnermost = Innermost == this;
            Innermost = outer;

            if (!wasInnermost)
            {
                Dbg.Err("Internal error: Recorder closed while it wasn't the innermost open recorder");
            }
        }

        internal bool IsInnermost()
        {
            return Innermost == this;
        }

        public struct Parameters : IRecorder
        {
            internal Recorder recorder;

            internal bool asThis;
            internal bool shared;

            internal bool bespoke_keytypedict;

            internal Dictionary<Type, Func<Type, object>> factories;

            /// <summary>
            /// Serialize or deserialize a member of a class.
            /// </summary>
            /// <remarks>
            /// See [`Dec.Recorder.Record`](xref:Dec.Recorder.Record*) for details.
            /// </remarks>
            public void Record<T>(ref T value, string label)
            {
                recorder.Record(ref value, label, this);
            }

            /// <summary>
            /// Serialize or deserialize a member of a class as if it were this class.
            /// </summary>
            /// <remarks>
            /// See [`Dec.Recorder.RecordAsThis`](xref:Dec.Recorder.RecordAsThis*) for details.
            /// </remarks>
            public void RecordAsThis<T>(ref T value)
            {
                Parameters parameters = this;
                parameters.asThis = true;

                if (parameters.shared)
                {
                    Dbg.Err("Recorder.RecordAsThis() called on Recorder.Parameters with sharing enabled. This is disallowed and sharing will be disabled.");
                    parameters.shared = false;
                }

                recorder.Record(ref value, "", parameters);
            }

            /// <summary>
            /// Add a factory layer to objects created during this call.
            /// </summary>
            /// <remarks>
            /// See [`Dec.Recorder.WithFactory`](xref:Dec.Recorder.WithFactory*) for details.
            /// </remarks>
            public Parameters WithFactory(Dictionary<Type, Func<Type, object>> factories)
            {
                Parameters parameters = this;
                if (parameters.factories != null)
                {
                    Dbg.Err("Recorder.WithFactory() called on Recorder.Parameters that already has factories. This has undefined results; currently replacing the old factory dictionary with the new one.");
                }

                if (parameters.shared)
                {
                    Dbg.Err("Recorder.WithFactory() called on a Shared Recorder.Parameters. This is disallowed; currently overriding Shared with factories.");
                    parameters.shared = false;
                }

                parameters.factories = factories;

                return parameters;
            }

            /// <summary>
            /// Informs the recorder system that the next parameter will be a Dictionary&lt;Type, V&gt; where the key should be interpreted as the type of the V.
            /// </summary>
            /// <remarks>
            /// /// See [`Dec.Recorder.Bespoke_KeyTypeDict`](xref:Dec.Recorder.Bespoke_KeyTypeDict*) for details.
            /// </remarks>
            public Recorder.Parameters Bespoke_KeyTypeDict()
            {
                Parameters parameters = this;
                parameters.bespoke_keytypedict = true;
                return parameters;
            }

            /// <summary>
            /// Allow sharing for class objects referenced during this call.
            /// </summary>
            /// <remarks>
            /// See [`Dec.Recorder.Shared`](xref:Dec.Recorder.Shared*) for details.
            /// </remarks>
            public Parameters Shared()
            {
                Parameters parameters = this;

                if (parameters.factories != null)
                {
                    Dbg.Err("Recorder.Shared() called on a WithFactory Recorder.Parameters. This is disallowed; currently erasing the factory and falling back on Shared.");
                    parameters.factories = null;
                }

                parameters.shared = true;
                return parameters;
            }

            /// <summary>
            /// Indicates whether this Recorder is being used for reading or writing.
            /// </summary>
            public Direction Mode { get => recorder.Mode; }

            internal readonly Settings CreateSettings()
            {
                return new Settings() { factories = factories, shared = shared ? Settings.Shared.Allow : Settings.Shared.Deny, bespoke_keytypedict = bespoke_keytypedict };
            }
        }

        // This is used for passing data to the Parse and Compose functions.
        internal struct Settings
        {
            internal enum Shared
            {
                Deny,   // "this cannot be shared"
                Flexible,   // "this is an implicit child of something that had been requested to be shared; let it be shared, but don't warn if it can't be"
                Allow,  // "this thing has specifically been requested to be shared, spit out a warning if it can't be shared"
            }

            public Dictionary<Type, Func<Type, object>> factories;
            public Shared shared;

            public bool bespoke_keytypedict;

            public Settings CreateChild()
            {
                Settings rv = this;
                if (rv.shared == Shared.Allow)
                {
                    // Downgrade this in case we have something like a List<int>; we don't want to spit out warnings about int not being sharable
                    rv.shared = Shared.Flexible;
                }

                rv.bespoke_keytypedict = false;
                return rv;
            }

            // For dictionary keys and hash set elements, which are hashed while being read, before anything they reference could have been filled in. Hash-by-value keys therefore cannot be shared, so we cut them off here. The reader has only a declared type, which may be a base class or interface of something that hashes by value, so it judges the object a reference resolves to instead.
            public Settings CreateChildKey(object key)
            {
                Settings rv = CreateChild();

                if (key != null && !Util.HashesByIdentity(key.GetType()))
                {
                    rv.shared = Shared.Deny;
                }

                return rv;
            }

            internal IRecordable CreateRecordableFromFactory(Type type, string name, ReaderNode node)
            {
                // Iterate back to the appropriate type.
                Type targetType = type;
                Func<Type, object> maker = null;
                while (targetType != null)
                {
                    if (factories.TryGetValue(targetType, out maker))
                    {
                        break;
                    }

                    targetType = targetType.BaseType;
                }

                if (maker == null)
                {
                    return (IRecordable)type.CreateInstanceSafe(name, node);
                }
                else
                {
                    // want to propogate this throughout the factories list to save on time later
                    // we're actually doing the same BaseType thing again, starting from scratch
                    Type writeType = type;
                    while (writeType != targetType)
                    {
                        factories[writeType] = maker;
                        writeType = writeType.BaseType;
                    }

                    // oh right and I guess we should actually make the thing too
                    var obj = maker(type);

                    if (obj == null)
                    {
                        // fall back to default behavior
                        return (IRecordable)type.CreateInstanceSafe(name, node);
                    }
                    else if (!type.IsAssignableFrom(obj.GetType()))
                    {
                        Dbg.Err($"Custom factory generated {obj.GetType()} when {type} was expected; falling back on a default object");
                        return (IRecordable)type.CreateInstanceSafe(name, node);
                    }
                    else
                    {
                        // now that we've checked this is of the right type
                        return (IRecordable)obj;
                    }
                }
            }
        }

        public abstract IUserSettings UserSettings { get; }

        /// <summary>
        /// Provide the Context for this serialization step, which can be used to get diagnostic information about "where" it is.
        /// </summary>
        public abstract Context Context { get; }

        /// <summary>
        /// Post a proper Context-decorated message to the info log.
        /// </summary>
        public void Inf(string message)
        {
            Context.Inf(message);
        }

        /// <summary>
        /// Post a proper Context-decorated message to the warning log.
        /// </summary>
        public void Wrn(string message)
        {
            Context.Wrn(message);
        }

        /// <summary>
        /// Post a proper Context-decorated message to the error log.
        /// </summary>
        public void Err(string message)
        {
            Context.Err(message);
        }

        /// <summary>
        /// Serialize or deserialize a member of a class.
        /// </summary>
        /// <remarks>
        /// This function serializes or deserializes a class member. Call it with a reference to the member and a label for the member (usually the member's name.)
        ///
        /// In most cases, you don't need to do anything different for read vs. write; this function will figure out the details and do the right thing.
        ///
        /// Be aware that if you're reading an object that was serialized as .Shared(), the object may not be fully deserialized by the time this function returns.
        /// If you aren't completely certain that the object was unshared when serialized, do not access members of the recorded object - they may be in an unknown state.
        /// If you need to do something after all objects are fully deserialized, wait until Recorder.Read() is finished and do a post-processing pass.
        /// </remarks>
        public void Record<T>(ref T value, string label)
        {
            Record(ref value, label, new Parameters());
        }

        /// <summary>
        /// Serialize or deserialize a member of a class as if it were this class.
        /// </summary>
        /// <remarks>
        /// This function serializes or deserializes a class member as if it were this entire class. Call it with a reference to the member.
        ///
        /// This is intended for cases where a class's contents are a single method and where an extra level of indirection in XML files isn't desired.
        ///
        /// In most cases, you don't need to do anything different for read vs. write; this function will figure out the details and do the right thing.
        ///
        /// This does not work, at all, if any of the classes in the `this` chain are inherited; it needs to be able to fall back on default types.
        /// </remarks>
        public void RecordAsThis<T>(ref T value)
        {
            Record(ref value, "", new Parameters() { recorder = this, asThis = true });
        }

        internal abstract void Record<T>(ref T value, string label, Parameters parameters);

        /// <summary>
        /// Add a factory layer to objects created during this call.
        /// </summary>
        /// <remarks>
        /// This allows you to create your own object initializer for things deserialized during this call. Standard Recorder functionality will apply on the object returned.
        /// This is sometimes a convenient way to set per-object defaults when deserializing.
        ///
        /// The initializer layout takes the form of a dictionary from Type to Func&lt;Type, object&gt;.
        /// When creating a new object, Dec will first look for a dictionary key of that type, then continue checking base types iteratively until it either finds a callback or passes `object`.
        /// That callback will be given a desired type and must return either an object of that type, an object of a type derived from that type, or `null`.
        /// On `null`, Dec will fall back to its default behavior. In each other case, it will then be deserialized as usual.
        ///
        /// The factory callback will persist until the next Recorder is called; recursive calls past that will be reset to default behavior.
        /// This means that it will effectively tunnel through supported containers such as List&lt;&gt; and Dictionary&lt;&gt;, allowing you to control the constructor of `CustomType` in ` List&lt;CustomType&gt;`.
        ///
        /// Be aware that any classes created with a factory callback added *cannot* be referenced from multiple places in Record hierarchy - the normal ref structure does not function with them.
        /// Also, be aware that excessively deep hierarchies full of factory callbacks may result in performance issues when writing pretty-print XML; this is not likely to be a problem in normal code, however.
        /// For performance's sake, this function does not duplicate `factories` and may modify it for efficiency reasons.
        /// It can be reused, but should not be modified by the user once passed into a function once.
        /// Recorder may modify this for caching purposes; if using multithreading, each concurrent Recorder call must use its own `factories` dictionary instance.
        ///
        /// This is incompatible with Shared().
        /// </remarks>
        public Parameters WithFactory(Dictionary<Type, Func<Type, object>> factories)
        {
            return new Parameters() { recorder = this, factories = factories };
        }

        /// <summary>
        /// Informs the recorder system that the next parameter will be a Dictionary&lt;Type, V&gt; where the key should be interpreted as the type of the V.
        /// </summary>
        /// <remarks>
        /// This is a specialty hack added for one specific user and may vanish with little warning, though if it does, it'll be replaced by something at least as capable.
        /// </remarks>
        public Recorder.Parameters Bespoke_KeyTypeDict()
        {
            return new Parameters() { recorder = this, bespoke_keytypedict = true };
        }

        /// <summary>
        /// Allow sharing for class objects referenced during this call.
        /// </summary>
        /// <remarks>
        /// Shared objects can be referenced from multiple classes. During serialization, these links will be stored; during deserialization, these links will be recreated.
        /// This is handy if (for example) your entities need to refer to each other for AI or targeting reasons.
        ///
        /// However, when reading, shared Recorder fields *must* be initially set to `null`.
        ///
        /// Dec objects are essentially treated as value types, and will be referenced appropriately even without this.
        ///
        /// This is incompatible with WithFactory().
        /// </remarks>
        public Parameters Shared()
        {
            return new Parameters() { recorder = this, shared = true };
        }

        /// <summary>
        /// Indicates that a field is intentionally unused and should be ignored.
        /// </summary>
        /// <remarks>
        /// Dec will output warnings if a field isn't being used, on the assumption that it's probably a mistake.
        ///
        /// Sometimes a field is ignored intentionally, usually for backwards compatibility reasons, and this function can be used to suppress that warning.
        /// </remarks>
        public virtual void Ignore(string label) { }

        /// <summary>
        /// Indicates whether this Recorder is being used for reading or writing.
        /// </summary>
        public enum Direction
        {
            Read,
            Write,
        }
        /// <summary>
        /// Indicates whether this Recorder is being used for reading or writing.
        /// </summary>
        public abstract Direction Mode { get; }

        /// <summary>
        /// Indicates what this Recorder is being used for.
        /// </summary>
        public enum Purpose
        {
            Serialization,
            Cloning,
            Checksum,
        }
        /// <summary>
        /// Indicates whether this Recorder is being used for serialization, cloning, or checksumming.
        /// </summary>
        public abstract Purpose Intent { get; }
    }

    // The labels one Record() body has used, kept to catch duplicates. Most bodies record a handful of fields, which a linear scan checks faster than hashing does; past Threshold it moves to a HashSet so a body with many fields doesn't go quadratic.
    internal class RecorderLabelSet
    {
        internal const int Threshold = 8;

        private string[] items;
        private int count;

        // Holds every label once there are more than Threshold.
        private HashSet<string> set;

        public int Count { get { return count; } }

        // As HashSet.Add: false, with nothing added, when the label is already present.
        public bool Add(string label)
        {
            if (count >= Threshold)
            {
                if (count == Threshold)
                {
                    set = set ?? new HashSet<string>();
                    for (int i = 0; i < Threshold; ++i)
                    {
                        set.Add(items[i]);
                    }
                }

                if (!set.Add(label))
                {
                    return false;
                }

                ++count;
                return true;
            }

            for (int i = 0; i < count; ++i)
            {
                if (items[i] == label)
                {
                    return false;
                }
            }

            items = items ?? new string[Threshold];
            items[count++] = label;
            return true;
        }

        public bool Contains(string label)
        {
            if (count > Threshold)
            {
                return set.Contains(label);
            }

            for (int i = 0; i < count; ++i)
            {
                if (items[i] == label)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            count = 0;

            // Cleared even below the threshold: a duplicate refused right at it has already copied the labels over.
            set?.Clear();
        }
    }

    // What a recorder's field checks decided about one field.
    internal enum RecorderFieldAdmission
    {
        Refused,
        AsThis,
        Child,
    }

    internal class RecorderWriter : Recorder
    {
        private bool asThis = false;
        private readonly RecorderLabelSet fields = new RecorderLabelSet();
        private readonly WriterNode node;

        internal RecorderWriter(WriterNode node)
        {
            this.node = node;
        }

        // For writers that reuse a recorder across bodies: clears what the previous body left behind.
        internal void Reset()
        {
            asThis = false;
            fields.Clear();
        }

        public override IUserSettings UserSettings { get => node.UserSettings; }

        public override Context Context { get => new Context(path: node.Path); }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            if (FieldAdmit(ref value, label, in parameters))
            {
                // Keep this call directly in Record<T>, here and in every override: on .NET 10 with dynamic PGO, the JIT inlines ComposeElement into each Record<T> instantiation, which makes a field-heavy Checksum measurably faster, but not when the call sits inside a helper, even an inlined one.
                Serialization.ComposeElement(node.CreateRecorderChild(label, parameters.CreateSettings()), value, typeof(T));
            }
        }

        // The checks every field write makes. False when the field was refused, or when RecordAsThis has already written it in place of this object; true when it's still to be written as a child.
        protected bool FieldAdmit<T>(ref T value, string label, in Parameters parameters)
        {
            var admission = FieldCheck(label, in parameters);
            if (admission == RecorderFieldAdmission.AsThis)
            {
                Serialization.ComposeElement(node, value, typeof(T), asThis: true);
                return false;
            }

            return admission == RecorderFieldAdmission.Child;
        }

        // Apart from FieldAdmit so the checks are written once in IL instead of once per field type; the JIT may still inline a copy into each Record<T>.
        private RecorderFieldAdmission FieldCheck(string label, in Parameters parameters)
        {
            if (!IsInnermost())
            {
                Dbg.Err($"Attempted to record `{label}` through a Recorder outside the Record() call it was passed to; skipping it. A Recorder is valid only on the calling thread, until its Record() call returns, and not while a nested object's Record() is running.");
                return RecorderFieldAdmission.Refused;
            }

            if (asThis)
            {
                Dbg.Err($"Attempting to write a second field after a RecordAsThis call");
                return RecorderFieldAdmission.Refused;
            }

            if (parameters.asThis)
            {
                if (fields.Count > 0)
                {
                    Dbg.Err($"Attempting to make a RecordAsThis call after writing a field");
                    return RecorderFieldAdmission.Refused;
                }

                asThis = true;

                if (!node.FlagAsThis())
                {
                    // just give up and skip it
                    return RecorderFieldAdmission.Refused;
                }

                if (node.AllowAsThis)
                {
                    return RecorderFieldAdmission.AsThis;
                }
            }

            if (!fields.Add(label))
            {
                Dbg.Err($"Field `{label}` written multiple times");
                return RecorderFieldAdmission.Refused;
            }

            return RecorderFieldAdmission.Child;
        }

        public override Direction Mode { get => Direction.Write; }
        public override Recorder.Purpose Intent { get => node.Intent; }
    }

    internal struct ReaderGlobals
    {
        public Dictionary<string, object> refs;
        public bool writeDecPaths;
        public bool allowRefs;
        public bool allowReflection;
        // Set at construction by Parser and Recorder.Read/ReadSimple, null for Clone; assigning at construction matters, because copies of this struct (RecorderReader, deferred ref closures) are taken before parsing starts.
        public Setup.Collection setupCollection;
    }

    internal class RecorderReader : Recorder
    {
        private bool asThis = false;
        private readonly ReaderNode node;
        private ReaderGlobals readerGlobals;
        private bool disallowShared;
        private readonly bool trackUsage;
        private readonly RecorderLabelSet seen = new RecorderLabelSet();

        internal RecorderReader(ReaderNode node, ReaderGlobals globals, bool disallowShared = false, bool trackUsage = false)
        {
            this.node = node;
            this.readerGlobals = globals;
            this.disallowShared = disallowShared;
            this.trackUsage = trackUsage;
        }

        // Every user Record()-style body on the read side runs through one of these, so its recorder is in scope for exactly as long as the body is running.
        internal void BodyRecord(IRecordable recordable)
        {
            var outer = Open();
            try
            {
                recordable.Record(this);
            }
            finally
            {
                Close(outer);
            }
        }

        internal void BodyRecord<T>(ref T value, RecordRef<T> record)
        {
            var outer = Open();
            try
            {
                record(ref value, this);
            }
            finally
            {
                Close(outer);
            }
        }

        internal object BodyRecord(ConverterRecord converter, object input)
        {
            var outer = Open();
            try
            {
                return converter.RecordObj(input, this);
            }
            finally
            {
                Close(outer);
            }
        }

        internal object BodyCreate(ConverterFactory converter)
        {
            var outer = Open();
            try
            {
                return converter.CreateObj(this);
            }
            finally
            {
                Close(outer);
            }
        }

        internal object BodyRead(ConverterFactory converter, object input)
        {
            var outer = Open();
            try
            {
                return converter.ReadObj(input, this);
            }
            finally
            {
                Close(outer);
            }
        }

        // For readers that are reused across bodies: clears what the previous body left behind.
        internal void Reset()
        {
            asThis = false;
            seen.Clear();
        }

        internal void AllowShared(ReaderGlobals newGlobals)
        {
            if (!disallowShared)
            {
                Dbg.Err($"{node.GetContext()}: Internal error, RecorderReader.AllowShared() called on a RecorderReader that already allows shared objects");
            }

            this.readerGlobals = newGlobals;
            disallowShared = false;
        }

        public override IUserSettings UserSettings { get => node.UserSettings; }

        public override Context Context { get => node.GetContext(); }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            var recorded = FieldAdmit(ref value, label, in parameters);
            if (recorded != null)
            {
                FieldRead(ref value, recorded, in parameters);
            }
        }

        // The checks every field read makes. Null when there's nothing more to do: the field was refused, isn't present, or RecordAsThis has already read this object; otherwise the child to read the field from.
        protected ReaderNode FieldAdmit<T>(ref T value, string label, in Parameters parameters)
        {
            var admission = FieldCheck(label, in parameters, out var recorded);
            if (admission == RecorderFieldAdmission.AsThis)
            {
                // Explicit cast here because we want an error if we have the wrong type!
                // This shouldn't need ParseElementTypd because it's guaranteed to have a valid input.
                var asThisNodes = node is ReaderNodeParseable nodeParseable ? new List<ReaderNodeParseable>() { nodeParseable } : ((ReaderNodeLayers)node).Layers;
                value = (T)Serialization.ParseElement(asThisNodes, typeof(T), value, readerGlobals, parameters.CreateSettings(), asThis: true);
                return null;
            }

            return recorded;
        }

        // Apart from FieldAdmit so the checks are written once in IL instead of once per field type; the JIT may still inline a copy into each Record<T>.
        private RecorderFieldAdmission FieldCheck(string label, in Parameters parameters, out ReaderNode recorded)
        {
            if (!IsInnermost())
            {
                Dbg.Err($"{node.GetContext()}: Attempted to read `{label}` through a Recorder outside the Record() call it was passed to; skipping it. A Recorder is valid only on the calling thread, until its Record() call returns, and not while a nested object's Record() is running.");
                recorded = null;
                return RecorderFieldAdmission.Refused;
            }

            if (asThis)
            {
                Dbg.Err($"{node.GetContext()}: Attempting to read a second field after a RecordAsThis call");
                recorded = null;
                return RecorderFieldAdmission.Refused;
            }

            if (parameters.asThis)
            {
                asThis = true;

                if (node.AllowAsThis && (node is ReaderNodeParseable || node is ReaderNodeLayers))
                {
                    recorded = null;
                    return RecorderFieldAdmission.AsThis;
                }
            }

            if (disallowShared && parameters.shared)
            {
                Dbg.Err($"{node.GetContext()}: Shared object used in a context that disallows shared objects (probably ConverterFactory<>.Create())");
            }

            recorded = node.GetChildNamed(label);
            if (recorded == null)
            {
                return RecorderFieldAdmission.Refused;
            }

            // Only labels whose element exists are ever added, so a repeat always gets this far.
            if (!seen.Add(label))
            {
                Dbg.Wrn($"{node.GetContext()}: Field `{label}` read multiple times");
            }

            return RecorderFieldAdmission.Child;
        }

        protected void FieldRead<T>(ref T value, ReaderNode recorded, in Parameters parameters)
        {
            // Avoid an explicit cast because that can cause null reference errors
            var result = recorded.ParseElement(typeof(T), value, readerGlobals, parameters.CreateSettings());
            if (result != null)
            {
                value = (T)result;
            }
            else
            {
                value = default;
            }
        }

        public override void Ignore(string label)
        {
            if (!IsInnermost())
            {
                Dbg.Err($"{node.GetContext()}: Attempted to ignore `{label}` through a Recorder outside the Record() call it was passed to; skipping it. A Recorder is valid only on the calling thread, until its Record() call returns, and not while a nested object's Record() is running.");
                return;
            }

            if (node.GetChildNamed(label) != null)
            {
                seen.Add(label);
            }
        }

        public override Direction Mode { get => Direction.Read; }
        public override Purpose Intent { get => node.Intent; }

        internal void ReportUnusedFields()
        {
            if (!trackUsage)
            {
                Dbg.Err($"{node.GetContext()}: Internal error, RecorderReader.ReportUnusedFields() called without trackUsage set");
                return;
            }

            if (asThis)
            {
                // field parsing deferred to our child anyway
                return;
            }

            // Each layer is reported on its own, so a warning points at the XML that caused it.
            if (node is ReaderNodeLayers layers)
            {
                foreach (var layer in layers.Layers)
                {
                    var names = layer.GetAllChildren();
                    ReportIgnoredText(layer, ReadFrom(names));
                    ReportUnusedElements(layer, names);
                }
            }
            else
            {
                // Only readers over XML track usage, and those hold a parseable node or layers of them.
                var single = (ReaderNodeParseable)node;
                ReportIgnoredText(single, seen.Count > 0);

                // Only names that exist are ever registered, so if "seen" is as long as the children, everything was read and the names aren't needed; a layer of several can't take this shortcut, since what it's seen may come from the other layers.
                if (seen.Count != single.GetChildCount())
                {
                    ReportUnusedElements(single, single.GetAllChildren());
                }
            }
        }

        // Asks HasText only when nothing was read, since it can scan every child.
        private void ReportIgnoredText(ReaderNodeParseable layer, bool readAny)
        {
            if (!readAny && layer.HasText())
            {
                Dbg.Err($"{layer.GetContext()}: XML contains text content which was ignored, and no fields were read from this object; the text will be ignored and the object will be left with default values.");
            }
        }

        private void ReportUnusedElements(ReaderNodeParseable layer, string[] names)
        {
            var unused = new HashSet<string>(names);
            unused.RemoveWhere(seen.Contains);

            if (unused.Count > 0)
            {
                Dbg.Wrn($"{layer.GetContext()}: Elements specified that don't exist on the object: {string.Join(", ", unused)}");
            }
        }

        // Whether any field was read from a layer of several, given its children's names; a field read from another layer doesn't count.
        private bool ReadFrom(string[] names)
        {
            foreach (var name in names)
            {
                if (seen.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
