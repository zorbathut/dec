namespace Def
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Linq;
    using System.Text.RegularExpressions;

    public class Parser
    {
        private enum Status
        {
            Uninitialized,
            Accumulating,
            Processing,
            Finished,
        }
        private static Status s_Status = Status.Uninitialized;

        private Dictionary<string, Type> typeLookup = new Dictionary<string, Type>();

        public Parser(Type[] types)
        {
            if (s_Status != Status.Uninitialized)
            {
                Dbg.Err($"Parser created while the world is in {s_Status} state; should be {Status.Uninitialized} state");
            }
            s_Status = Status.Accumulating;

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

                    var defInstance = (Def)ParseThing(defElement, typeHandle, null);
                    defInstance.defName = defName;

                    Database.Register(defInstance);
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


            if (s_Status != Status.Processing)
            {
                Dbg.Err($"Completing while the world is in {s_Status} state; should be {Status.Processing} state");
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

        private object ParseThing(XElement element, Type type, object model)
        {
            bool hasElements = element.Elements().Any();
            bool hasText = element.Nodes().OfType<XText>().Any();

            if (!hasElements && hasText)
            {
                // If we've got text, treat us as an object of appropriate type
                try
                {
                    return TypeDescriptor.GetConverter(type).ConvertFromString((element.FirstNode as XText).Value);
                }
                catch (System.Exception e)  // I would normally not catch System.Exception, but TypeConverter is wrapping FormatException in an Exception for some reason
                {
                    Dbg.Ex(e);
                    return Activator.CreateInstance(type);
                }
            }
            else if (!hasElements && !hasText && type == typeof(string))
            {
                // If we don't have text, and we're a string, return ""
                return "";
            }
            else if (!hasElements && !hasText && type.IsPrimitive)
            {
                // If we don't have text, and we're any primitive type, that's an error (and return default values I guess)
                Dbg.Err($"{element.LineNumber()}: Empty field provided for type {type}");
                return Activator.CreateInstance(type);
            }

            // We either have elements, or we're a composite type of some sort and can pretend we do

            // Special-case type testing here!

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

                    list.Add(ParseThing(fieldElement, referencedType, null));
                }

                return list;
            }

            if (type.IsArray)
            {
                // [] handling
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

                    array.SetValue(ParseThing(fieldElement, referencedType, null), i);
                }

                return array;
            }

            // End special-case testing; we're a generic class or a struct

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

                fieldInfo.SetValue(model, ParseThing(fieldElement, fieldInfo.FieldType, fieldInfo.GetValue(model)));
            }

            return model;
        }
    }
}