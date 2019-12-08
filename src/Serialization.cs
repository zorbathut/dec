namespace Def
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Internal serialization utilities.
    /// </summary>
    internal static class Serialization
    {
        internal static Dictionary<Type, Converter> Converters;

        internal static void Initialize(bool explicitOnly, Type[] explicitConversionTypes)
        {
            Converters = new Dictionary<Type, Converter>();

            IEnumerable<Type> conversionTypes;
            if (explicitConversionTypes != null)
            {
                conversionTypes = explicitConversionTypes;
            }
            else if (explicitOnly)
            {
                conversionTypes = Enumerable.Empty<Type>();
            }
            else
            {
                conversionTypes = Util.GetAllTypes().Where(t => t.IsSubclassOf(typeof(Converter)) && !t.IsAbstract);
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

        internal static object ParseElement(XElement element, Type type, object model, bool rootNode, string inputName, ReaderContext context = null)
        {
            // The first thing we do is parse all our attributes. This is because we want to verify that there are no attributes being ignored.
            // Don't return anything until we do our element.HasAtttributes check!

            // Figure out our intended type, if it's been overridden
            if (element.Attribute("class") != null)
            {
                var className = element.Attribute("class").Value;
                var possibleType = (Type)ParseString(className, typeof(Type), inputName, element.LineNumber());
                if (!type.IsAssignableFrom(possibleType))
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Explicit type {className} cannot be assigned to expected type {type}");
                }
                else if (model != null && model.GetType() != possibleType)
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Explicit type {className} does not match already-provided instance {type}");
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
                Dbg.Err($"{inputName}:{element.LineNumber()}: Element cannot be both null and a reference at the same time");
            }

            // No remaining attributes are allowed
            if (element.HasAttributes)
            {
                Dbg.Err($"{inputName}:{element.LineNumber()}: Has unconsumed attributes");
            }

            // See if we just want to return null
            if (shouldBeNull)
            {
                // okay
                return null;

                // Note: It may seem wrong that we can return null along with a non-null model.
                // The problem is that this is meant to be able to override defaults. If the default if an object, explicitly setting it to null *should* clear the object out.
                // If we actually need a specific object to be returned, for whatever reason, the caller has to do the comparison.
            }

            // See if we can get a ref out of it
            if (refId != null)
            {
                if (context == null)
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Found a reference object outside of record-reader mode");
                    return model;
                }

                if (!context.refs.ContainsKey(refId))
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Found a reference object {refId} without a valid reference mapping");
                    return model;
                }

                object refObject = context.refs[refId];
                if (!type.IsAssignableFrom(refObject.GetType()))
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Reference object {refId} is of type {refObject.GetType()}, which cannot be converted to expected type {type}");
                    return model;
                }

                return refObject;
            }

            bool hasElements = element.Elements().Any();
            bool hasText = element.Nodes().OfType<XText>().Any();
            var text = hasText ? element.Nodes().OfType<XText>().First().Value : "";

            if (hasElements && hasText)
            {
                Dbg.Err($"{inputName}:{element.LineNumber()}: Elements and text are never valid together");
            }

            if (typeof(Def).IsAssignableFrom(type) && hasElements && !rootNode)
            {
                Dbg.Err($"{inputName}:{element.LineNumber()}: Inline def definitions are not currently supported");
                return null;
            }

            if (hasText ||
                (typeof(Def).IsAssignableFrom(type) && !rootNode) ||
                type == typeof(Type) ||
                type == typeof(string) ||
                type.IsPrimitive ||
                (!hasElements && Converters.ContainsKey(type)))
            {
                if (hasElements && !hasText)
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Elements are not valid when parsing {type}");
                }

                return ParseString(text, type, inputName, element.LineNumber());
            }

            // We either have elements, or we're a composite type of some sort that conceptually *does* contain elements, we just don't have any

            // Special case: IRecordables
            if (typeof(IRecordable).IsAssignableFrom(type))
            {
                var recordable = (IRecordable)(model ?? Activator.CreateInstance(type));

                recordable.Record(new RecorderReader(element, context));

                return recordable;
            }

            // Special case: Converter override
            if (Converters.ContainsKey(type))
            {
                // It's possible we already have a model here. For the sake of simplicity (and allowing polymorphic-output Converters) we discard the model entirely.

                var result = Converters[type].FromXml(element, type, inputName);
                if (result != null && !type.IsAssignableFrom(result.GetType()))
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Converter {Converters[type].GetType()} for {type} returned unexpected type {result.GetType()}");
                    return null;
                }

                return result;
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
                        Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    list.Add(ParseElement(fieldElement, referencedType, null, false, inputName, context: context));
                }

                return list;
            }

            // Special case: Arrays
            if (type.IsArray)
            {
                Type referencedType = type.GetElementType();

                var elements = element.Elements().ToArray();
                var array = (Array)(model ?? Activator.CreateInstance(type, new object[] { elements.Length }));
                for (int i = 0; i < elements.Length; ++i)
                {
                    var fieldElement = elements[i];
                    if (fieldElement.Name.LocalName != "li")
                    {
                        Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    array.SetValue(ParseElement(fieldElement, referencedType, null, false, inputName, context: context), i);
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
                            Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Dictionary includes li tag without a key");
                            continue;
                        }

                        if (valueNode == null)
                        {
                            Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Dictionary includes li tag without a value");
                            continue;
                        }

                        var key = ParseElement(keyNode, keyType, null, false, inputName, context: context);

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {key.ToString()}");
                        }

                        dict[key] = ParseElement(valueNode, valueType, null, false, inputName, context: context);
                    }
                    else
                    {
                        var key = ParseString(fieldElement.Name.LocalName, keyType, inputName, fieldElement.LineNumber());

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {fieldElement.Name.LocalName}");
                        }

                        dict[key] = ParseElement(fieldElement, valueType, null, false, inputName, context: context);
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
            if (context != null)
            {
                Dbg.Err($"{inputName}:{element.LineNumber()}: Falling back to reflection within a Record system; this is currently not allowed for security reasons");
                return null;
            }

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
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Duplicate field {fieldName}");
                    // Just allow us to fall through; it's an error, but one with a reasonably obvious handling mechanism
                }
                fields.Add(fieldName);

                var fieldInfo = type.GetFieldFromHierarchy(fieldElement.Name.LocalName);
                if (fieldInfo == null)
                {
                    Dbg.Err($"{inputName}:{element.LineNumber()}: Field {fieldElement.Name.LocalName} does not exist in type {type}");
                    continue;
                }

                fieldInfo.SetValue(model, ParseElement(fieldElement, fieldInfo.FieldType, fieldInfo.GetValue(model), false, inputName, context: context));
            }

            return model;
        }

        internal static object ParseString(string text, Type type, string inputName, int lineNumber)
        {
            // Special case: Converter override
            if (Converters.ContainsKey(type))
            {
                var result = Converters[type].FromString(text, type, inputName, lineNumber);
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
                if (Util.GetDefHierarchyType(type) == null)
                {
                    Dbg.Err($"{inputName}:{lineNumber}: Non-hierarchy defs cannot be used as references");
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

                var possibleType = Type.GetType(text);
                if (possibleType != null)
                {
                    return possibleType;
                }

                var possibleTypes = Util.GetAllTypes().Where(t => t.Name == text || t.FullName == text).ToArray();
                if (possibleTypes.Length == 0)
                {
                    Dbg.Err($"{inputName}:{lineNumber}: Couldn't find type named {text}");
                    return null;
                }
                else if (possibleTypes.Length > 1)
                {
                    Dbg.Err($"{inputName}:{lineNumber}: Found too many types named {text} ({possibleTypes.Select(t => t.FullName).ToCommaString()})");
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
                Dbg.Err($"{inputName}:{lineNumber}: Empty field provided for type {type}");
                return Activator.CreateInstance(type);
            }
            else
            {
                // If we don't have text, and we're not a primitive type, then I'm not sure how we got here, but return null
                Dbg.Err($"{inputName}:{lineNumber}: Empty field provided for type {type}");
                return null;
            }
        }

        internal static XElement ComposeElement(object value, Type fieldType, string label, WriterContext context, bool forceRefContents)
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

            if (typeof(Def).IsAssignableFrom(fieldType))
            {
                // It is! Let's just get the def name and be done with it.
                if (value != null)
                {
                    var valueDef = value as Def;

                    result.Add(new XText(valueDef.defName));
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

            // Now we drop into ref-mode, if allowed
            if (!forceRefContents && !fieldType.IsValueType)
            {
                // We're going to turn this into a reference
                result.SetAttributeValue("ref", context.GetRef(value));

                return result;
            }

            // We'll drop through if we're in force-ref-resolve mode, or if we have something that needs conversion and is a struct (classes get turned into refs)

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = value as IList;
                
                Type referencedType = fieldType.GetGenericArguments()[0];

                for (int i = 0; i < list.Count; ++i)
                {
                    result.Add(ComposeElement(list[i], referencedType, "li", context, false));
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

                    element.Add(ComposeElement(iterator.Key, keyType, "key", context, false));
                    element.Add(ComposeElement(iterator.Value, valueType, "value", context, false));
                }

                return result;
            }

            if (typeof(IRecordable).IsAssignableFrom(fieldType))
            {
                var recordable = value as IRecordable;

                recordable.Record(new RecorderWriter(result, context));

                return result;
            }

            {
                // Look for a converter; that's the only way we're going to handle this one!
                var converter = Serialization.Converters.TryGetValue(fieldType);
                if (converter == null)
                {
                    Dbg.Err($"Couldn't find a converter for type {fieldType}");
                }
                else
                {
                    converter.ToXml(value, result);
                }

                return result;
            }
        }
    }
}
