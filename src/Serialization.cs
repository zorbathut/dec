namespace Def
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
        // Initialize it to empty in order to support Recorder operations without Def initialization.
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
                var converter = (Converter)System.Activator.CreateInstance(type);
                var convertedTypes = converter.HandledTypes();
                if (convertedTypes.Count == 0)
                {
                    Dbg.Err($"{type} is a Def.Converter, but doesn't convert anything");
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

        internal static object ParseElement(XElement element, Type type, object model, ReaderContext context, bool isRootDef = false, bool hasReferenceId = false)
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

                try
                {
                    result = Converters[type].Record(model, type, new RecorderReader(element, context));
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);

                    if (model != null)
                    {
                        result = model;
                    }
                    else if (type.IsValueType)
                    {
                        result = Activator.CreateInstance(type);
                    }
                    else
                    {
                        result = null;
                    }
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
                    return null;
                }

                return result;
            }

            // After this point we won't be using a converter in any way, we'll be requiring native Def types (as native as it gets, at least)

            bool hasElements = element.Elements().Any();
            bool hasText = element.Nodes().OfType<XText>().Any();

            if (typeof(Def).IsAssignableFrom(type) && hasElements && !isRootDef)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Inline def definitions are not currently supported");
                return null;
            }

            // Special case: IRecordables
            if (typeof(IRecordable).IsAssignableFrom(type))
            {
                var recordable = (IRecordable)(model ?? Activator.CreateInstance(type));

                recordable.Record(new RecorderReader(element, context));

                // TODO: support indices if this is within the Def system?

                return recordable;
            }

            // No remaining attributes are allowed past this point!
            if (element.HasAttributes)
            {
                Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Has unconsumed attributes");
            }

            // All our standard text-using options
            if (hasText ||
                (typeof(Def).IsAssignableFrom(type) && !isRootDef) ||
                type == typeof(Type) ||
                type == typeof(string) ||
                type.IsPrimitive)
            {
                if (hasElements)
                {
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Elements are not valid when parsing {type}");
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

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {key.ToString()}");
                        }

                        dict[key] = ParseElement(valueNode, valueType, null, context);
                    }
                    else
                    {
                        var key = ParseString(fieldElement.Name.LocalName, keyType, null, context.sourceName, fieldElement.LineNumber());

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{context.sourceName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {fieldElement.Name.LocalName}");
                        }

                        dict[key] = ParseElement(fieldElement, valueType, null, context);
                    }
                }

                return dict;
            }

            // At this point, we're either a class or a struct, and we need to do the reflection thing

            // If we have refs, something has gone wrong; we should never be doing reflection inside a Record system.
            // This is a really ad-hoc way of testing this and should be fixed.
            // One big problem here is that I'm OK with security vulnerabilities in def xmls. Those are either supplied by the developer or by mod authors who are intended to have full code support anyway.
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
                model = Activator.CreateInstance(type);
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

                    foreach (var testField in type.GetFieldsFromHierarchy())
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
                    Dbg.Err($"{context.sourceName}:{element.LineNumber()}: Attempting to set index field {fieldName}; these are generated by the def system");
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

            // Special case: defs
            if (typeof(Def).IsAssignableFrom(type))
            {
                if (type.GetDefRootType() == null)
                {
                    Dbg.Err($"{inputName}:{lineNumber}: Non-hierarchy defs cannot be used as references");
                    return null;
                }

                if (text == "" || text == null)
                {
                    // you reference nothing, you get the null
                    return null;
                }
                else
                {
                    Def result = Database.Get(type, text);
                    if (result == null)
                    {
                        Dbg.Err($"{inputName}:{lineNumber}: Couldn't find {type} named {text}");
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

                return UtilType.ParseDefFormatted(text, inputName, lineNumber);
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

        internal static XElement ComposeElement(object value, Type fieldType, string label, WriterContext context, bool isRootDef = false)
        {
            var result = new XElement(label);

            // Do all our unreferencables first
            if (fieldType.IsPrimitive)
            {
                result.Add(new XText(value.ToString()));

                return result;
            }
            
            if (value is string)
            {
                result.Add(new XText(value as string));

                return result;
            }

            if (value is Type)
            {
                result.Add(new XText((value as Type).ComposeDefFormatted()));

                return result;
            }

            if (!isRootDef && typeof(Def).IsAssignableFrom(fieldType))
            {
                // It is! Let's just get the def name and be done with it.
                if (value != null)
                {
                    var valueDef = value as Def;

                    if (valueDef != Database.Get(valueDef.GetType(), valueDef.DefName))
                    {
                        Dbg.Err("Referenced def {valueDef} no longer exists in the database; serializing an error value instead");
                        result.Add(new XText($"{valueDef.DefName}_DELETED"));
                    }
                    else
                    {
                        result.Add(new XText(valueDef.DefName));
                    }
                }

                // "No data" is defined as null for defs, so we just do that

                return result;
            }

            // Everything after this represents "null" with an explicit XML tag, so let's just do that
            if (value == null)
            {
                result.SetAttributeValue("null", "true");
                return result;
            }

            // Check to see if we should make this into a ref
            if (context.RecorderMode && !fieldType.IsValueType)
            {
                if (context.RegisterReference(value, result))
                {
                    // The ref system has set up the appropriate tagging, so we're done!
                    return result;
                }

                // This is not a reference! (yet, at least). So keep on generating it.
            }

            // We'll drop through if we're in force-ref-resolve mode, or if we have something that needs conversion and is a struct (classes get turned into refs)

            // This is also where we need to start being concerned about types. If we have a type that isn't the expected type, tag it.
            if (value.GetType() != fieldType)
            {
                result.Add(new XAttribute("class", value.GetType().ComposeDefFormatted()));
            }

            if (fieldType.IsArray)
            {
                var list = value as Array;

                Type referencedType = fieldType.GetElementType();

                for (int i = 0; i < list.Length; ++i)
                {
                    result.Add(ComposeElement(list.GetValue(i), referencedType, "li", context));
                }

                return result;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = value as IList;
                
                Type referencedType = fieldType.GetGenericArguments()[0];

                for (int i = 0; i < list.Count; ++i)
                {
                    result.Add(ComposeElement(list[i], referencedType, "li", context));
                }

                return result;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var dict = value as IDictionary;

                Type keyType = fieldType.GetGenericArguments()[0];
                Type valueType = fieldType.GetGenericArguments()[1];

                // I really want some way to canonicalize this ordering
                IDictionaryEnumerator iterator = dict.GetEnumerator();
                while (iterator.MoveNext())
                {
                    // In theory, some dicts support inline format, not li format. Inline format is cleaner and smaller and we should be using it when possible.
                    // In practice, it's hard and I'm lazy and this always works, and we're not providing any guarantees about cleanliness of serialized output.
                    // Revisit this later when someone (possibly myself) really wants it improved.
                    var element = new XElement("li");
                    result.Add(element);

                    element.Add(ComposeElement(iterator.Key, keyType, "key", context));
                    element.Add(ComposeElement(iterator.Value, valueType, "value", context));
                }

                return result;
            }

            if (typeof(IRecordable).IsAssignableFrom(fieldType))
            {
                var recordable = value as IRecordable;

                context.RegisterPendingWrite(() => recordable.Record(new RecorderWriter(result, context)));

                return result;
            }

            {
                // Look for a converter; that's the only way to handle this before we fall back to reflection
                var converter = Serialization.Converters.TryGetValue(fieldType);
                if (converter != null)
                {
                    context.RegisterPendingWrite(() => converter.Record(value, fieldType, new RecorderWriter(result, context)));
                    return result;
                }
            }

            if (context.RecorderMode)
            {
                Dbg.Err($"Couldn't find a composition method for type {fieldType}; do you need a Converter?");
                return result;
            }

            // We absolutely should not be doing reflection when in recorder mode; that way lies madness.
            
            foreach (var field in value.GetType().GetFieldsFromHierarchy())
            {
                if (field.IsBackingField())
                {
                    continue;
                }

                if (field.GetCustomAttribute<IndexAttribute>() != null)
                {
                    // we don't save indices
                    continue;
                }

                if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    // we also don't save nonserialized
                    continue;
                }

                result.Add(ComposeElement(field.GetValue(value), field.FieldType, field.Name, context));
            }

            return result;
        }

        internal static void Clear()
        {
            Converters = new Dictionary<Type, Converter>();
        }
    }
}
