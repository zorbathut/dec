namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Linq;
    using System.Text.RegularExpressions;

    public class Parser
    {
        private static readonly Regex DefNameValidator = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public void ParseFromString(string input, Type[] types)
        {
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

            var typeLookup = new Dictionary<string, Type>();
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

        private object ParseThing(XElement element, Type type, object model)
        {
            // TODO: verify we don't have both text and elements

            bool hasElements = element.Elements().Any();
            bool hasText = element.Nodes().OfType<XText>().Any();

            if (!hasElements && hasText)
            {
                return TypeDescriptor.GetConverter(type).ConvertFromString((element.FirstNode as XText).Value);
            }
            else if (!hasElements && !hasText && type == typeof(string))
            {
                // TODO: Error on types where we don't accept "empty"
                return TypeDescriptor.GetConverter(type).ConvertFromString("");
            }

            // We definitely have elements; treat this like a composite

            if (model == null)
            {
                model = Activator.CreateInstance(type);
            }

            foreach (var fieldElement in element.Elements())
            {
                // TODO: verify we don't have duplicates

                // TODO: handle private members of parent classes
                var fieldInfo = type.GetField(fieldElement.Name.LocalName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // TODO: verify it exists

                fieldInfo.SetValue(model, ParseThing(fieldElement, fieldInfo.FieldType, fieldInfo.GetValue(model)));
            }

            return model;
        }
    }
}