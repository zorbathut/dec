using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Dec
{
    internal class ReaderFileRecorderXml : ReaderFileRecorder
    {
        private XElement record;
        private string fileIdentifier;
        private Recorder.IUserSettings userSettings;

        public static ReaderFileRecorderXml Create(string input, string identifier, Recorder.IUserSettings userSettings)
        {
            XDocument doc = UtilXml.ParseSafely(new System.IO.StringReader(input));
            if (doc == null)
            {
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
            result.fileIdentifier = identifier;
            result.userSettings = userSettings;

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

                    var context = new InputContext(fileIdentifier, reference);

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

                    readerRef.node = new ReaderNodeXml(reference, fileIdentifier, userSettings);
                    result.Add(readerRef);
                }
            }

            return result;
        }

        public override ReaderNodeParseable ParseNode()
        {
            var data = record.ElementNamed("data");
            if (data == null)
            {
                Dbg.Err($"{new InputContext(fileIdentifier, record)}: No data element provided. This is not very recoverable.");

                return null;
            }

            return new ReaderNodeXml(data, fileIdentifier, userSettings);
        }
    }
}
