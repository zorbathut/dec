using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Dec
{
    internal class WriterXmlSimple : WriterXml
    {
        public override bool AllowReflection { get => false; }
        public override Recorder.IUserSettings UserSettings { get; }

        private HashSet<object> seenObjects = new HashSet<object>();

        private XDocument doc;
        private string rootTag;

        public WriterXmlSimple(string rootTag, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;

            doc = new XDocument();
            this.rootTag = rootTag;
        }

        public override bool RegisterReference(object referenced, XElement element, Recorder.Context recContext)
        {
            if (!seenObjects.Add(referenced))
            {
                Dbg.Err($"{recContext}: Object {referenced} has already been written, and shared objects do not work in simple mode. Skipping to avoid infinite loops.");
                return true;
            }

            return false;
        }

        public WriterNodeXml StartData(Type type)
        {
            var node = WriterNodeXml.StartData(this, doc, rootTag, type);
            return node;
        }

        public string Finish(bool pretty)
        {
            // Handle all our pending writes
            DequeuePendingWrites();

            return doc.ToString();
        }
    }
}
