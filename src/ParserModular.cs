namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;

    /// <summary>
    /// Handles all parsing and initialization of dec structures.
    ///
    /// Intended for moddable games; use Parser for non-moddable or prototype games.
    /// </summary>
    public class ParserModular
    {
        public class Module : IParser
        {
            internal string name;
            internal readonly List<ReaderFileDec> readers = new List<ReaderFileDec>();

            /// <summary>
            /// Pass a directory in for recursive processing.
            /// </summary>
            /// <remarks>
            /// This function will ignore dot-prefixed directory names and files, which are common for development tools to create.
            /// </remarks>
            /// <param name="directory">The directory to look for files in.</param>
            public void AddDirectory(string directory)
            {
                foreach (var file in Directory.GetFiles(directory, "*.xml"))
                {
                    if (!System.IO.Path.GetFileName(file).StartsWith("."))
                    {
                        AddFile(Parser.FileType.Xml, file);
                    }
                }

                foreach (var subdir in Directory.GetDirectories(directory))
                {
                    if (!System.IO.Path.GetFileName(subdir).StartsWith("."))
                    {
                        AddDirectory(subdir);
                    }
                }
            }

            /// <summary>
            /// Pass a file in for processing.
            /// </summary>
            /// <param name="stringName">A human-readable identifier useful for debugging. Generally, the name of the file that the string was read from. Not required; will be derived from filename automatically.</param>
            public void AddFile(Parser.FileType fileType, string filename, string identifier = null)
            {
                if (identifier == null)
                {
                    // This is imperfect, but good enough. People can pass their own identifier in if they want something clever.
                    identifier = Path.GetFileName(filename);
                }

                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    AddStream(fileType, fs, identifier);
                }
            }

            /// <summary>
            /// Pass a stream in for processing.
            /// </summary>
            /// <param name="identifier">A human-readable identifier useful for debugging. Generally, the name of the file that the stream was built from. Not required; will be derived from filename automatically</param>
            public void AddStream(Parser.FileType fileType, Stream stream, string identifier = "(unnamed)")
            {
                using (var reader = new StreamReader(stream))
                {
                    AddTextReader(fileType, reader, identifier);
                }
            }

            /// <summary>
            /// Pass a string in for processing.
            /// </summary>
            /// <param name="identifier">A human-readable identifier useful for debugging. Generally, the name of the file that the string was built from. Not required, but helpful.</param>
            public void AddString(Parser.FileType fileType, string contents, string identifier = "(unnamed)")
            {
                // This is a really easy error to make; we might as well handle it.
                if (contents.EndsWith(".xml"))
                {
                    Dbg.Err($"It looks like you've passed the filename `{contents}` to AddString instead of the actual XML file. Either use AddFile() or pass the file contents in.");
                }

                using (var reader = new StringReader(contents))
                {
                    AddTextReader(fileType, reader, identifier);
                }
            }

            private void AddTextReader(Parser.FileType fileType, TextReader textReader, string identifier = "(unnamed)")
            {
                if (s_Status != Status.Accumulating)
                {
                    Dbg.Err($"Adding data while while the world is in {s_Status} state; should be {Status.Accumulating} state");
                }

                string bakedIdentifier = (name == "core") ? identifier : $"{name}:{identifier}";

                ReaderFileDec reader;

                if (fileType == Parser.FileType.Xml)
                {
                    reader = ReaderFileDecXml.Create(textReader, bakedIdentifier);
                }
                else
                {
                    Dbg.Err($"{bakedIdentifier}: Only XML files are supported at this time");
                    return;
                }

                if (reader != null)
                {
                    readers.Add(reader);
                }

                // otherwise, error has already been printed
            }
        }

        // Global status
        private enum Status
        {
            Uninitialized,
            Accumulating,
            Processing,
            Distributing,
            Finalizing,
            Finished,
        }
        private static Status s_Status = Status.Uninitialized;

        // Data stored from initialization parameters
        private List<Type> staticReferences = new List<Type>();

        // Modules
        internal List<Module> modules = new List<Module>();

        // A list of types that can be inherited from
        private struct Parent
        {
            public ReaderNode node;
            public ReaderContext context;
            public string parent;
        }

        // A list of inheritance-based work that still has to be resolved
        private struct InheritanceJob
        {
            public Dec target;
            public ReaderNode node;
            public string parent;
        }

        // Used for static reference validation
        private static Action s_StaticReferenceHandler = null;

        /// <summary>
        /// Creates a Parser.
        /// </summary>
        public ParserModular()
        {
            if (s_Status != Status.Uninitialized)
            {
                Dbg.Err($"Parser created while the world is in {s_Status} state; should be {Status.Uninitialized} state");
            }
            s_Status = Status.Accumulating;

            bool unitTestMode = Config.TestParameters != null;

            {
                IEnumerable<Type> staticRefs;
                if (!unitTestMode)
                {
                    staticRefs = UtilReflection.GetAllUserTypes().Where(t => t.GetCustomAttribute<StaticReferencesAttribute>() != null);
                }
                else if (Config.TestParameters.explicitStaticRefs != null)
                {
                    staticRefs = Config.TestParameters.explicitStaticRefs;
                }
                else
                {
                    staticRefs = Enumerable.Empty<Type>();
                }

                foreach (var type in staticRefs)
                {
                    if (type.GetCustomAttribute<StaticReferencesAttribute>() == null)
                    {
                        Dbg.Err($"{type} is not tagged as StaticReferences");
                    }

                    if (!type.IsAbstract || !type.IsSealed)
                    {
                        Dbg.Err($"{type} is not static");
                    }

                    staticReferences.Add(type);
                }
            }

            Serialization.Initialize();
        }

        /// <summary>
        /// Creates and registers a new module with a given name.
        /// </summary>
        public Module CreateModule(string name)
        {
            var module = new Module();
            module.name = name;
            modules.Add(module);
            return module;
        }

        /// <summary>
        /// Finish all parsing.
        /// </summary>
        public void Finish()
        {
            System.GC.Collect();

            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                if (s_Status != Status.Accumulating)
                {
                    Dbg.Err($"Finishing while the world is in {s_Status} state; should be {Status.Accumulating} state");
                }
                s_Status = Status.Processing;

                // A list of inheritance-based work that still has to be resolved
                var inheritanceJobs = new List<InheritanceJob>();

                // A list of types that can be inherited from
                var potentialParents = new Dictionary<Tuple<Type, string>, Parent>();

                // List of work to be run during the Finish stage
                var finishWork = new List<Action>();

                var readerContext = new ReaderContext(false);

                // this is absolutely not the right way to handle multiple modules
                foreach (var reader in modules.SelectMany(module => module.readers))
                {
                    foreach (var readerDec in reader.ParseDecs())
                    {
                        // Register ourselves as an available parenting object
                        {
                            var identifier = Tuple.Create(readerDec.type.GetDecRootType(), readerDec.name);
                            if (potentialParents.ContainsKey(identifier))
                            {
                                Dbg.Err($"{readerDec.inputContext}: Dec [{identifier.Item1}:{identifier.Item2}] defined twice");
                            }
                            else
                            {
                                potentialParents[identifier] = new Parent { node = readerDec.node, context = readerContext, parent = readerDec.parent };
                            }
                        }

                        if (!readerDec.abstrct)
                        {
                            // Not an abstract dec instance, so create our instance
                            var decInstance = (Dec)readerDec.type.CreateInstanceSafe("dec", readerDec.node);

                            // Error reporting happens within CreateInstanceSafe; if we get null out, we just need to clean up elegantly
                            if (decInstance != null)
                            {
                                decInstance.DecName = readerDec.name;

                                Database.Register(decInstance);

                                if (readerDec.parent == null)
                                {
                                    // Non-parent objects are simple; we just handle them here in order to avoid unnecessary GC churn
                                    finishWork.Add(() => Serialization.ParseElement(new List<ReaderNode>() { readerDec.node }, readerDec.type, decInstance, readerContext, new Recorder.Context(), isRootDec: true));
                                }
                                else
                                {
                                    // Add an inheritance resolution job; we'll take care of this soon
                                    inheritanceJobs.Add(new InheritanceJob { target = decInstance, node = readerDec.node, parent = readerDec.parent });
                                }
                            }
                        }
                    }
                }

                // It's time to actually shove stuff into the database, so let's stop spitting out empty warnings.
                Database.SuppressEmptyWarning();

                // Resolve all our inheritance jobs
                foreach (var work in inheritanceJobs)
                {
                    // These are the nodes we need to analyze in order
                    var readerNodes = new List<ReaderNode>();

                    readerNodes.Add(work.node);

                    string currentDecName = work.target.DecName;
                    string parentDecName = work.parent;
                    while (parentDecName != null)
                    {
                        var parentData = potentialParents.TryGetValue(Tuple.Create(work.target.GetType().GetDecRootType(), parentDecName));

                        // This is a struct for the sake of performance, so parentData itself won't be null
                        // (wish I could just use ?. here)
                        if (parentData.node == null)
                        {
                            Dbg.Err($"{work.node.GetInputContext()}: Dec `{currentDecName}` is attempting to use parent `{parentDecName}`, but no such dec exists");

                            // Not much more we can do here.
                            break;
                        }

                        readerNodes.Add(parentData.node);

                        currentDecName = parentDecName;
                        parentDecName = parentData.parent;
                    }

                    readerNodes.Reverse();

                    // do this all in one batch
                    finishWork.Add(() =>
                    {
                        Serialization.ParseElement(readerNodes, work.target.GetType(), work.target, readerContext, new Recorder.Context(), isRootDec: true);
                    });
                }

                foreach (var work in finishWork)
                {
                    work();
                }

                if (s_Status != Status.Processing)
                {
                    Dbg.Err($"Distributing while the world is in {s_Status} state; should be {Status.Processing} state");
                }
                s_Status = Status.Distributing;

                foreach (var stat in staticReferences)
                {
                    if (!StaticReferencesAttribute.StaticReferencesFilled.Contains(stat))
                    {
                        s_StaticReferenceHandler = () =>
                        {
                            s_StaticReferenceHandler = null;
                            StaticReferencesAttribute.StaticReferencesFilled.Add(stat);
                        };
                    }

                    bool touched = false;
                    foreach (var field in stat.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
                    {
                        var dec = Database.Get(field.FieldType, field.Name);
                        if (dec == null)
                        {
                            Dbg.Err($"Static reference class {stat} has member `{field.FieldType} {field.Name}` that does not correspond to any loaded Dec");
                            field.SetValue(null, null); // this is unnecessary, but it does kick the static constructor just in case we wouldn't do it otherwise
                        }
                        else if (!field.FieldType.IsAssignableFrom(dec.GetType()))
                        {
                            Dbg.Err($"Static reference class {stat} has member `{field.FieldType} {field.Name}` that is not compatible with actual {dec.GetType()} {dec}");
                            field.SetValue(null, null); // this is unnecessary, but it does kick the static constructor just in case we wouldn't do it otherwise
                        }
                        else
                        {
                            field.SetValue(null, dec);
                        }

                        touched = true;
                    }

                    if (s_StaticReferenceHandler != null)
                    {
                        if (touched)
                        {
                            // Otherwise we shouldn't even expect this to have been registered, but at least there's literally no fields in it so it doesn't matter
                            Dbg.Err($"Failed to properly register {stat}; you may be missing a call to Dec.StaticReferencesAttribute.Initialized() in its static constructor, or the class may already have been initialized elsewhere (this should have thrown an error)");
                        }
                        
                        s_StaticReferenceHandler = null;
                    }
                }

                if (s_Status != Status.Distributing)
                {
                    Dbg.Err($"Finalizing while the world is in {s_Status} state; should be {Status.Distributing} state");
                }
                s_Status = Status.Finalizing;

                foreach (var dec in Database.List)
                {
                    try
                    {
                        dec.ConfigErrors(err => Dbg.Err($"{dec}: {err}"));
                    }
                    catch (Exception e)
                    {
                        Dbg.Ex(e);
                    }
                }

                foreach (var dec in Database.List)
                {
                    try
                    {
                        dec.PostLoad(err => Dbg.Err($"{dec}: {err}"));
                    }
                    catch (Exception e)
                    {
                        Dbg.Ex(e);
                    }
                }

                if (s_Status != Status.Finalizing)
                {
                    Dbg.Err($"Completing while the world is in {s_Status} state; should be {Status.Finalizing} state");
                }
                s_Status = Status.Finished;
            }
        }

        internal static void Clear()
        {
            if (s_Status != Status.Finished && s_Status != Status.Uninitialized)
            {
                Dbg.Err($"Clearing while the world is in {s_Status} state; should be {Status.Uninitialized} state or {Status.Finished} state");
            }
            s_Status = Status.Uninitialized;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void StaticReferencesInitialized()
        {
            if (s_StaticReferenceHandler != null)
            {
                s_StaticReferenceHandler();
                return;
            }

            Dbg.Err($"Initializing static reference class at an inappropriate time - this probably means you accessed a static reference class before it was ready");
        }
    }
}