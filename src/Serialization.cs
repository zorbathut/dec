namespace Dec
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    /// Internal serialization utilities.
    /// </summary>
    internal static class Serialization
    {
        // Initialize it to empty in order to support Recorder operations without Dec initialization.
        // At some point we'll figure out how to support Converters at that point as well.
        internal static Dictionary<Type, Converter> Converters = new Dictionary<Type, Converter>();

        internal static void Initialize()
        {
            Converters = new Dictionary<Type, Converter>();

            IEnumerable<Type> conversionTypes;
            if (Config.TestParameters == null)
            {
                conversionTypes = UtilReflection.GetAllTypes().Where(t => t.IsSubclassOf(typeof(Converter)) && !t.IsAbstract);
            }
            else if (Config.TestParameters.explicitConverters != null)
            {
                conversionTypes = Config.TestParameters.explicitConverters;
            }
            else
            {
                conversionTypes = Enumerable.Empty<Type>();
            }

            foreach (var type in conversionTypes)
            {
                var converter = (Converter)type.CreateInstanceSafe("converter", () => "converter");

                if (converter != null)
                {
                    var convertedTypes = converter.HandledTypes();
                    if (convertedTypes.Count == 0)
                    {
                        Dbg.Err($"{type} is a Dec.Converter, but doesn't convert anything");
                    }

                    foreach (var convertedType in convertedTypes)
                    {
                        if (Converters.ContainsKey(convertedType))
                        {
                            Dbg.Err($"Converters {Converters[convertedType].GetType()} and {type} both generate result {convertedType}");
                        }

                        Converters[convertedType] = converter;
                    }
                }
            }
        }

        internal static object ParseElement(XElement element, Type type, object model, ReaderContext context, bool isRootDec = false, bool hasReferenceId = false)
        {
            // The first thing we do is parse all our attributes. This is because we want to verify that there are no attributes being ignored.
            // Don't return anything until we do our element.HasAtttributes check!

            // Figure out our intended type, if it's been overridden
            if (element.Attribute("class") != null)
            {
                var className = element.Attribute("class").Value;
                var possibleType = (Type)ParseString(className, typeof(Type), null, context.sourceName, element.LineNumber());
                if (!type.IsAssignableFrom(possibleType))
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Explicit type {className} cannot be assigned to expected type {type}");
                }
                else if (model != null && model.GetType() != possibleType)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Explicit type {className} does not match already-provided instance {type}");
                }
                else
                {
                    type = possibleType;
                }

                element.Attribute("class").Remove();
            }

            bool shouldBeNull = bool.Parse(element.ConsumeAttribute("null") ?? "false");
            string refId = element.ConsumeAttribute("ref");

            if (shouldBeNull && refId != null)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Element cannot be both null and a reference at the same time");

                // There's no good answer here, but we're sticking with the null because it feels like an error-handling path that the user is more likely to properly support.
                refId = null;
            }

            // See if we just want to return null
            if (shouldBeNull)
            {
                // No remaining attributes are allowed in nulls
                if (element.HasAttributes)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Has unconsumed attributes");
                }

                // okay
                return null;

                // Note: It may seem wrong that we can return null along with a non-null model.
                // The problem is that this is meant to be able to override defaults. If the default if an object, explicitly setting it to null *should* clear the object out.
                // If we actually need a specific object to be returned, for whatever reason, the caller has to do the comparison.
            }

            // See if we can get a ref out of it
            if (refId != null)
            {
                // No remaining attributes are allowed in refs
                if (element.HasAttributes)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Has unconsumed attributes");
                }

                if (context == null)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Found a reference object outside of record-reader mode");
                    return model;
                }

                if (!context.refs.ContainsKey(refId))
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Found a reference object {refId} without a valid reference mapping");
                    return model;
                }

                object refObject = context.refs[refId];
                if (!type.IsAssignableFrom(refObject.GetType()))
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Reference object {refId} is of type {refObject.GetType()}, which cannot be converted to expected type {type}");
                    return model;
                }

                return refObject;
            }

            // Converters may do their own processing, so we'll just defer off to them now; hell, you can even have both elements and text, if that's your jam
            if (Converters.ContainsKey(type))
            {
                // context might be null; that's OK at the moment
                object result;

                object GenerateResultFallback()
                {
                    if (model != null)
                    {
                        return model;
                    }
                    else if (type.IsValueType)
                    {
                        return Activator.CreateInstance(type);
                    }
                    else
                    {
                        return null;
                    }
                }

                try
                {
                    result = Converters[type].Record(model, type, new RecorderReader(element, context));
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);

                    result = GenerateResultFallback();
                }

                // This is an important check if we have a referenced type, because if we've changed the result, references won't link up to it properly.
                // Outside referenced types, it doesn't matter - we want to give people as much control over modification as possible.
                if (model != null && hasReferenceId && model != result)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Converter {Converters[type].GetType()} for {type} ignored the model {model} while reading a referenced object; this may cause lost data");
                    return result;
                }

                if (result != null && !type.IsAssignableFrom(result.GetType()))
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Converter {Converters[type].GetType()} for {type} returned unexpected type {result.GetType()}");
                    result = GenerateResultFallback();
                    return result;
                }

                return result;
            }

            // After this point we won't be using a converter in any way, we'll be requiring native Dec types (as native as it gets, at least)

            bool hasChildren = element.Elements().Any();
            bool hasText = element.Nodes().OfType<XText>().Any();

            if (hasChildren && hasText)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Cannot have both text and child nodes in XML (unless it's handled by a Converter and you're doing it yourself) - this is probably a typo, maybe you have the wrong number of close tags or added text somewhere you didn't mean to?");

                // we'll just fall through and try to parse anyway, though
            }

            if (typeof(Dec).IsAssignableFrom(type) && hasChildren && !isRootDec)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Defining members of an item of type {type}, derived from Dec.Dec, is not supported within an outer Dec. Either reference a {type} defined independently or remove {type}'s inheritance from Dec.");
                return null;
            }

            // Special case: IRecordables
            if (typeof(IRecordable).IsAssignableFrom(type))
            {
                var recordable = (IRecordable)(model ?? type.CreateInstanceSafe("recordable", () => $"{context.sourceName}:{element.LineNumber()}"));

                if (recordable != null)
                {
                    recordable.Record(new RecorderReader(element, context));

                    // TODO: support indices if this is within the Dec system?
                }

                return recordable;
            }

            // No remaining attributes are allowed past this point!
            if (element.HasAttributes)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Has unconsumed attributes");
            }

            // All our standard text-using options
            if (hasText ||
                (typeof(Dec).IsAssignableFrom(type) && !isRootDec) ||
                type == typeof(Type) ||
                type == typeof(string) ||
                type.IsPrimitive)
            {
                if (hasChildren)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Child nodes are not valid when parsing {type}");
                }

                return ParseString(element.GetText(), type, model, context.sourceName, element.LineNumber());
            }

            // Nothing past this point even supports text, so let's just get angry and break stuff.
            if (hasText)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Text detected in a situation where it is invalid; will be ignored");
            }

            // Special case: Lists
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                // List<> handling
                Type referencedType = type.GetGenericArguments()[0];

                var list = (IList)(model ?? Activator.CreateInstance(type));

                // If you have a default list, but specify it in XML, we assume this is a full override. Clear the original list.
                list.Clear();

                foreach (var fieldElement in element.Elements())
                {
                    if (fieldElement.Name.LocalName != "li")
                    {
                        Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    list.Add(ParseElement(fieldElement, referencedType, null, context));
                }

                return list;
            }

            // Special case: Arrays
            if (type.IsArray)
            {
                Type referencedType = type.GetElementType();

                var elements = element.Elements().ToArray();

                // We don't bother falling back on model here; we probably need to recreate it anyway with the right length
                var array = (Array)Activator.CreateInstance(type, new object[] { elements.Length });

                for (int i = 0; i < elements.Length; ++i)
                {
                    var fieldElement = elements[i];
                    if (fieldElement.Name.LocalName != "li")
                    {
                        Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    array.SetValue(ParseElement(fieldElement, referencedType, null, context), i);
                }

                return array;
            }

            // Special case: Dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // Dictionary<> handling
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];

                var dict = (IDictionary)(model ?? Activator.CreateInstance(type));

                // If you have a default dict, but specify it in XML, we assume this is a full override. Clear the original dict.
                dict.Clear();

                foreach (var fieldElement in element.Elements())
                {
                    if (fieldElement.Name.LocalName == "li")
                    {
                        // Treat this like a key/value pair
                        var keyNode = fieldElement.ElementNamed("key");
                        var valueNode = fieldElement.ElementNamed("value");

                        if (keyNode == null)
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes li tag without a key");
                            continue;
                        }

                        if (valueNode == null)
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes li tag without a value");
                            continue;
                        }

                        var key = ParseElement(keyNode, keyType, null, context);

                        if (key == null)
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes null key, skipping pair");
                            continue;
                        }

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {key.ToString()}");
                        }

                        dict[key] = ParseElement(valueNode, valueType, null, context);
                    }
                    else
                    {
                        var key = ParseString(fieldElement.Name.LocalName, keyType, null, context.sourceName, fieldElement.LineNumber());

                        if (key == null)
                        {
                            // it's really rare for this to happen, I think you could do it with a converter but that's it
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes null key, skipping pair");
                            continue;
                        }

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {fieldElement.Name.LocalName}");
                        }

                        dict[key] = ParseElement(fieldElement, valueType, null, context);
                    }
                }

                return dict;
            }

            // Special case: HashSet
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                // HashSet<> handling
                // This is a gigantic pain because HashSet<> doesn't inherit from any non-generic interface that provides the functionality we want
                // So we're stuck doing it all through object and reflection
                // Thanks, HashSet
                // This might be a performance problem and we'll . . . deal with it later I guess?
                // This might actually be a good first place to use IL generation.

                Type keyType = type.GetGenericArguments()[0];

                var set = model ?? Activator.CreateInstance(type);

                var clearFunction = set.GetType().GetMethod("Clear");
                var containsFunction = set.GetType().GetMethod("Contains");
                var addFunction = set.GetType().GetMethod("Add");

                // If you have a default set, but specify it in XML, we assume this is a full override. Clear the original set.
                // Did you know there's no non-generic interface that HashSet<> supports that includes a Clear function?
                // Fun fact:
                // That thing I just wrote!
                clearFunction.Invoke(set, null);

                foreach (var fieldElement in element.Elements())
                {
                    // There's a potential bit of ambiguity here if someone does <li /> and expects that to be an actual string named "li".
                    // Practically, I think this is less likely than someone doing <li></li> and expecting that to be the empty string.
                    // And there's no other way to express the empty string.
                    // So . . . we treat that like the empty string.
                    if (fieldElement.Name.LocalName == "li")
                    {
                        // Treat this like a full node
                        var key = ParseElement(fieldElement, keyType, null, context);
                        var keyParam = new object[] { key };

                        if (key == null)
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: HashSet includes null key, skipping");
                            continue;
                        }

                        if ((bool)containsFunction.Invoke(set, keyParam))
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: HashSet includes duplicate key {key.ToString()}");
                        }

                        addFunction.Invoke(set, keyParam);
                    }
                    else
                    {
                        if (fieldElement.HasElements)
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: HashSet non-li member includes data, ignoring");
                        }

                        var key = ParseString(fieldElement.Name.LocalName, keyType, null, context.sourceName, fieldElement.LineNumber());
                        var keyParam = new object[] { key };

                        if (key == null)
                        {
                            // it's really rare for this to happen, I think you could do it with a converter but that's it
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: HashSet includes null key, skipping pair");
                            continue;
                        }

                        if ((bool)containsFunction.Invoke(set, keyParam))
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: HashSet includes duplicate key {fieldElement.Name.LocalName}");
                        }

                        addFunction.Invoke(set, keyParam);
                    }
                }

                return set;
            }

            // At this point, we're either a class or a struct, and we need to do the reflection thing

            // If we have refs, something has gone wrong; we should never be doing reflection inside a Record system.
            // This is a really ad-hoc way of testing this and should be fixed.
            // One big problem here is that I'm OK with security vulnerabilities in dec xmls. Those are either supplied by the developer or by mod authors who are intended to have full code support anyway.
            // I'm less OK with security vulnerabilities in save files. Nobody expects a savefile can compromise their system.
            // And the full reflection system is probably impossible to secure, whereas the Record system should be secureable.
            if (context.RecorderMode)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Falling back to reflection within a Record system; this is currently not allowed for security reasons");
                return model;
            }

            // If we haven't been given a template class from our parent, go ahead and init to defaults
            if (model == null)
            {
                model = type.CreateInstanceSafe("object", () => $"{context.sourceName}:{element.LineNumber()}");

                if (model == null)
                {
                    // error already reported
                    return model;
                }
            }

            var setFields = new HashSet<string>();
            foreach (var fieldElement in element.Elements())
            {
                // Check for fields that have been set multiple times
                string fieldName = fieldElement.Name.LocalName;
                if (setFields.Contains(fieldName))
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Duplicate field {fieldName}");
                    // Just allow us to fall through; it's an error, but one with a reasonably obvious handling mechanism
                }
                setFields.Add(fieldName);

                var fieldInfo = type.GetFieldFromHierarchy(fieldName);
                if (fieldInfo == null)
                {
                    // Try to find a close match, if we can, just for a better error message
                    string match = null;
                    string canonicalFieldName = Util.LooseMatchCanonicalize(fieldName);

                    foreach (var testField in type.GetSerializableFieldsFromHierarchy())
                    {
                        if (Util.LooseMatchCanonicalize(testField.Name) == canonicalFieldName)
                        {
                            match = testField.Name;

                            // We could in theory do something overly clever where we try to find the best name, but I really don't care that much; this is meant as a quick suggestion, not an ironclad solution.
                            break;
                        }
                    }

                    if (match != null)
                    {
                        Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Field {fieldName} does not exist in type {type}; did you mean {match}?");
                    }
                    else
                    {
                        Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Field {fieldName} does not exist in type {type}");
                    }
                    
                    continue;
                }

                if (fieldInfo.GetCustomAttribute<IndexAttribute>() != null)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Attempting to set index field {fieldName}; these are generated by the dec system");
                    continue;
                }

                if (fieldInfo.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Attempting to set nonserialized field {fieldName}");
                    continue;
                }

                // Check for fields we're not allowed to set
                if (UtilReflection.ReflectionSetForbidden(fieldInfo))
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Field {fieldName} is not allowed to be set through reflection");
                    continue;
                }

                fieldInfo.SetValue(model, ParseElement(fieldElement, fieldInfo.FieldType, fieldInfo.GetValue(model), context));
            }


            // Set up our index fields; this has to happen last in case we're a struct
            Index.Register(ref model);

            return model;
        }

        internal static object ParseString(string text, Type type, object model, string inputName, int lineNumber)
        {
            // Special case: Converter override
            // This is redundant if we're being called from ParseElement, but we aren't always.
            if (Converters.ContainsKey(type))
            {
                object result;

                try
                {
                    result = Converters[type].FromString(text, type, inputName, lineNumber);
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);

                    if (type.IsValueType)
                    {
                        result = Activator.CreateInstance(type);
                    }
                    else
                    {
                        result = null;
                    }
                }

                if (result != null && !type.IsAssignableFrom(result.GetType()))
                {
                    Dbg.Err($"{inputName}:{lineNumber}: Converter {Converters[type].GetType()} for {type} returned unexpected type {result.GetType()}");
                    return null;
                }

                return result;
            }

            // Special case: decs
            if (typeof(Dec).IsAssignableFrom(type))
            {
                if (type.GetDecRootType() == null)
                {
                    Dbg.Err($"{inputName}:{lineNumber}: Non-hierarchy decs cannot be used as references");
                    return null;
                }

                if (text == "" || text == null)
                {
                    // you reference nothing, you get the null
                    return null;
                }
                else
                {
                    Dec result = Database.Get(type, text);
                    if (result == null)
                    {
                        // This feels very hardcoded, but these are also *by far* the most common errors I've seen, and I haven't come up with a better and more general solution
                        if (text.Contains(" "))
                        {
                            Dbg.Err($"{inputName}:{lineNumber}: Dec name \"{text}\" is not a valid identifier; consider removing spaces");
                        }
                        else if (text.Contains("\""))
                        {
                            Dbg.Err($"{inputName}:{lineNumber}: Dec name \"{text}\" is not a valid identifier; consider removing quotes");
                        }
                        else if (!Parser.DecNameValidator.IsMatch(text))
                        {
                            Dbg.Err($"{inputName}:{lineNumber}: Dec name \"{text}\" is not a valid identifier; dec identifiers must be valid C# identifiers");
                        }
                        else
                        {
                            Dbg.Err($"{inputName}:{lineNumber}: Couldn't find {type} named {text}");
                        }
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

                return UtilType.ParseDecFormatted(text, inputName, lineNumber);
            }

            // Various non-composite-type special-cases
            if (text != "")
            {
                // If we've got text, treat us as an object of appropriate type
                try
                {
                    return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(text);
                }
                catch (System.Exception e)  // I would normally not catch System.Exception, but TypeConverter is wrapping FormatException in an Exception for some reason
                {
                    Dbg.Err($"{inputName}:{lineNumber}: {e.ToString()}");
                    return model;
                }
            }
            else if (type == typeof(string))
            {
                // If we don't have text, and we're a string, return ""
                return "";
            }
            else
            {
                // If we don't have text, and we've fallen down to this point, that's an error (and return original value I guess)
                Dbg.Err($"{inputName}:{lineNumber}: Empty field provided for type {type}");
                return model;
            }
        }

        internal static Type TypeSystemRuntimeType = Type.GetType("System.RuntimeType");
        internal static void ComposeElement(WriterNode node, object value, Type fieldType, bool isRootDec = false)
        {
            // Handle Dec types, if this isn't a root (otherwise we'd just reference ourselves and that's kind of pointless)
            if (!isRootDec && value is Dec)
            {
                // Dec types are special in a few ways.
                // First off, they don't include their type data, because we assume it's of a type provided by the structure.
                // Second, we represent null values as an empty string, not as a null tag.
                // (We'll accept the null tag if you insist, we just have a cleaner special case.)
                // Null tag stuff is done further down, in the null check.

                var rootType = value.GetType().GetDecRootType();
                if (!rootType.IsAssignableFrom(fieldType))
                {
                    // fieldType isn't a root type that we expect.
                    // We could put our root type here so we could re-parse it later on. But this is actually a huge problem.
                    // What if we move the Dec to another hierarchy entirely? Or just rename a hierarchy?
                    // We won't be able to find it given the root type. Do we search *all* Decs for it? This is a nightmare case.
                    // So instead we yell at the user and tell them this won't work (then save it anyway.)
                    Dbg.Err($"Attempting to save {value} into type {fieldType}, which is not a valid Dec root type");
                    node.TagClass(rootType);
                }
                
                node.WriteDec(value as Dec);

                return;
            }

            // Everything represents "null" with an explicit XML tag, so let's just do that
            // Maybe at some point we want to special-case this for the empty Dec link
            if (value == null)
            {
                if (typeof(Dec).IsAssignableFrom(fieldType))
                {
                    node.WriteDec(null);
                }
                else
                {
                    node.WriteExplicitNull();
                }

                return;
            }

            var valType = value.GetType();

            // This is our value's type, but we may need a little bit of tinkering to make it useful.
            // The current case I know of is System.RuntimeType, which appears if we call .GetType() on a Type.
            // I assume there is a complicated internal reason for this; good news, we can ignore it and just pretend it's a System.Type.
            // Bad news: it's actually really hard to detect this case because System.RuntimeType is private.
            // That's why we have the annoying `static` up above.
            if (valType == TypeSystemRuntimeType)
            {
                valType = typeof(Type);
            }

            // If we have a type that isn't the expected type, tag it. We need to do this before any further handling because everything fits in `object`.
            if (valType != fieldType)
            {
                node.TagClass(valType);
            }

            // Do all our unreferencables first
            if (valType.IsPrimitive)
            {
                node.WritePrimitive(value);

                return;
            }

            if (value is System.Enum)
            {
                node.WriteEnum(value);

                return;
            }

            if (value is string)
            {
                node.WriteString(value as string);

                return;
            }

            if (value is Type)
            {
                node.WriteType(value as Type);

                return;
            }

            // Check to see if we should make this into a ref
            if (!valType.IsValueType)
            {
                if (node.WriteReference(value))
                {
                    // The ref system has set up the appropriate tagging, so we're done!
                    return;
                }

                // Either this isn't a reference yet, or we don't even support references in this mode. So keep on processing.
            }

            if (valType.IsArray)
            {
                node.WriteArray(value as Array);

                return;
            }

            if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(List<>))
            {
                node.WriteList(value as IList);

                return;
            }

            if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                node.WriteDictionary(value as IDictionary);

                return;
            }

            if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                node.WriteHashSet(value as IEnumerable);

                return;
            }

            if (value is IRecordable)
            {
                node.WriteRecord(value as IRecordable);

                return;
            }

            {
                // Look for a converter; that's the only way to handle this before we fall back to reflection
                var converter = Serialization.Converters.TryGetValue(fieldType);
                if (converter != null)
                {
                    node.WriteConvertible(converter, value, fieldType);
                    return;
                }
            }

            if (!node.AllowReflection)
            {
                Dbg.Err($"Couldn't find a composition method for type {fieldType}; do you need a Converter?");
                return;
            }

            // We absolutely should not be doing reflection when in recorder mode; that way lies madness.
            
            foreach (var field in valType.GetSerializableFieldsFromHierarchy())
            {
                ComposeElement(node.CreateMember(field), field.GetValue(value), field.FieldType);
            }

            return;
        }

        internal static void Clear()
        {
            Converters = new Dictionary<Type, Converter>();
        }
    }
}
