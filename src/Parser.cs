namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    
    /// <summary>
    /// Handles all parsing and initialization of def structures.
    /// </summary>
    public class Parser
    {
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
        private Dictionary<string, Type> typeLookup = new Dictionary<string, Type>();
        private List<Type> staticReferences = new List<Type>();
        
        // List of work to be run during the Finish stage
        private List<Action> finishWork = new List<Action>();

        // Used to deal with static reference validation
        private static HashSet<Type> staticReferencesRegistered = new HashSet<Type>();
        private static HashSet<Type> staticReferencesRegistering = new HashSet<Type>();

        /// <summary>
        /// Creates a Parser.
        /// </summary>
        /// <remarks>
        /// Parameters are provided for the sake of unit tests. Using them is unnecessary and generally not recommended.
        /// </remarks>
        public Parser(bool explicitOnly = false, Type[] explicitTypes = null, Type[] explicitStaticRefs = null, Type[] explicitConverters = null)
        {
            if (s_Status != Status.Uninitialized)
            {
                Dbg.Err($"Parser created while the world is in {s_Status} state; should be {Status.Uninitialized} state");
            }
            s_Status = Status.Accumulating;

            {
                IEnumerable<Type> defTypes;
                if (explicitTypes != null)
                {
                    defTypes = explicitTypes;
                }
                else if (explicitOnly)
                {
                    defTypes = Enumerable.Empty<Type>();
                }
                else
                {
                    defTypes = UtilReflection.GetAllTypes().Where(t => t.IsSubclassOf(typeof(Def)));
                }

                foreach (var type in defTypes)
                {
                    if (type.IsSubclassOf(typeof(Def)))
                    {
                        typeLookup[type.Name] = type;
                    }
                    else
                    {
                        Dbg.Err($"{type} is not a subclass of Def");
                    }
                }
            }

            {
                IEnumerable<Type> staticRefs;
                if (explicitStaticRefs != null)
                {
                    staticRefs = explicitStaticRefs;
                }
                else if (explicitOnly)
                {
                    staticRefs = Enumerable.Empty<Type>();
                }
                else
                {
                    staticRefs = UtilReflection.GetAllTypes().Where(t => t.HasAttribute(typeof(StaticReferencesAttribute)));
                }

                foreach (var type in staticRefs)
                {
                    if (!type.HasAttribute(typeof(StaticReferencesAttribute)))
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

            Serialization.Initialize(explicitOnly, explicitConverters);
        }

        private static readonly Regex DefNameValidator = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        /// Pass an XML document string in for processing.
        /// </summary>
        /// <param name="stringName">A human-readable identifier useful for debugging. Generally, the name of the file that the string was read from. Not required (but very useful.)</param>
        public void AddString(string input, string stringName = "(unnamed)")
        {
            // This is a really easy error to make; we might as well handle it.
            if (input.EndsWith(".xml"))
            {
                Dbg.Err($"It looks like you've passed the filename {input} to AddString instead of the actual XML file. Either use AddFile() or pass the file contents in.");
            }

            if (s_Status != Status.Accumulating)
            {
                Dbg.Err($"Adding data while while the world is in {s_Status} state; should be {Status.Accumulating} state");
            }

            XDocument doc;

            try
            {
                doc = XDocument.Parse(input, LoadOptions.SetLineInfo);
            }
            catch (System.Xml.XmlException e)
            {
                Dbg.Ex(e);
                return;
            }

            if (doc.Elements().Count() > 1)
            {
                Dbg.Err($"{stringName}: Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            var readerContext = new ReaderContext(stringName, false);

            foreach (var rootElement in doc.Elements())
            {
                if (rootElement.Name.LocalName != "Defs")
                {
                    Dbg.Wrn($"{stringName}:{rootElement.LineNumber()}: Found root element with name \"{rootElement.Name.LocalName}\" when it should be \"Defs\"");
                }

                foreach (var defElement in rootElement.Elements())
                {
                    string typeName = defElement.Name.LocalName;

                    Type typeHandle = typeLookup.TryGetValue(typeName);
                    if (typeHandle == null)
                    {
                        Dbg.Err($"{stringName}:{defElement.LineNumber()}: {typeName} is not a valid root Def type");
                        continue;
                    }

                    if (defElement.Attribute("defName") == null)
                    {
                        Dbg.Err($"{stringName}:{defElement.LineNumber()}: No def name provided");
                        continue;
                    }

                    string defName = defElement.Attribute("defName").Value;
                    if (!DefNameValidator.IsMatch(defName))
                    {
                        Dbg.Err($"{stringName}:{defElement.LineNumber()}: Def name \"{defName}\" doesn't match regex pattern \"{DefNameValidator}\"");
                        continue;
                    }

                    // Consume defName so we know it's not hanging around
                    defElement.Attribute("defName").Remove();

                    // Create our instance
                    var defInstance = (Def)Activator.CreateInstance(typeHandle);
                    defInstance.DefName = defName;

                    Database.Register(defInstance);

                    finishWork.Add(() => Serialization.ParseElement(defElement, typeHandle, defInstance, readerContext, isRootDef: true));
                }
            }
        }

        /// <summary>
        /// Pass a file in for processing.
        /// </summary>
        /// <param name="stringName">A human-readable identifier useful for debugging. Generally, the name of the file that the string was read from. Not required; will be derived from filename automatically</param>
        public void AddFile(string filename, string identifier = null)
        {
            if (identifier == null)
            {
                // This is imperfect, but good enough. People can pass their own identifier in if they want something clever.
                identifier = Path.GetFileName(filename);
            }

            AddString(File.ReadAllText(filename), identifier);
        }

        /// <summary>
        /// Finish all parsing.
        /// </summary>
        public void Finish()
        {
            if (s_Status != Status.Accumulating)
            {
                Dbg.Err($"Finishing while the world is in {s_Status} state; should be {Status.Accumulating} state");
            }
            s_Status = Status.Processing;

            foreach (var work in finishWork)
            {
                work();
            }

            if (s_Status != Status.Processing)
            {
                Dbg.Err($"Distributing while the world is in {s_Status} state; should be {Status.Processing} state");
            }
            s_Status = Status.Distributing;

            staticReferencesRegistering.Clear();
            staticReferencesRegistering.UnionWith(staticReferences);
            foreach (var stat in staticReferences)
            {
                StaticReferencesAttribute.StaticReferencesFilled.Add(stat);

                foreach (var field in stat.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
                {
                    var def = Database.Get(field.FieldType, field.Name);
                    if (def == null)
                    {
                        Dbg.Err($"Failed to find {field.FieldType} named {field.Name}");
                    }
                    else if (!field.FieldType.IsAssignableFrom(def.GetType()))
                    {
                        Dbg.Err($"Static reference {field.FieldType} {stat}.{field.Name} is not compatible with {def.GetType()} {def}");
                        field.SetValue(null, null); // this is unnecessary, but it does kick the static constructor just in case we wouldn't do it otherwise
                    }
                    else
                    {
                        field.SetValue(null, def);
                    }
                }

                if (!staticReferencesRegistered.Contains(stat))
                {
                    Dbg.Err($"Failed to properly register {stat}; you may be missing a call to Def.StaticReferences.Initialized() in its static constructor");
                }
            }

            if (s_Status != Status.Distributing)
            {
                Dbg.Err($"Finalizing while the world is in {s_Status} state; should be {Status.Distributing} state");
            }
            s_Status = Status.Finalizing;

            foreach (var def in Database.List)
            {
                try
                {
                    foreach (var err in def.ConfigErrors())
                    {
                        Dbg.Err($"{def.GetType()} {def}: {err}");
                    }
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                }
            }

            foreach (var def in Database.List)
            {
                try
                {
                    foreach (var err in def.PostLoad())
                    {
                        Dbg.Err($"{def.GetType()} {def}: {err}");
                    }
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
            var frame = new StackFrame(2);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            if (s_Status != Status.Distributing)
            {
                Dbg.Err($"Initializing static reference class {type} while the world is in {s_Status} state; should be {Status.Distributing} state - this probably means you accessed a static reference class before it was ready");
            }
            else if (!staticReferencesRegistering.Contains(type))
            {
                Dbg.Err($"Initializing static reference class {type} which was not originally detected as a static reference class");
            }

            staticReferencesRegistered.Add(type);
        }
    }
}