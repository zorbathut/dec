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
                
                parameters.factories = factories;

                return parameters;
            }

            internal Context CreateContext()
            {
                return new Context() { factories = factories };
            }
        }

        // This is used for passing data to the Parse and Compose functions.
        internal struct Context
        {
            public Dictionary<Type, Func<Type, object>> factories;

            public bool Referenceable
            {
                get => factories == null;
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
        /// See https://github.com/zorbathut/dec/blob/master/example/loaf/RollTable.cs for an example.
        ///
        /// In most cases, you don't need to do anything different for read vs. write; this function will figure out the details and do the right thing.
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
        /// </remarks>
        public Parameters WithFactory(Dictionary<Type, Func<Type, object>> factories)
        {
            return new Parameters() { recorder = this, factories = factories };
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
            var writerContext = new WriterXmlRecord();

            Serialization.ComposeElement(writerContext.StartData(), target, typeof(T), new Recorder.Context());

            return writerContext.Finish(pretty);
        }

        /// <summary>
        /// Returns C# validation code starting at an option.
        /// </summary>
        public static string WriteValidation<T>(T target)
        {
            var writerContext = new WriterValidationRecord();

            Serialization.ComposeElement(writerContext.StartData(), target, typeof(T), new Recorder.Context());

            return writerContext.Finish();
        }

        /// <summary>
        /// Parses the output of Write, generating an object and all its related serialized data.
        /// </summary>
        public static T Read<T>(string input, string stringName = "input")
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
                Dbg.Wrn($"{stringName}:{record.LineNumber()}: Found root element with name \"{record.Name.LocalName}\" when it should be \"Record\"");
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

            if (refs != null)
            {
                // First, we need to make the instances for all the references, so they can be crosslinked appropriately
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

                    // Create a stub so other things can reference it later
                    readerContext.refs[id] = possibleType.CreateInstanceSafe("object", () => $"{stringName}:{reference.LineNumber()}");
                    // Might be null; that's okay, CreateInstanceSafe has done the error reporting
                }

                // Now that all the refs exist, we can run through them again and actually parse them
                foreach (var reference in refs.Elements())
                {
                    var id = reference.Attribute("id")?.Value;
                    if (id == null)
                    {
                        // Just skip it, we don't have anything useful we can do here
                        continue;
                    }

                    // The serialization routines don't know how to deal with this, so we'll remove it now
                    reference.Attribute("id").Remove();

                    var refInstance = readerContext.refs.TryGetValue(id);
                    if (refInstance == null)
                    {
                        // We failed to parse this for some reason, so just skip it now
                        continue;
                    }

                    // Do our actual parsing
                    var refInstanceOutput = Serialization.ParseElement(reference, refInstance.GetType(), refInstance, readerContext, new Recorder.Context(), hasReferenceId: true);

                    if (refInstance != refInstanceOutput)
                    {
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Something really bizarre has happened and we got the wrong object back. Things are probably irrevocably broken. Please report this as a bug in Dec.");
                        continue;
                    }
                }
            }

            var data = record.ElementNamed("data");
            if (data == null)
            {
                Dbg.Err($"{stringName}:{record.LineNumber()}: No data element provided. This is not very recoverable.");

                return default(T);
            }

            // And now, we can finally parse our actual root element!
            // (which accounts for a tiny percentage of things that need to be parsed)
            return (T)Serialization.ParseElement(data, typeof(T), null, readerContext, new Recorder.Context());
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

                Serialization.ComposeElement(node, value, typeof(T), parameters.CreateContext());

                return;
            }

            if (fields.Contains(label))
            {
                Dbg.Err($"Field '{label}' written multiple times");
                return;
            }

            fields.Add(label);

            Serialization.ComposeElement(node.CreateChild(label, parameters.CreateContext()), value, typeof(T), parameters.CreateContext());
        }

        public override Direction Mode { get => Direction.Write; }
    }

    internal class ReaderContext
    {
        public string sourceName;
        public Dictionary<string, object> refs;

        public bool RecorderMode { get => refs != null; }

        public ReaderContext(string sourceName, bool withRefs)
        {
            this.sourceName = sourceName;

            if (withRefs)
            {
                refs = new Dictionary<string, object>();
            }
        }
    }

    internal class RecorderReader : Recorder
    {
        private bool asThis = false;
        private readonly XElement element;
        private readonly ReaderContext context;

        public string SourceName { get => context.sourceName; }
        public int SourceLine { get => element.LineNumber(); }

        internal RecorderReader(XElement element, ReaderContext context)
        {
            this.element = element;
            this.context = context;
        }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            if (asThis)
            {
                Dbg.Err($"Attempting to read a second field after a RecordAsThis call");
                return;
            }

            if (parameters.asThis)
            {
                asThis = true;

                // Explicit cast here because we want an error if we have the wrong type!
                value = (T)Serialization.ParseElement(element, typeof(T), value, context, parameters.CreateContext());

                return;
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
