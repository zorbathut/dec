namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    
    /// <summary>
    /// Handles all parsing and initialization of dec structures.
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
        private List<Type> staticReferences = new List<Type>();

        // A list of types that can be inherited from
        private struct Parent
        {
            public XElement xml;
            public ReaderContext context;
            public string parent;
        }
        private Dictionary<Tuple<Type, string>, Parent> potentialParents = new Dictionary<Tuple<Type, string>, Parent>();

        // A list of inheritance-based work that still has to be resolved
        private struct InheritanceJob
        {
            public Dec target;
            public XElement xml;
            public ReaderContext context;
            public string parent;
        }
        private List<InheritanceJob> inheritanceJobs = new List<InheritanceJob>();

        // List of work to be run during the Finish stage
        private List<Action> finishWork = new List<Action>();

        // Used for static reference validation
        private static Action s_StaticReferenceHandler = null;

        /// <summary>
        /// Creates a Parser.
        /// </summary>
        public Parser()
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

        // this should really be yanked out of here
        internal static readonly Regex DecNameValidator = new Regex(@"^[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}][\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]*$", RegexOptions.Compiled);

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
                // This isn't testable, unfortunately; XDocument doesn't even support multiple root elements.
                Dbg.Err($"{stringName}: Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            var readerContext = new ReaderContext(stringName, false);

            foreach (var rootElement in doc.Elements())
            {
                if (rootElement.Name.LocalName != "Decs")
                {
                    Dbg.Wrn($"{stringName}:{rootElement.LineNumber()}: Found root element with name \"{rootElement.Name.LocalName}\" when it should be \"Decs\"");
                }

                foreach (var decElement in rootElement.Elements())
                {
                    string typeName = decElement.Name.LocalName;

                    Type typeHandle = UtilType.ParseDecFormatted(typeName, stringName, decElement.LineNumber());
                    if (typeHandle == null || !typeof(Dec).IsAssignableFrom(typeHandle))
                    {
                        Dbg.Err($"{stringName}:{decElement.LineNumber()}: {typeName} is not a valid root Dec type");
                        continue;
                    }

                    if (decElement.Attribute("decName") == null)
                    {
                        Dbg.Err($"{stringName}:{decElement.LineNumber()}: No dec name provided");
                        continue;
                    }

                    string decName = decElement.Attribute("decName").Value;
                    if (!DecNameValidator.IsMatch(decName))
                    {
                        // This feels very hardcoded, but these are also *by far* the most common errors I've seen, and I haven't come up with a better and more general solution
                        if (decName.Contains(" "))
                        {
                            Dbg.Err($"{stringName}:{decElement.LineNumber()}: Dec name \"{decName}\" is not a valid identifier; consider removing spaces");
                        }
                        else if (decName.Contains("\""))
                        {
                            Dbg.Err($"{stringName}:{decElement.LineNumber()}: Dec name \"{decName}\" is not a valid identifier; consider removing quotes");
                        }
                        else
                        {
                            Dbg.Err($"{stringName}:{decElement.LineNumber()}: Dec name \"{decName}\" is not a valid identifier; dec identifiers must be valid C# identifiers");
                        }
                        
                        continue;
                    }

                    // Consume decName so we know it's not hanging around
                    decElement.Attribute("decName").Remove();

                    // Check to see if we're abstract
                    bool abstrct = false;
                    {
                        var abstractAttribute = decElement.Attribute("abstract");
                        if (abstractAttribute != null)
                        {
                            if (!bool.TryParse(abstractAttribute.Value, out abstrct))
                            {
                                Dbg.Err($"{stringName}:{decElement.LineNumber()}: Error encountered when parsing abstract attribute");
                            }

                            abstractAttribute.Remove();
                        }
                    }

                    // Get our parent info
                    string parent = null;
                    {
                        var parentAttribute = decElement.Attribute("parent");
                        if (parentAttribute != null)
                        {
                            parent = parentAttribute.Value;

                            parentAttribute.Remove();
                        }
                    }

                    // Register ourselves as an available parenting object
                    {
                        var identifier = Tuple.Create(typeHandle.GetDecRootType(), decName);
                        if (potentialParents.ContainsKey(identifier))
                        {
                            Dbg.Err($"{stringName}:{decElement.LineNumber()}: Dec {identifier.Item1}:{identifier.Item2} defined twice");
                        }
                        else
                        {
                            potentialParents[identifier] = new Parent { xml = decElement, context = readerContext, parent = parent };
                        }
                    }

                    if (!abstrct)
                    {
                        // Not an abstract dec instance, so create our instance
                        var decInstance = (Dec)typeHandle.CreateInstanceSafe("dec", () => $"{stringName}:{decElement.LineNumber()}");

                        // Error reporting happens within CreateInstanceSafe; if we get null out, we just need to clean up elegantly
                        if (decInstance != null)
                        {
                            decInstance.DecName = decName;

                            Database.Register(decInstance);

                            if (parent == null)
                            {
                                // Non-parent objects are simple; we just handle them here in order to avoid unnecessary GC churn
                                finishWork.Add(() => Serialization.ParseElement(decElement, typeHandle, decInstance, readerContext, new Recorder.Context(), isRootDec: true));
                            }
                            else
                            {
                                // Add an inheritance resolution job; we'll take care of this soon
                                inheritanceJobs.Add(new InheritanceJob { target = decInstance, xml = decElement, context = readerContext, parent = parent });
                            }
                        }
                    }
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
        /// Pass a directory in for recursive processing.
        /// </summary>
        /// <param name="directory">The directory to look for files in.</param>
        /// <param name="pattern">The filename glob pattern to match.</param>
        public void AddDirectory(string directory, string pattern = "*.xml")
        {
            foreach (var file in Directory.GetFiles(directory, pattern, SearchOption.AllDirectories))
            {
                AddFile(file);
            }
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

            // We've successfully hit the Finish call, so let's stop spitting out empty warnings.
            Database.SuppressEmptyWarning();

            // Resolve all our inheritance jobs
            foreach (var work in inheritanceJobs)
            {
                // These are the actions we need to perform; we actually have to resolve these backwards (it makes their construction a little easier)
                // The final parse is listed first, then all the children up to the final point
                var actions = new List<Action>();

                actions.Add(() => Serialization.ParseElement(work.xml, work.target.GetType(), work.target, work.context, new Recorder.Context(), isRootDec: true));

                string currentDecName = work.target.DecName;
                XElement currentXml = work.xml;
                ReaderContext currentContext = work.context;

                string parentDecName = work.parent;
                while (parentDecName != null)
                {
                    var parentData = potentialParents.TryGetValue(Tuple.Create(work.target.GetType().GetDecRootType(), parentDecName));

                    // This is a struct for the sake of performance, so child itself won't be null
                    if (parentData.xml == null)
                    {
                        Dbg.Err($"{currentContext.sourceName}:{currentXml.LineNumber()}: Dec {currentDecName} is attempting to use parent {parentDecName}, but no such dec exists");

                        // Not much more we can do here.
                        break;
                    }

                    actions.Add(() => Serialization.ParseElement(parentData.xml, work.target.GetType(), work.target, parentData.context, new Recorder.Context(), isRootDec: true));

                    currentDecName = parentDecName;
                    currentXml = parentData.xml;
                    currentContext = parentData.context;

                    parentDecName = parentData.parent;
                }

                finishWork.Add(() =>
                {
                    for (int i = actions.Count - 1; i >= 0; --i)
                    {
                        actions[i]();
                    }
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
                        Dbg.Err($"Failed to find {field.FieldType} declaration named {field.Name}");
                        field.SetValue(null, null); // this is unnecessary, but it does kick the static constructor just in case we wouldn't do it otherwise
                    }
                    else if (!field.FieldType.IsAssignableFrom(dec.GetType()))
                    {
                        Dbg.Err($"Static reference {field.FieldType} {stat}.{field.Name} is not compatible with {dec.GetType()} {dec}");
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
                    dec.ConfigErrors(err => Dbg.Err($"{dec.GetType()} {dec}: {err}"));
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
                    dec.PostLoad(err => Dbg.Err($"{dec.GetType()} {dec}: {err}"));
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
            if (s_StaticReferenceHandler != null)
            {
                s_StaticReferenceHandler();
                return;
            }

            Dbg.Err($"Initializing static reference class at an inappropriate time - this probably means you accessed a static reference class before it was ready");
        }
    }
}