

using System;
using System.Xml.Linq;

namespace Dec
{
    internal class WriterXmlCompose : WriterXml
    {
        public override bool AllowReflection { get => true; }
        public override Recorder.IUserSettings UserSettings { get; }

        private XDocument doc;
        private XElement decs;

        public WriterXmlCompose(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;

            doc = new XDocument();

            decs = new XElement("Decs");
            doc.Add(decs);
        }

        public override bool RegisterReference(object referenced, XElement element, Recorder.Context recContext)
        {
            // We never register references in Compose mode.
            return false;
        }

        public WriterNode StartDec(Type type, string decName)
        {
            string typeName;
            Type overrideClass = null;
            if (type.IsGenericType)
            {
                // okay, this just got more complicated
                // we need to find a dec subclass that doesn't have any generics, then attach an attribute
                // this is . . . not great design, honestly, I'm not sure I like this format right now
                // but, fine, it's ugly but it will work
                Type baseType = type.BaseType;
                while (baseType.IsGenericType)
                {
                    baseType = baseType.BaseType;
                }

                typeName = baseType.ComposeDecFormatted();
                overrideClass = type;
            }
            else
            {
                typeName = type.ComposeDecFormatted();
            }

            var nodeXml = WriterNodeXml.StartDec(this, decs, typeName, decName);

            if (overrideClass != null)
            {
                nodeXml.TagClass(overrideClass);
            }

            return nodeXml;
        }

        public string Finish(bool pretty)
        {
            DequeuePendingWrites();

            return doc.ToString(pretty ? SaveOptions.None : SaveOptions.DisableFormatting);
        }
    }
}
