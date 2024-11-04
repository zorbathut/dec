using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Dec
{
    internal class ReaderFileDecXml : ReaderFileDec
    {
        private XDocument doc;
        private string fileIdentifier;
        private Recorder.IUserSettings userSettings;

        public static ReaderFileDecXml Create(TextReader input, string identifier, Recorder.IUserSettings userSettings)
        {
            XDocument doc = UtilXml.ParseSafely(input);
            if (doc == null)
            {
                return null;
            }

            var result = new ReaderFileDecXml();
            result.doc = doc;
            result.fileIdentifier = identifier;
            result.userSettings = userSettings;
            return result;
        }

        public override List<ReaderDec> ParseDecs()
        {
            if (doc.Elements().Count() > 1)
            {
                // This isn't testable, unfortunately; XDocument doesn't even support multiple root elements.
                Dbg.Err($"{fileIdentifier}: Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            var result = new List<ReaderDec>();

            foreach (var rootElement in doc.Elements())
            {
                var rootContext = new InputContext(fileIdentifier, rootElement);
                if (rootElement.Name.LocalName != "Decs")
                {
                    Dbg.Wrn($"{rootContext}: Found root element with name `{rootElement.Name.LocalName}` when it should be `Decs`");
                }

                foreach (var decElement in rootElement.Elements())
                {
                    var readerDec = new ReaderDec();

                    readerDec.inputContext = new InputContext(fileIdentifier, decElement);
                    string typeName = decElement.Name.LocalName;

                    readerDec.type = UtilType.ParseDecFormatted(typeName, readerDec.inputContext);
                    if (readerDec.type == null || !typeof(Dec).IsAssignableFrom(readerDec.type))
                    {
                        Dbg.Err($"{readerDec.inputContext}: {typeName} is being used as a Dec but does not inherit from Dec.Dec");
                        continue;
                    }

                    var decNameAttribute = decElement.Attribute("decName");
                    if (decNameAttribute == null)
                    {
                        Dbg.Err($"{readerDec.inputContext}: No dec name provided, add a `decName=` attribute to the {typeName} tag (example: <{typeName} decName=\"TheNameOfYour{typeName}\">)");
                        continue;
                    }

                    readerDec.name = decNameAttribute.Value;
                    if (!UtilMisc.ValidateDecName(readerDec.name, readerDec.inputContext))
                    {
                        continue;
                    }

                    // Consume decName so we know it's not hanging around
                    decNameAttribute.Remove();

                    // Parse `class` if we can
                    if (decElement.Attribute("class") is var classAttribute && classAttribute != null)
                    {
                        var parsedClass = (Type)Serialization.ParseString(classAttribute.Value,
                            typeof(Type), null, readerDec.inputContext);

                        if (parsedClass == null)
                        {
                            // we have presumably already reported an error
                        }
                        else if (!readerDec.type.IsAssignableFrom(parsedClass))
                        {
                            Dbg.Err($"{readerDec.inputContext}: Attribute-parsed class {parsedClass} is not a subclass of {readerDec.type}; using the original class");
                        }
                        else
                        {
                            // yay
                            readerDec.type = parsedClass;
                        }

                        // clean up
                        classAttribute.Remove();
                    }

                    // Check to see if we're abstract
                    {
                        var abstractAttribute = decElement.Attribute("abstract");
                        if (abstractAttribute != null)
                        {
                            if (!bool.TryParse(abstractAttribute.Value, out bool abstrct))
                            {
                                Dbg.Err($"{readerDec.inputContext}: Error encountered when parsing abstract attribute");
                            }
                            readerDec.abstrct = abstrct; // little dance to deal with the fact that readerDec.abstrct is a `bool?`

                            abstractAttribute.Remove();
                        }
                    }

                    // Get our parent info
                    {
                        var parentAttribute = decElement.Attribute("parent");
                        if (parentAttribute != null)
                        {
                            readerDec.parent = parentAttribute.Value;

                            parentAttribute.Remove();
                        }
                    }

                    // Everything looks good!
                    readerDec.node = new ReaderNodeXml(decElement, fileIdentifier, userSettings);

                    result.Add(readerDec);
                }
            }

            return result;
        }
    }
}
