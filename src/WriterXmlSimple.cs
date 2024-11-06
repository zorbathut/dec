using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Dec
{
    internal class WriterXmlSimple : WriterXml
    {
        public override bool AllowReflection { get => false; }
        public override bool AllowDecPath { get => true; }  // . . . sure, I guess?
        public override Recorder.IUserSettings UserSettings { get; }

        private Dictionary<object, Path> seenObjects = new Dictionary<object, Path>();

        private XDocument doc;
        private string rootTag;

        public WriterXmlSimple(string rootTag, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;

            doc = new XDocument();
            this.rootTag = rootTag;
        }

        public override bool RegisterReference(object referenced, XElement element, Recorder.Settings recSettings, Path path)
        {
            if (!seenObjects.TryAdd(referenced, path))
            {
                Dbg.Err($"{recSettings}: Object {referenced} at [{path.Serialize()}] has already been written from [{seenObjects[referenced].Serialize()}], and shared objects do not work in simple mode. Skipping to avoid infinite loops.");
                return true;
            }

            return false;
        }

        public WriterNodeXml StartRecord(Type type, string pathId)
        {
            var node = WriterNodeXml.StartRecord(this, doc, rootTag, type, pathId);
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
