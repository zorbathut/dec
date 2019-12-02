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

        // This handles conversions, defs, and IRecordables.
        // I am not happy with this, but I can't think of a better way to handle it.
        public abstract void Record<T>(ref T value, string label);

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

            var writerContext = new WriterContext();

            recordable.Record(new RecorderWriter(data, writerContext));

            // We have a bunch of refs that need to be written and we're going to deal with them here
            // However, this is iffy because we may actually have *more* refs to output once we're done with the first batch.
            // Keep repeating until we run out of new refs.
            while (writerContext.refsToWrite.Any())
            {
                // Clear out our list so we won't lose any while we're iterating over our current list.
                var refsToWrite = writerContext.refsToWrite;
                writerContext.refsToWrite = new HashSet<IRecordable>();

                // Canonical ordering to provide some stability and ease-of-reading.
                foreach (var reference in refsToWrite.OrderBy(reference => writerContext.refs[reference]))
                {
                    var element = new XElement("Ref");
                    refs.Add(element);

                    element.SetAttributeValue("id", writerContext.refs[reference]);
                    element.SetAttributeValue("class", reference.GetType().ToString());

                    // This may fill out more refsToWrite; if so, we'll get to them later.
                    reference.Record(new RecorderWriter(element, writerContext));
                }
            }

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
                Dbg.Err($"{stringName}:{recordFormatVersion.LineNumber()}: Unknown record format version {recordFormatVersion.GetText()}, expected 1 or earlier");
            }

            var refs = record.ElementNamed("refs");

            var readerContext = new ReaderContext();
            readerContext.docName = stringName;

            // First, we need to make the instances for all the references, so they can be crosslinked appropriately
            foreach (var reference in refs.Elements())
            {
                if (reference.Name.LocalName != "Ref")
                {
                    Dbg.Wrn($"{stringName}:{reference.LineNumber()}: Reference element should be named 'Ref'");
                }

                var id = reference.Attribute("id")?.Value;
                if (id == null)
                {
                    Dbg.Err($"{stringName}:{reference.LineNumber()}: Missing reference ID");
                    continue;
                }

                var className = reference.Attribute("class")?.Value;
                if (className == null)
                {
                    Dbg.Err($"{stringName}:{reference.LineNumber()}: Missing reference class name");
                    continue;
                }

                var possibleType = (Type)Serialization.ParseString(className, typeof(Type), stringName, reference.LineNumber());
                if (!typeof(IRecordable).IsAssignableFrom(possibleType))
                {
                    Dbg.Err($"{stringName}:{reference.LineNumber()}: Reference assigned type {possibleType}, which is not recordable");
                    continue;
                }

                readerContext.refs[id] = (IRecordable)Activator.CreateInstance(possibleType);
                if (readerContext.refs[id] == null)
                {
                    Dbg.Err($"{stringName}:{reference.LineNumber()}: Reference of type {possibleType} was not properly created; this will cause issues");
                    continue;
                }
            }

            // Now that all the refs exist, we can run through them again and actually parse them
            foreach (var reference in refs.Elements())
            {
                var id = reference.Attribute("id")?.Value;

                // The serialization routines don't know how to deal with this, so we'll just remove it
                reference.Attribute("id").Remove();

                var refInstance = readerContext.refs[id];

                // Do our actual parsing
                var refInstanceOutput = Serialization.ParseElement(reference, refInstance.GetType(), refInstance, false, stringName);

                if (refInstance != refInstanceOutput)
                {
                    Dbg.Err($"{stringName}:{reference.LineNumber()}: Something really bizarre has happened and we got the wrong object back. Things are probably irrevocably broken. Please report this as a bug in Def.");
                    continue;
                }
            }

            // And now, we can finally parse our actual root element!
            // (which accounts for a tiny percentage of things that need to be parsed)
            var result = new T();
            result.Record(new RecorderReader(record.ElementNamed("data"), readerContext));
            return result;
        }
    }

    public class WriterContext
    {
        // This is a full lookup of all references that have been encountered.
        public Dictionary<IRecordable, string> refs = new Dictionary<IRecordable, string>();

        // This is a partial list of just the references that have been added since the last clear.
        public HashSet<IRecordable> refsToWrite = new HashSet<IRecordable>();
    }

    public class RecorderWriter : Recorder
    {
        private readonly XElement element;
        private readonly HashSet<string> fields = new HashSet<string>();
        private readonly WriterContext context;

        internal RecorderWriter(XElement element, WriterContext context)
        {
            this.element = element;
            this.context = context;
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

        public override void Record<T>(ref T value, string label)
        {
            if (fields.Contains(label))
            {
                Dbg.Err($"Field '{label}' written multiple times");
                return;
            }

            fields.Add(label);

            var recorded = new XElement(label);
            element.Add(recorded);

            // See if this is a def
            if (typeof(T).IsSubclassOf(typeof(Def)))
            {
                // It is! Let's just get the def name and be done with it.
                if (value != null)
                {
                    var valueDef = value as Def;

                    recorded.Add(new XText(valueDef.defName));
                }
            }
            else if (typeof(IRecordable).IsAssignableFrom(typeof(T)))
            {
                // It's a recordable, so we're going to store a reference
                if (value != null)
                {
                    var recordable = value as IRecordable;
                    var refid = context.refs.TryGetValue(recordable);
                    if (refid == null)
                    {
                        refid = $"ref{context.refs.Count:D5}";
                        context.refs[recordable] = refid;
                        context.refsToWrite.Add(recordable);
                    }

                    recorded.SetAttributeValue("ref", refid);
                }
                else
                {
                    recorded.SetAttributeValue("null", "true");
                }
            }
            else
            {
                // Look for a converter; that's the only way we're going to handle this one!
                var converter = Serialization.Converters.TryGetValue(typeof(T));
                if (converter == null)
                {
                    Dbg.Err($"Couldn't find a converter for type {typeof(T)}");
                    return;
                }

                converter.ToXml(value, recorded);
            }
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

    internal class ReaderContext
    {
        public string docName;
        public Dictionary<string, IRecordable> refs = new Dictionary<string, IRecordable>();
    }

    public class RecorderReader : Recorder
    {
        private readonly XElement element;
        private readonly ReaderContext context;

        internal RecorderReader(XElement element, ReaderContext context)
        {
            this.element = element;
            this.context = context;
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

        public override void Record<T>(ref T value, string label)
        {
            var recorded = element.ElementNamed(label);
            if (recorded == null)
            {
                return;
            }

            if (typeof(IRecordable).IsAssignableFrom(typeof(T)))
            {
                // This is a reference!

                if (recorded.Attribute("null")?.Value == "true")
                {
                    // This is null, it's just easier to write this way here in Genericland.
                    value = default(T);
                    return;
                }

                var refId = recorded.Attribute("ref")?.Value;
                if (refId == null)
                {
                    Dbg.Err($"Reference stored with neither null nor ref");
                    // Just leave it alone; hopefully its default value is sensible!
                    return;
                }

                var reference = context.refs.TryGetValue(refId);
                if (reference == null)
                {
                    Dbg.Err($"File referred to unrecognized reference ID {refId}");
                    // Just leave it alone; hopefully its default value is sensible!
                    return;
                }

                if (!(reference is T))
                {
                    Dbg.Err($"File referred to reference ID {refId} of type {value.GetType()}, but expected type {typeof(T)}");
                    // Just leave it alone; hopefully its default value is sensible!
                    return;
                }

                value = (T)reference;
                return;
            }
            else
            {
                // Def or convertable, we hope
                value = ReadField(label, value);
            }
        }

        private T ReadField<T>(string label, T def)
        {
            var recorded = element.ElementNamed(label);
            if (recorded == null)
            {
                return def;
            }

            var result = Serialization.ParseElement(recorded, typeof(T), null, false, context.docName);
            if (result == null)
            {
                return def;
            }

            return (T)result;
        }
    }
}
