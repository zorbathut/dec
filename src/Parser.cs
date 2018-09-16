namespace Def
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    

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

        // Data stored from Parser
        private Dictionary<string, Type> typeLookup = new Dictionary<string, Type>();
        private List<Type> staticReferences = new List<Type>();
        
        // List of work to be run during the Finish stage
        private List<Action> finishWork = new List<Action>();

        // Used to deal with static reference validation
        private static HashSet<Type> staticReferencesRegistered = new HashSet<Type>();
        private static HashSet<Type> staticReferencesRegistering = new HashSet<Type>();

        public Parser(Type[] explicitTypes = null, Type[] explicitStaticRefs = null)
        {
            if (s_Status != Status.Uninitialized)
            {
                Dbg.Err($"Parser created while the world is in {s_Status} state; should be {Status.Uninitialized} state");
            }
            s_Status = Status.Accumulating;

            IEnumerable<Type> types;
            if (explicitTypes != null)
            {
                types = explicitTypes;
            }
            else
            {
                types = Util.GetAllTypes().Where(t => t.IsSubclassOf(typeof(Def)));
            }

            foreach (var type in types)
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

            IEnumerable<Type> staticRefs;
            if (explicitStaticRefs != null)
            {
                staticRefs = explicitStaticRefs;
            }
            else
            {
                staticRefs = Util.GetAllTypes().Where(t => t.HasAttribute(typeof(StaticReferences)));
            }

            foreach (var type in staticRefs)
            {
                if (!type.HasAttribute(typeof(StaticReferences)))
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

        private static readonly Regex DefNameValidator = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        public void AddString(string input)
        {
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
                Dbg.Err($"Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            foreach (var rootElement in doc.Elements())
            {
                if (rootElement.Name.LocalName != "Defs")
                {
                    Dbg.Wrn($"{rootElement.LineNumber()}: Found root element with name \"{rootElement.Name.LocalName}\" when it should be \"Defs\"");
                }

                foreach (var defElement in rootElement.Elements())
                {
                    string typeName = defElement.Name.LocalName;

                    Type typeHandle = typeLookup.TryGetValue(typeName);
                    if (typeHandle == null)
                    {
                        Dbg.Err($"{defElement.LineNumber()}: {typeName} is not a valid root Def type");
                        continue;
                    }

                    if (defElement.Attribute("defName") == null)
                    {
                        Dbg.Err($"{defElement.LineNumber()}: No def name provided");
                        continue;
                    }

                    string defName = defElement.Attribute("defName").Value;
                    if (!DefNameValidator.IsMatch(defName))
                    {
                        Dbg.Err($"{defElement.LineNumber()}: Def name \"{defName}\" doesn't match regex pattern \"{DefNameValidator}\"");
                        continue;
                    }

                    // Consume defName so we know it's not hanging around
                    defElement.Attribute("defName").Remove();

                    // Create our instance
                    var defInstance = (Def)Activator.CreateInstance(typeHandle);
                    defInstance.defName = defName;

                    Database.Register(defInstance);

                    finishWork.Add(() => ParseElement(defElement, typeHandle, defInstance, true));
                }
            }
        }

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
                StaticReferences.StaticReferencesFilled.Add(stat);

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

        private object ParseElement(XElement element, Type type, object model, bool rootNode = false)
        {
            // No attributes are allowed
            if (element.HasAttributes)
            {
                Dbg.Err($"{element.LineNumber()}: Has unconsumed attributes");
            }

            bool hasElements = element.Elements().Any();
            bool hasText = element.Nodes().OfType<XText>().Any();
            var text = hasText ? element.Nodes().OfType<XText>().First().Value : "";

            if (hasElements && hasText)
            {
                Dbg.Err($"{element.LineNumber()}: Elements and text are never valid together");
            }

            if (typeof(Def).IsAssignableFrom(type) && hasElements && !rootNode)
            {
                Dbg.Err($"{element.LineNumber()}: Inline def definitions are not currently supported");
                return null;
            }

            if (hasText ||
                (typeof(Def).IsAssignableFrom(type) && !rootNode) ||
                type == typeof(Type) ||
                type == typeof(string) ||
                type.IsPrimitive)
            {
                if (hasElements && !hasText)
                {
                    Dbg.Err($"{element.LineNumber()}: Elements are not valid when parsing {type}");
                }

                return ParseString(text, type, element.LineNumber());
            }

            // We either have elements, or we're a composite type of some sort and can pretend we do

            // Special case: Lists
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                // List<> handling
                Type referencedType = type.GetGenericArguments()[0];

                var list = (IList)Activator.CreateInstance(type);
                foreach (var fieldElement in element.Elements())
                {
                    if (fieldElement.Name.LocalName != "li")
                    {
                        Dbg.Err($"{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    list.Add(ParseElement(fieldElement, referencedType, null));
                }

                return list;
            }

            // Special case: Arrays
            if (type.IsArray)
            {
                Type referencedType = type.GetElementType();

                var elements = element.Elements().ToArray();
                var array = (Array)Activator.CreateInstance(type, new object[] { elements.Length });
                for (int i = 0; i < elements.Length; ++i)
                {
                    var fieldElement = elements[i];
                    if (fieldElement.Name.LocalName != "li")
                    {
                        Dbg.Err($"{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    array.SetValue(ParseElement(fieldElement, referencedType, null), i);
                }

                return array;
            }

            // Special case: Dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // Dictionary<> handling
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];

                var list = (IDictionary)Activator.CreateInstance(type);
                foreach (var fieldElement in element.Elements())
                {
                    var key = ParseString(fieldElement.Name.LocalName, keyType, fieldElement.LineNumber());

                    if (list.Contains(key))
                    {
                        Dbg.Err($"{fieldElement.LineNumber()}: Dictionary includes duplicate key {fieldElement.Name.LocalName}");
                    }

                    list[key] = ParseElement(fieldElement, valueType, null);
                }

                return list;
            }

            // At this point, we're either a class or a struct

            // If we haven't been given a template class from our parent, go ahead and init to defaults
            if (model == null)
            {
                model = Activator.CreateInstance(type);
            }

            var fields = new HashSet<string>();
            foreach (var fieldElement in element.Elements())
            {
                // Check for fields that have been set multiple times
                string fieldName = fieldElement.Name.LocalName;
                if (fields.Contains(fieldName))
                {
                    Dbg.Err($"{element.LineNumber()}: Duplicate field {fieldName}");
                    // Just allow us to fall through; it's an error, but one with a reasonably obvious handling mechanism
                }
                fields.Add(fieldName);

                var fieldInfo = type.GetFieldFromHierarchy(fieldElement.Name.LocalName);
                if (fieldInfo == null)
                {
                    Dbg.Err($"{element.LineNumber()}: Field {fieldElement.Name.LocalName} does not exist in type {type}");
                    continue;
                }

                fieldInfo.SetValue(model, ParseElement(fieldElement, fieldInfo.FieldType, fieldInfo.GetValue(model)));
            }

            return model;
        }

        private object ParseString(string text, Type type, int lineNumber)
        {
            // Special case: defs
            if (typeof(Def).IsAssignableFrom(type))
            {
                if (Util.GetDefHierarchyType(type) == null)
                {
                    Dbg.Err($"{lineNumber}: Non-hierarchy defs cannot be used as references");
                    return null;
                }

                if (text == "")
                {
                    // you reference nothing, you get the null
                    return null;
                }
                else
                {
                    Def result = Database.Get(type, text);
                    if (result == null)
                    {
                        Dbg.Err($"{lineNumber}: Couldn't find {type} named {text}");
                    }
                    return result;
                }
            }

            // Special case: types
            if (type == typeof(Type))
            {
                if (text == "")
                {
                    return null;
                }

                var possibleTypes = Util.GetAllTypes().Where(t => t.Name == text).ToArray();
                if (possibleTypes.Length == 0)
                {
                    Dbg.Err($"{lineNumber}: Couldn't find type named {text}");
                    return null;
                }
                else if (possibleTypes.Length > 1)
                {
                    Dbg.Err($"{lineNumber}: Found too many types named {text} ({possibleTypes.Select(t => t.FullName).ToCommaString()})");
                    return possibleTypes[0];
                }
                else
                {
                    return possibleTypes[0];
                }
            }

            // Various non-composite-type special-cases
            if (text != "")
            {
                // If we've got text, treat us as an object of appropriate type
                try
                {
                    return TypeDescriptor.GetConverter(type).ConvertFromString(text);
                }
                catch (System.Exception e)  // I would normally not catch System.Exception, but TypeConverter is wrapping FormatException in an Exception for some reason
                {
                    Dbg.Ex(e);
                    return Activator.CreateInstance(type);
                }
            }
            else if (type == typeof(string))
            {
                // If we don't have text, and we're a string, return ""
                return "";
            }
            else if (type.IsPrimitive)
            {
                // If we don't have text, and we're any primitive type, that's an error (and return default values I guess)
                Dbg.Err($"{lineNumber}: Empty field provided for type {type}");
                return Activator.CreateInstance(type);
            }
            else
            {
                // If we don't have text, and we're not a primitive type, then I'm not sure how we got here, but return null
                Dbg.Err($"{lineNumber}: Empty field provided for type {type}");
                return null;
            }
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