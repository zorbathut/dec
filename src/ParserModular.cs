using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dec
{
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
            internal Recorder.IUserSettings userSettings;

            internal Module(Recorder.IUserSettings userSettings)
            {
                this.userSettings = userSettings;
            }

            /// <summary>
            /// Pass a directory in for recursive processing.
            /// </summary>
            /// <remarks>
            /// This function will ignore dot-prefixed directory names and files, which are common for development tools to create.
            /// </remarks>
            /// <param name="directory">The directory to look for files in.</param>
            public void AddDirectory(string directory)
            {
                if (directory.IsNullOrEmpty())
                {
                    Dbg.Err("Attempted to add a null or empty directory to the parser; this is probably wrong");
                    return;
                }

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
                if (filename.IsNullOrEmpty())
                {
                    Dbg.Err("Attempted to add a null or empty filename to the parser; this is probably wrong");
                    return;
                }

                if (identifier == null)
                {
                    // This is imperfect, but good enough. People can pass their own identifier in if they want something clever.
                    identifier = System.IO.Path.GetFileName(filename);
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
                if (stream == null)
                {
                    Dbg.Err("Attempted to add a null stream to the parser; this is probably wrong");
                    return;
                }

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
                if (contents.IsNullOrEmpty())
                {
                    Dbg.Err("Attempted to add a null or empty string to the parser; this is probably wrong");
                    return;
                }

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
                    Dbg.Err($"Adding data while the world is in {s_Status} state; should be {Status.Accumulating} state");
                }

                string bakedIdentifier = (name == "core") ? identifier : $"{name}:{identifier}";

                ReaderFileDec reader;

                if (fileType == Parser.FileType.Xml)
                {
                    reader = ReaderFileDecXml.Create(textReader, bakedIdentifier, userSettings);
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
        private List<Setup.StaticSetupMethod> staticSetupMethods = new List<Setup.StaticSetupMethod>();
        private Recorder.IUserSettings userSettings;

        // Modules
        internal List<Module> modules = new List<Module>();

        // Used for static reference validation
        private static Action s_StaticReferenceHandler = null;

        /// <summary>
        /// Creates a Parser.
        /// </summary>
        public ParserModular(Recorder.IUserSettings userSettings = null)
        {
            this.userSettings = userSettings;

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

            {
                // Scan for static [Dec.Setup] functions, and for setup attributes misplaced on structs. Instance setup functions are not scanned; they're discovered lazily as instances of their types are parsed.
                IEnumerable<Type> setupScanTypes;
                if (!unitTestMode)
                {
                    setupScanTypes = UtilReflection.GetAllUserTypes();
                }
                else if (Config.TestParameters.explicitSetupScanTypes != null)
                {
                    setupScanTypes = Config.TestParameters.explicitSetupScanTypes;
                }
                else
                {
                    setupScanTypes = Enumerable.Empty<Type>();
                }

                foreach (var type in setupScanTypes)
                {
                    if (type.IsValueType)
                    {
                        if (type.IsEnum || type.IsPrimitive)
                        {
                            continue;
                        }

                        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        {
                            if (method.GetCustomAttribute<SetupAttribute>(inherit: false) != null || method.GetCustomAttributes<SetupAfterAttribute>(inherit: false).Any() || method.GetCustomAttributes<SetupBeforeAttribute>(inherit: false).Any())
                            {
                                string rationale = method.IsStatic ? "" : "; struct instances are copied during parsing and any mutations would be lost";
                                Dbg.Err($"{type}.{method.Name} is tagged as a setup function, but setup functions are not supported on structs{rationale}");
                            }
                        }

                        continue;
                    }

                    if (type.IsInterface)
                    {
                        // Interfaces can't hold static setup functions, but their contract declarations can be malformed; running the contract scan here surfaces those declaration bugs even for interfaces nothing implements.
                        UtilReflection.GetDeclaredInterfaceContract(type);
                        continue;
                    }

                    if (!type.IsClass)
                    {
                        continue;
                    }

                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        var setupAttribute = method.GetCustomAttribute<SetupAttribute>(inherit: false);
                        if (setupAttribute == null)
                        {
                            if (method.GetCustomAttributes<SetupAfterAttribute>(inherit: false).Any() || method.GetCustomAttributes<SetupBeforeAttribute>(inherit: false).Any())
                            {
                                Dbg.Err($"{type}.{method.Name} has a [Dec.SetupAfter] or [Dec.SetupBefore] attribute but no [Dec.Setup]; the ordering constraint has no effect");
                            }

                            continue;
                        }

                        if (type.IsGenericTypeDefinition)
                        {
                            Dbg.Err($"{type}.{method.Name} cannot be a setup function because {type} is a generic type definition");
                            continue;
                        }

                        if (!UtilReflection.ValidateSetupSignature(method))
                        {
                            continue;
                        }

                        staticSetupMethods.Add(new Setup.StaticSetupMethod { method = method, attribute = setupAttribute });
                    }
                }
            }

            Serialization.Initialize();
        }

        /// <summary>
        /// Creates and registers a new module with a given name.
        /// </summary>
        public Module CreateModule(string name)
        {
            var module = new Module(userSettings);
            module.name = name;

            if (modules.Any(mod => mod.name == name))
            {
                Dbg.Err($"Created duplicate module {name}");
            }

            modules.Add(module);
            return module;
        }

        /// <summary>
        /// Finish all parsing and run Dec.Setup functions.
        /// </summary>
        public void Finish()
        {
            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                if (s_Status != Status.Accumulating)
                {
                    Dbg.Err($"Finishing while the world is in {s_Status} state; should be {Status.Accumulating} state");
                }
                s_Status = Status.Processing;

                var setupCollection = new Setup.Collection(excludeDatabaseOwned: false);
                var readerContext = new ReaderGlobals() { allowReflection = true, allowRefs = false, writeDecPaths = true, setupCollection = setupCollection };

                // Collate reader decs
                var registeredDecs = new Dictionary<(Type, string), List<ReaderFileDec.ReaderDec>>();
                foreach (var module in modules)
                {
                    var seenDecs = new Dictionary<(Type, string), Context>();
                    foreach (var reader in module.readers)
                    {
                        foreach (var readerDec in reader.ParseDecs())
                        {
                            var id = (readerDec.type.GetDecRootType(), readerDec.name);

                            if (seenDecs.TryGetValue(id, out var collidingDec))
                            {
                                Dbg.Err($"{collidingDec} / {readerDec.context}: Dec [{id.Item1}:{id.name}] defined twice");

                                // If the already-parsed one is abstract, we throw it away and go with the non-abstract one, because it's arguably more likely to be the one the user wants.
                                if (!(registeredDecs[id].Select(dec => dec.abstrct).LastOrDefault(abstrct => abstrct.HasValue) ?? false))
                                {
                                    continue;
                                }

                                registeredDecs.Remove(id);
                            }

                            seenDecs[id] = readerDec.context;

                            if (!registeredDecs.TryGetValue(id, out var list))
                            {
                                list = new List<ReaderFileDec.ReaderDec>();
                                registeredDecs[id] = list;
                            }
                            list.Add(readerDec);
                        }
                    }
                }

                // Compile reader decs into Order assemblies
                var registeredDecOrders = new Dictionary<(Type, string), List<ReaderFileDec.ReaderDec>>();
                foreach (var seenDec in registeredDecs)
                {
                    var orders = Serialization.CompileDecOrders(seenDec.Value);

                    // If we have no orders, this probably ended up deleted.
                    if (orders.Count > 0)
                    {
                        registeredDecOrders[seenDec.Key] = orders;
                    }
                }

                // Instantiate all decs
                var toParseDecOrders = new Dictionary<(Type, string), List<ReaderFileDec.ReaderDec>>();
                foreach (var (id, orders) in registeredDecOrders)
                {
                    // It's currently sort of unclear how we should be deriving the type.
                    // I'm choosing, for now, to go with "the most derived type in the list, assuming all types are in the same inheritance sequence".
                    // Thankfully this isn't too hard to do.
                    var typeDeterminor = orders[0];
                    bool abstrct = typeDeterminor.abstrct ?? false;

                    foreach (var order in orders.Skip(1))
                    {
                        // Since we're iterating over this anyway, yank the abstract updates out as we go.
                        if (order.abstrct.HasValue)
                        {
                            abstrct = order.abstrct.Value;
                        }

                        if (order.type == typeDeterminor.type)
                        {
                            // fast case
                            continue;
                        }

                        if (order.type.IsSubclassOf(typeDeterminor.type))
                        {
                            typeDeterminor = order;
                            continue;
                        }

                        if (typeDeterminor.type.IsSubclassOf(order.type))
                        {
                            continue;
                        }

                        // oops, they're not subclasses of each other
                        Dbg.Err($"{typeDeterminor.context} / {order.context}: Modded dec with tree-identifier [{id.Item1}:{id.Item2}] has conflicting types without a simple subclass relationship ({typeDeterminor.type}/{order.type}); deferring to {order.type}");
                        typeDeterminor = order;
                    }

                    // We don't actually want an instance of this.
                    if (abstrct)
                    {
                        continue;
                    }

                    // Not an abstract dec instance, so create our instance
                    // also, this invocation of NodeFactory is unnecessarily slow and should be fixed at some point
                    var decInstance = (Dec)typeDeterminor.type.CreateInstanceSafe("dec", typeDeterminor.nodeFactory(new PathDec(id.Item1, id.Item2)));

                    // Error reporting happens within CreateInstanceSafe; if we get null out, we just need to clean up elegantly
                    if (decInstance != null)
                    {
                        decInstance.DecName = id.Item2;

                        Database.Register(decInstance);

                        // create a new map that filters out abstract objects and failed instantiations
                        toParseDecOrders.Add(id, orders);
                    }
                }

                // It's time to actually pull references out of the database, so let's stop spitting out empty warnings.
                Database.SuppressEmptyWarning();

                foreach (var (id, orders) in toParseDecOrders)
                {
                    // Accumulate our orders
                    var completeOrders = orders;

                    var currentOrder = orders;
                    while (true)
                    {
                        // See if we have a parent
                        var decWithParent = currentOrder.Where(dec => dec.parent != null).LastOrDefault();
                        if (decWithParent.parent == null || decWithParent.parent == "")
                        {
                            break;
                        }

                        var parentId = (id.Item1, decWithParent.parent);
                        if (!registeredDecOrders.TryGetValue(parentId, out var parentDec))
                        {
                            Dbg.Err($"{decWithParent.context}: Dec [{decWithParent.type}:{id.Item2}] is attempting to use parent `[{parentId.Item1}:{parentId.parent}]`, but no such dec exists");
                            // guess we'll just try to build it from nothing
                            break;
                        }

                        completeOrders.InsertRange(0, parentDec);

                        currentOrder = parentDec;
                    }

                    var generatedOrders = completeOrders.Select(order => order.nodeFactory(new PathDec(id.Item1, id.Item2))).ToList();

                    var targetDec = Database.Get(id.Item1, id.Item2);
                    Serialization.ParseElement(generatedOrders, targetDec.GetType(), targetDec, readerContext, new Recorder.Settings(), isRootDec: true, ordersOverride: generatedOrders.Select(order => (Serialization.ParseCommand.Patch, node: order)).ToList());
                }

                // Validate CompatDecLookup now that all decs are registered
                Database.ValidateCompatDecLookup();

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
                        if (field.IsInitOnly)
                        {
                            Dbg.Err($"Static reference class {stat} has readonly member `{field.FieldType} {field.Name}`; static reference fields cannot be readonly");
                            touched = true;
                            continue;
                        }

                        if (!typeof(Dec).IsAssignableFrom(field.FieldType))
                        {
                            Dbg.Err($"Static reference class {stat} has member `{field.FieldType} {field.Name}` that is not a Dec type");
                            touched = true;
                            continue;
                        }

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
                            Dbg.Err($"Failed to properly register {stat}; you may be missing a call to Dec.StaticReferencesAttribute.Initialized() in its static constructor, or the class may already have been initialized elsewhere (this should have registered or thrown an error, go look for it!)");
                        }

                        s_StaticReferenceHandler = null;
                    }
                }


                if (s_Status != Status.Distributing)
                {
                    Dbg.Err($"Finalizing while the world is in {s_Status} state; should be {Status.Distributing} state");
                }
                s_Status = Status.Finalizing;

                // figure out our config/postload order; first we just run over all the Decs and collate classes
                var decTypes = new Dictionary<Type, List<Dec>>();
                foreach (var dec in Database.List)
                {
                    var list = decTypes.TryGetValue(dec.GetType());
                    if (list == null)
                    {
                        list = new List<Dec>();
                        decTypes[dec.GetType()] = list;
                    }

                    list.Add(dec);
                }

                Setup.ExecuteParser(staticSetupMethods, decTypes, setupCollection);

                // Invert the dec path lookup tables; these were presumably filled out during PostLoad
                Database.DecPathResolveDatabase();

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

        internal static void StaticReferencesInitialized()
        {
            if (s_StaticReferenceHandler != null)
            {
                s_StaticReferenceHandler();
                return;
            }

            switch (s_Status)
            {
                case Status.Uninitialized:
                    Dbg.Err($"A static reference class was accessed before any Parser had been created; the dec database is empty. Static reference fields are not populated until the setup step of Parser.Finish().");
                    break;
                case Status.Accumulating:
                    Dbg.Err($"A static reference class was accessed while the Parser was still accumulating input, before Parser.Finish() was called. Static reference fields are not populated until the setup step of Parser.Finish().");
                    break;
                case Status.Processing:
                    Dbg.Err($"A static reference class was accessed during dec parsing, most likely from a Dec constructor, field initializer, or converter. Static reference fields are not populated until the setup step of Parser.Finish().");
                    break;
                case Status.Distributing:
                    Dbg.Err($"A static reference class was accessed during the static-reference distribution phase, but was not registered with this Parser. Verify that it is tagged with [StaticReferences] and is reachable by Dec's type discovery.");
                    break;
                case Status.Finalizing:
                case Status.Finished:
                    Dbg.Err($"A static reference class was accessed, but was not registered with this Parser and so was never populated. Verify that it is tagged with [StaticReferences] and is reachable by Dec's type discovery.");
                    break;
            }
        }
    }
}
