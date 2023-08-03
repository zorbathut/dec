namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal class ReaderFileDecXml : ReaderFileDec
    {
        public static ReaderFileDecXml Create(string input, string identifier)
        {
            XDocument doc;

            try
            {
                doc = XDocument.Parse(input, LoadOptions.SetLineInfo);
            }
            catch (System.Xml.XmlException e)
            {
                Dbg.Ex(e);
                return null;
            }

            var result = new ReaderFileDecXml();
            result.doc = doc;
            result.identifier = identifier;
            return result;
        }

        public override List<ReaderDec> ParseDecs()
        {
            if (doc.Elements().Count() > 1)
            {
                // This isn't testable, unfortunately; XDocument doesn't even support multiple root elements.
                Dbg.Err($"{identifier}: Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            var result = new List<ReaderDec>();

            foreach (var rootElement in doc.Elements())
            {
                var rootContext = new InputContext(identifier, rootElement);
                if (rootElement.Name.LocalName != "Decs")
                {
                    Dbg.Wrn($"{rootContext}: Found root element with name `{rootElement.Name.LocalName}` when it should be `Decs`");
                }

                foreach (var decElement in rootElement.Elements())
                {
                    var readerDec = new ReaderDec();

                    readerDec.inputContext = new InputContext(identifier, decElement);
                    string typeName = decElement.Name.LocalName;

                    readerDec.type = UtilType.ParseDecFormatted(typeName, readerDec.inputContext);
                    if (readerDec.type == null || !typeof(Dec).IsAssignableFrom(readerDec.type))
                    {
                        Dbg.Err($"{readerDec.inputContext}: {typeName} is not a valid root Dec type");
                        continue;
                    }

                    if (decElement.Attribute("decName") == null)
                    {
                        Dbg.Err($"{readerDec.inputContext}: No dec name provided, add a `decName=` attribute to the {typeName} tag (example: <{typeName} decName=\"TheNameOfYour{typeName}\">)");
                        continue;
                    }

                    readerDec.name = decElement.Attribute("decName").Value;
                    if (!Util.ValidateDecName(readerDec.name, readerDec.inputContext))
                    {
                        continue;
                    }

                    // Consume decName so we know it's not hanging around
                    decElement.Attribute("decName").Remove();

                    // Check to see if we're abstract
                    {
                        var abstractAttribute = decElement.Attribute("abstract");
                        if (abstractAttribute != null)
                        {
                            if (!bool.TryParse(abstractAttribute.Value, out readerDec.abstrct))
                            {
                                Dbg.Err($"{readerDec.inputContext}: Error encountered when parsing abstract attribute");
                            }

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
                    readerDec.node = new ReaderNodeXml(decElement);

                    result.Add(readerDec);
                }
            }

            return result;
        }

        private XDocument doc;
        private string identifier;
    }

    internal class ReaderFileRecorderXml : ReaderFileRecorder
    {
        public static ReaderFileRecorderXml Create(string input, string identifier)
        {
            XDocument doc;

            try
            {
                doc = XDocument.Parse(input, LoadOptions.SetLineInfo);
            }
            catch (System.Xml.XmlException e)
            {
                Dbg.Ex(e);
                return null;
            }

            if (doc.Elements().Count() > 1)
            {
                // This isn't testable, unfortunately; XDocument doesn't even support multiple root elements.
                Dbg.Err($"{identifier}: Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            var record = doc.Elements().First();
            if (record.Name.LocalName != "Record")
            {
                Dbg.Wrn($"{new InputContext(identifier, record)}: Found root element with name `{record.Name.LocalName}` when it should be `Record`");
            }

            var recordFormatVersion = record.ElementNamed("recordFormatVersion");
            if (recordFormatVersion == null)
            {
                Dbg.Err($"{new InputContext(identifier, record)}: Missing record format version, assuming the data is up-to-date");
            }
            else if (recordFormatVersion.GetText() != "1")
            {
                Dbg.Err($"{new InputContext(identifier, recordFormatVersion)}: Unknown record format version {recordFormatVersion.GetText()}, expected 1 or earlier");

                // I would rather not guess about this
                return null;
            }

            var result = new ReaderFileRecorderXml();
            result.record = record;
            result.identifier = identifier;

            return result;
        }

        public override List<ReaderRef> ParseRefs()
        {
            var result = new List<ReaderRef>();

            var refs = record.ElementNamed("refs");
            if (refs != null)
            {
                foreach (var reference in refs.Elements())
                {
                    var readerRef = new ReaderRef();

                    var context = new InputContext(identifier, reference);

                    if (reference.Name.LocalName != "Ref")
                    {
                        Dbg.Wrn($"{context}: Reference element should be named 'Ref'");
                    }

                    readerRef.id = reference.Attribute("id")?.Value;
                    if (readerRef.id == null)
                    {
                        Dbg.Err($"{context}: Missing reference ID");
                        continue;
                    }

                    // Further steps don't know how to deal with this, so we just strip it
                    reference.Attribute("id").Remove();

                    var className = reference.Attribute("class")?.Value;
                    if (className == null)
                    {
                        Dbg.Err($"{context}: Missing reference class name");
                        continue;
                    }

                    readerRef.type = (Type)Serialization.ParseString(className, typeof(Type), null, context);
                    if (readerRef.type.IsValueType)
                    {
                        Dbg.Err($"{context}: Reference assigned type {readerRef.type}, which is a value type");
                        continue;
                    }

                    readerRef.node = new ReaderNodeXml(reference);
                    result.Add(readerRef);
                }
            }

            return result;
        }

        public override ReaderNode ParseNode()
        {
            var data = record.ElementNamed("data");
            if (data == null)
            {
                Dbg.Err($"{new InputContext(identifier, record)}: No data element provided. This is not very recoverable.");

                return null;
            }

            return new ReaderNodeXml(data);
        }

        private XElement record;
        private string identifier;
    }

    internal class ReaderNodeXml : ReaderNode
    {
        public ReaderNodeXml(XElement xml)
        {
            this.xml = xml;
        }

        public override XElement HackyExtractXml()
        {
            return xml;
        }

        private XElement xml;
    }
}
