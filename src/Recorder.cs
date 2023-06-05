namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

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

    // This exists solely to ensure I always remember to add the right functions both to Parameter and Recorder.
    internal interface IRecorder
    {
        void Record<T>(ref T value, string label);
        void RecordAsThis<T>(ref T value);

        Recorder.Parameters WithFactory(Dictionary<Type, Func<Type, object>> factories);
    }

    /// <summary>
    /// Main class for the serialization/deserialization system.
    /// </summary>
    /// <remarks>
    /// Recorder is used to call the main functions for serialization/deserialization. This includes both the static initiation functions (Read, Write) and the per-element status functions.
    ///
    /// To start serializing or deserializing an object, see Recorder.Read and Recorder.Write.
    /// </remarks>
    public abstract class Recorder : IRecorder
    {
        public struct Parameters : IRecorder
        {
            internal Recorder recorder;

            internal bool asThis;
            internal bool shared;

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

            internal Context CreateContext()
            {
                return new Context() { factories = factories, shared = shared ? Context.Shared.Allow : Context.Shared.Deny };
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

            public Context CreateChild()
            {
                Context rv = this;
                if (rv.shared == Shared.Allow)
                {
                    // Downgrade this in case we have something like a List<int>; we don't want to spit out warnings about int not being sharable
                    rv.shared = Shared.Flexible;
                }
                return rv;
            }
        }

        /// <summary>
        /// Serialize or deserialize a member of a class.
        /// </summary>
        /// <remarks>
        /// This function serializes or deserializes a class member. Call it with a reference to the member and a label for the member (usually the member's name.)
        ///
        /// In most cases, you don't need to do anything different for read vs. write; this function will figure out the details and do the right thing.
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
        /// Returns a fully-formed XML document starting at an object.
        /// </summary>
        public static string Write<T>(T target, bool pretty = true)
        {
            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterXmlRecord();

                Serialization.ComposeElement(writerContext.StartData(typeof(T)), target, typeof(T));

                return writerContext.Finish(pretty);
            }
        }

        /// <summary>
        /// Returns C# validation code starting at an option.
        /// </summary>
        public static string WriteValidation<T>(T target)
        {
            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterValidationRecord();

                Serialization.ComposeElement(writerContext.StartData(), target, typeof(T));

                return writerContext.Finish();
            }
        }

        /// <summary>
        /// Parses the output of Write, generating an object and all its related serialized data.
        /// </summary>
        public static T Read<T>(string input, string stringName = "input")
        {
            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                XDocument doc;

                try
                {
                    doc = XDocument.Parse(input, LoadOptions.SetLineInfo);
                }
                catch (System.Xml.XmlException e)
                {
                    Dbg.Ex(e);
                    return default(T);
                }

                if (doc.Elements().Count() > 1)
                {
                    // This isn't testable, unfortunately; XDocument doesn't even support multiple root elements.
                    Dbg.Err($"{stringName}: Found {doc.Elements().Count()} root elements instead of the expected 1");
                }

                var record = doc.Elements().First();
                if (record.Name.LocalName != "Record")
                {
                    Dbg.Wrn($"{stringName}:{record.LineNumber()}: Found root element with name `{record.Name.LocalName}` when it should be `Record`");
                }

                var recordFormatVersion = record.ElementNamed("recordFormatVersion");
                if (recordFormatVersion == null)
                {
                    Dbg.Err($"{stringName}:{record.LineNumber()}: Missing record format version, assuming the data is up-to-date");
                }
                else if (recordFormatVersion.GetText() != "1")
                {
                    Dbg.Err($"{stringName}:{recordFormatVersion.LineNumber()}: Unknown record format version {recordFormatVersion.GetText()}, expected 1 or earlier");

                    // I would rather not guess about this
                    return default(T);
                }

                var refs = record.ElementNamed("refs");

                var readerContext = new ReaderContext(stringName, true);
                var refDict = new Dictionary<string, object>();

                if (refs != null)
                {
                    // First, we need to make the instances for all the references, so they can be crosslinked appropriately
                    // We'll be doing a second parse to parse *many* of these, but not all
                    var furtherParsing = new List<Action>();
                    foreach (var reference in refs.Elements())
                    {
                        if (reference.Name.LocalName != "Ref")
                        {
                            Dbg.Wrn($"{stringName}:{reference.LineNumber()}: Reference element should be named 'Ref'");
                        }

                        var id = reference.Attribute("id")?.Value;
                        if (id == null)
                        {
                            Dbg.Err($"{stringName}:{reference.LineNumber()}: Missing reference ID");
                            continue;
                        }

                        // Further steps don't know how to deal with this, so we just strip it
                        reference.Attribute("id").Remove();

                        var className = reference.Attribute("class")?.Value;
                        if (className == null)
                        {
                            Dbg.Err($"{stringName}:{reference.LineNumber()}: Missing reference class name");
                            continue;
                        }

                        var possibleType = (Type)Serialization.ParseString(className, typeof(Type), null, stringName, reference.LineNumber());
                        if (possibleType.IsValueType)
                        {
                            Dbg.Err($"{stringName}:{reference.LineNumber()}: Reference assigned type {possibleType}, which is a value type");
                            continue;
                        }

                        object refInstance = null;
                        if (Serialization.Converters.TryGetValue(possibleType, out var converter))
                        {
                            if (converter is ConverterString converterString)
                            {
                                refInstance = converterString.ReadObj(reference.GetText(), new InputContext() { filename = stringName, handle = reference });

                                // this does not need to be queued for parsing
                            }
                            else if (converter is ConverterRecord converterRecord)
                            {
                                // create the basic object
                                refInstance = possibleType.CreateInstanceSafe("object", () => $"{stringName}:{reference.LineNumber()}", children: reference.Elements().Count());

                                // the next parse step
                                furtherParsing.Add(() => converterRecord.RecordObj(refInstance, new RecorderReader(reference, readerContext)));
                            }
                            else if (converter is ConverterFactory converterFactory)
                            {
                                // create the basic object
                                refInstance = converterFactory.CreateObj(new RecorderReader(reference, readerContext, disallowShared: true));

                                // the next parse step
                                furtherParsing.Add(() => converterFactory.ReadObj(refInstance, new RecorderReader(reference, readerContext)));
                            }
                            else
                            {
                                Dbg.Err($"Somehow ended up with an unsupported converter {converter.GetType()}");
                            }
                        }
                        else
                        {
                            // Create a stub so other things can reference it later
                            refInstance = possibleType.CreateInstanceSafe("object", () => $"{stringName}:{reference.LineNumber()}", children: reference.Elements().Count());

                            // Whoops, failed to construct somehow. CreateInstanceSafe() has already made a report
                            if (refInstance != null)
                            {
                                furtherParsing.Add(() =>
                                {
                                    // Do our actual parsing
                                    var refInstanceOutput = Serialization.ParseElement(reference, refInstance.GetType(), refInstance, readerContext, new Recorder.Context(), hasReferenceId: true);

                                    if (refInstance != refInstanceOutput)
                                    {
                                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Something really bizarre has happened and we got the wrong object back. Things are probably irrevocably broken. Please report this as a bug in Dec.");
                                    }
                                });
                            }
                        }

                        // If this is null, CreateInstanceSafe has done the error reporting, but we still don't want it in the array because it will break stuff downstream
                        if (refInstance != null)
                        {
                            refDict[id] = refInstance;
                        }
                    }

                    // link up the ref dict
                    readerContext.refs = refDict;

                    // finish up our second-stage ref parsing
                    foreach (var action in furtherParsing)
                    {
                        action();
                    }
                }
                else
                {
                    // empty ref dict, sure
                    readerContext.refs = new Dictionary<string, object>();
                }

                var data = record.ElementNamed("data");
                if (data == null)
                {
                    Dbg.Err($"{stringName}:{record.LineNumber()}: No data element provided. This is not very recoverable.");

                    return default(T);
                }

                // And now, we can finally parse our actual root element!
                // (which accounts for a tiny percentage of things that need to be parsed)
                return (T)Serialization.ParseElement(data, typeof(T), null, readerContext, new Recorder.Context() { shared = Context.Shared.Flexible });
            }
        }
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

                Serialization.ComposeElement(node, value, typeof(T));

                return;
            }

            if (fields.Contains(label))
            {
                Dbg.Err($"Field `{label}` written multiple times");
                return;
            }

            fields.Add(label);

            Serialization.ComposeElement(node.CreateChild(label, parameters.CreateContext()), value, typeof(T));
        }

        public override Direction Mode { get => Direction.Write; }
    }

    internal class ReaderContext
    {
        public string sourceName;
        public Dictionary<string, object> refs;
        public bool recorderMode;

        public ReaderContext(string sourceName, bool recorderMode)
        {
            this.sourceName = sourceName;
            this.recorderMode = recorderMode;
        }
    }

    internal class RecorderReader : Recorder
    {
        private bool asThis = false;
        private readonly XElement element;
        private readonly ReaderContext context;
        private bool disallowShared;

        public string SourceName { get => context.sourceName; }
        public int SourceLine { get => element.LineNumber(); }

        internal RecorderReader(XElement element, ReaderContext context, bool disallowShared = false)
        {
            this.element = element;
            this.context = context;
            this.disallowShared = disallowShared;
        }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            if (asThis)
            {
                Dbg.Err($"{SourceName}:{SourceLine}: Attempting to read a second field after a RecordAsThis call");
                return;
            }

            if (parameters.asThis)
            {
                asThis = true;

                // Explicit cast here because we want an error if we have the wrong type!
                value = (T)Serialization.ParseElement(element, typeof(T), value, context, parameters.CreateContext());

                return;
            }

            if (disallowShared && parameters.shared)
            {
                Dbg.Err($"{SourceName}:{SourceLine}: Shared object used in a context that disallows shared objects (probably ConverterFactory<>.Create())");
            }

            var recorded = element.ElementNamed(label);
            if (recorded == null)
            {
                return;
            }

            // Explicit cast here because we want an error if we have the wrong type!
            value = (T)Serialization.ParseElement(recorded, typeof(T), value, context, parameters.CreateContext());
        }

        public override Direction Mode { get => Direction.Read; }
    }
}
