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
    internal static class ReaderFileXmlSimple
    {
        public static ReaderNodeParseable Create(string input, string rootTag, string identifier, Recorder.IUserSettings userSettings)
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
            if (record.Name.LocalName != rootTag)
            {
                Dbg.Wrn($"{new InputContext(identifier, record)}: Found root element with name `{record.Name.LocalName}` when it should be `{rootTag}`");
            }

            return new ReaderNodeXml(record, identifier, userSettings);
        }
    }
}
