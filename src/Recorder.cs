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
    /// </remarks>
    public abstract partial class Recorder : IRecorder
    {
        public interface IUserSettings { }

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
                    Dbg.Err("Recorder.WithFactory() called on Recorder.Parameters that already has factories. This is undefined results; currently replacing the old factory dictionary with the new one.");
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

            internal Context CreateContext()
            {
                return new Context() { factories = factories, shared = shared ? Context.Shared.Allow : Context.Shared.Deny, bespoke_keytypedict = bespoke_keytypedict };
            }
        }

        // This is used for passing data to the Parse and Compose functions.
        internal struct Context
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

            public Context CreateChild()
            {
                Context rv = this;
                if (rv.shared == Shared.Allow)
                {
                    // Downgrade this in case we have something like a List<int>; we don't want to spit out warnings about int not being sharable
                    rv.shared = Shared.Flexible;
                }

                rv.bespoke_keytypedict = false;
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
    }

    internal class RecorderWriter : Recorder
    {
        private bool asThis = false;
        private readonly HashSet<string> fields = new HashSet<string>();
        private readonly WriterNode node;

        internal RecorderWriter(WriterNode node)
        {
            this.node = node;
        }

        public override IUserSettings UserSettings { get => node.UserSettings; }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            if (asThis)
            {
                Dbg.Err($"Attempting to write a second field after a RecordAsThis call");
                return;
            }

            if (parameters.asThis)
            {
                if (fields.Count > 0)
                {
                    Dbg.Err($"Attempting to make a RecordAsThis call after writing a field");
                    return;
                }

                asThis = true;

                if (!node.FlagAsThis())
                {
                    // just give up and skip it
                    return;
                }

                if (node.AllowAsThis)
                {
                    Serialization.ComposeElement(node, value, typeof(T), asThis: true);

                    return;
                }
            }

            if (fields.Contains(label))
            {
                Dbg.Err($"Field `{label}` written multiple times");
                return;
            }

            fields.Add(label);

            Serialization.ComposeElement(node.CreateRecorderChild(label, parameters.CreateContext()), value, typeof(T));
        }

        public override Direction Mode { get => Direction.Write; }
    }

    internal struct ReaderContext
    {
        public Dictionary<string, object> refs;
        public bool allowRefs;
        public bool allowReflection;
    }

    internal class RecorderReader : Recorder
    {
        private bool asThis = false;
        private readonly ReaderNode node;
        private ReaderContext readerContext;
        private bool disallowShared;
        private HashSet<string> seen;

        internal RecorderReader(ReaderNode node, ReaderContext context, bool disallowShared = false, bool trackUsage = false)
        {
            this.node = node;
            this.readerContext = context;
            this.disallowShared = disallowShared;

            if (trackUsage)
            {
                seen = new HashSet<string>();
            }
        }
        internal void AllowShared(ReaderContext newContext)
        {
            if (!disallowShared)
            {
                Dbg.Err($"{node.GetInputContext()}: Internal error, RecorderReader.AllowShared() called on a RecorderReader that does not disallow shared objects");
            }

            this.readerContext = newContext;
            disallowShared = false;
        }

        public override IUserSettings UserSettings { get => node.UserSettings; }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            if (asThis)
            {
                Dbg.Err($"{node.GetInputContext()}: Attempting to read a second field after a RecordAsThis call");
                return;
            }

            if (parameters.asThis)
            {
                asThis = true;

                if (node.AllowAsThis && (node is ReaderNodeParseable nodeParseable))
                {
                    // Explicit cast here because we want an error if we have the wrong type!
                    value = (T)Serialization.ParseElement(new List<ReaderNodeParseable>() { nodeParseable }, typeof(T), value, readerContext, parameters.CreateContext(), asThis: true);

                    return;
                }
            }

            if (disallowShared && parameters.shared)
            {
                Dbg.Err($"{node.GetInputContext()}: Shared object used in a context that disallows shared objects (probably ConverterFactory<>.Create())");
            }

            var recorded = node.GetChildNamed(label);
            if (recorded == null)
            {
                return;
            }

            seen?.Add(label);

            // Explicit cast here because we want an error if we have the wrong type!
            value = (T)recorded.ParseElement(typeof(T), value, readerContext, parameters.CreateContext());
        }

        public override void Ignore(string label)
        {
            seen?.Add(label);
        }

        public override Direction Mode { get => Direction.Read; }

        internal void ReportUnusedFields()
        {
            if (seen == null)
            {
                Dbg.Err($"{node.GetInputContext()}: Internal error, RecorderReader.HasUnusedFields() called without trackUsage set");
                return;
            }

            if (asThis)
            {
                // field parsing deferred to our child anyway
                return;
            }

            var allChildren = node.GetAllChildren();
            if (seen.Count == allChildren.Length)
            {
                // we only register things that existed
                // so if "seen" is the same length as "all", then we've seen everything
                return;
            }

            var unused = new HashSet<string>(allChildren);
            unused.ExceptWith(seen);

            Dbg.Wrn($"{node.GetInputContext()}: Unused fields: {string.Join(", ", unused)}");
        }
    }
}
