namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public interface IRecordable
    {
        void Record(Recorder record);
    }

    public abstract class Recorder
    {
        public abstract void Record(ref int value, string label);
        public abstract void Record(ref float value, string label);
        public abstract void Record(ref bool value, string label);
        public abstract void Record(ref string value, string label);

        public static string Write(IRecordable recordable, bool pretty = true)
        {
            var doc = new XDocument();

            var record = new XElement("Record");
            doc.Add(record);

            record.Add(new XElement("recordFormatVersion", 1));

            var refs = new XElement("refs");
            record.Add(refs);

            var data = new XElement("data");
            record.Add(data);

            recordable.Record(new RecorderWriter(data));

            return doc.ToString();
        }

        public static T Read<T>(string input, string stringName = "input") where T : IRecordable, new()
        {
            var doc = XDocument.Parse(input, LoadOptions.SetLineInfo);

            if (doc.Elements().Count() > 1)
            {
                Dbg.Err($"{stringName}: Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            var record = doc.Elements().First();
            if (record.Name.LocalName != "Record")
            {
                Dbg.Wrn($"{stringName}:{record.LineNumber()}: Found root element with name \"{record.Name.LocalName}\" when it should be \"Record\"");
            }

            var recordFormatVersion = record.ElementNamed("recordFormatVersion");
            if (recordFormatVersion.GetText() != "1")
            {
                Dbg.Wrn($"{stringName}:{recordFormatVersion.LineNumber()}: Unknown record format version {recordFormatVersion.GetText()}, expected 1 or earlier");
            }

            var refs = record.ElementNamed("refs");

            var result = new T();
            result.Record(new RecorderReader(record.ElementNamed("data"), stringName));
            return result;
        }
    }

    public class RecorderWriter : Recorder
    {
        private readonly XElement element;
        private readonly HashSet<string> fields = new HashSet<string>();

        internal RecorderWriter(XElement element)
        {
            this.element = element;
        }

        public override void Record(ref int value, string label)
        {
            WriteField(label, value.ToString());
        }

        public override void Record(ref float value, string label)
        {
            WriteField(label, value.ToString());
        }

        public override void Record(ref bool value, string label)
        {
            WriteField(label, value.ToString());
        }

        public override void Record(ref string value, string label)
        {
            WriteField(label, value);
        }

        private void WriteField(string label, string value)
        {
            if (fields.Contains(label))
            {
                Dbg.Err($"Field '{label}' written multiple times");
                return;
            }

            fields.Add(label);
            element.Add(new XElement(label, value));
        }
    }

    public class RecorderReader : Recorder
    {
        private readonly XElement element;
        private readonly string docName;

        internal RecorderReader(XElement element, string docName)
        {
            this.element = element;
            this.docName = docName;
        }

        public override void Record(ref int value, string label)
        {
            value = ReadField(label, value);
        }

        public override void Record(ref float value, string label)
        {
            value = ReadField(label, value);
        }

        public override void Record(ref bool value, string label)
        {
            value = ReadField(label, value);
        }

        public override void Record(ref string value, string label)
        {
            value = ReadField(label, value);
        }

        private T ReadField<T>(string label, T def)
        {
            var recorded = element.ElementNamed(label);
            if (recorded == null)
            {
                return def;
            }

            var result = Serialization.ParseElement(recorded, typeof(T), null, false, docName);
            if (result == null)
            {
                return def;
            }

            return (T)result;
        }
    }
}
