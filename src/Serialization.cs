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

        internal static object ParseElement(XElement element, Type type, object model, bool rootNode, string inputName)
        {
            // Figure out our intended type
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

            // No remaining attributes are allowed
            if (element.HasAttributes)
            {
                Dbg.Err($"{inputName}:{element.LineNumber()}: Has unconsumed attributes");
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

            // We either have elements, or we're a composite type of some sort and can pretend we do

            // Special case: Converter override
            if (Converters.ContainsKey(type))
            {
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

                var list = (IList)Activator.CreateInstance(type);
                foreach (var fieldElement in element.Elements())
                {
                    if (fieldElement.Name.LocalName != "li")
                    {
                        Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    list.Add(ParseElement(fieldElement, referencedType, null, false, inputName));
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
                        Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    array.SetValue(ParseElement(fieldElement, referencedType, null, false, inputName), i);
                }

                return array;
            }

            // Special case: Dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                // Dictionary<> handling
                Type keyType = type.GetGenericArguments()[0];
                Type valueType = type.GetGenericArguments()[1];

                var dict = (IDictionary)Activator.CreateInstance(type);
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

                        var key = ParseElement(keyNode, keyType, null, false, inputName);

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {key.ToString()}");
                        }

                        dict[key] = ParseElement(valueNode, valueType, null, false, inputName);
                    }
                    else
                    {
                        var key = ParseString(fieldElement.Name.LocalName, keyType, inputName, fieldElement.LineNumber());

                        if (dict.Contains(key))
                        {
                            Dbg.Err($"{inputName}:{fieldElement.LineNumber()}: Dictionary includes duplicate key {fieldElement.Name.LocalName}");
                        }

                        dict[key] = ParseElement(fieldElement, valueType, null, false, inputName);
                    }
                }

                return dict;
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

                fieldInfo.SetValue(model, ParseElement(fieldElement, fieldInfo.FieldType, fieldInfo.GetValue(model), false, inputName));
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

        internal static XElement ComposeElement(object value, Type fieldType, string label, WriterContext refs)
        {
            var result = new XElement(label);

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

            if (typeof(IRecordable).IsAssignableFrom(fieldType))
            {
                // It's a recordable, so we're going to store a reference
                if (value != null)
                {
                    result.SetAttributeValue("ref", refs.GetRef(value as IRecordable));
                }
                else
                {
                    // Need an explicit null here, otherwise (once we support inline references) we'll have no way to distinguish "empty" from "null"
                    result.SetAttributeValue("null", "true");
                }

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
            }

            return result;
        }
    }
}
