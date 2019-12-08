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
        // This handles literally everything. I wish I could do more validation at compiletime, but the existence of Converters makes that impossible.
        public abstract void Record<T>(ref T value, string label);

        public abstract XElement Xml { get; }

        public enum Direction
        {
            Read,
            Write,
        }
        public abstract Direction Mode { get; }

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
            while (writerContext.HasRefsToWrite())
            {
                // Clear out our list so we won't lose any while we're iterating over our current list.
                var refsToWrite = writerContext.ConsumeRefsToWrite();

                // Canonical ordering to provide some stability and ease-of-reading.
                foreach (var reference in refsToWrite.OrderBy(reference => writerContext.GetRef(reference)))
                {
                    var element = Serialization.ComposeElement(reference, reference.GetType(), "Ref", writerContext, true);
                    refs.Add(element);

                    element.SetAttributeValue("id", writerContext.GetRef(reference));
                    element.SetAttributeValue("class", reference.GetType().ToStringDefFormatted());
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

            var readerContext = new ReaderContext(stringName, true);

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
                if (possibleType.IsValueType)
                {
                    Dbg.Err($"{stringName}:{reference.LineNumber()}: Reference assigned type {possibleType}, which is a value type");
                    continue;
                }

                if (Serialization.Converters.ContainsKey(possibleType))
                {
                    // We have a converter, so we just go ahead and make it instead of bothering with the two-pass method.
                    // This is because converters actually can't reference other things, so it's safe. However, they also can return unpredictable data types, so we can't make a placeholder.

                    // Remove the id so ParseElement doesn't choke.
                    reference.Attribute("id").Remove();

                    // I'm not totally sure why I'm using ParseElement here instead of calling the converter directly, except for a deep feeling that I'll regret it if I go through a nonstandard pathway.
                    readerContext.refs[id] = Serialization.ParseElement(reference, possibleType, null, false, readerContext);
                    if (readerContext.refs[id].GetType() != possibleType)
                    {
                        Dbg.Wrn($"{stringName}:{reference.LineNumber()}: Converter for type {possibleType} returned an unexpected {readerContext.refs[id].GetType()} instead");
                    }
                }
                else
                {
                    // We don't have a converter, so we create a stub so other things can reference it later
                    readerContext.refs[id] = Activator.CreateInstance(possibleType);
                    if (readerContext.refs[id] == null)
                    {
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Reference of type {possibleType} was not properly created; this will cause issues");
                        continue;
                    }
                }
            }

            // Now that all the refs exist, we can run through them again and actually parse them
            foreach (var reference in refs.Elements())
            {
                var id = reference.Attribute("id")?.Value;

                // If we don't have an ID, then we've already eaten it due to using a converter. And that means we're done, just move on.
                if (id == null)
                {
                    continue;
                }

                // The serialization routines don't know how to deal with this, so we'll remove it now
                reference.Attribute("id").Remove();

                var refInstance = readerContext.refs[id];
                
                // Do our actual parsing
                var refInstanceOutput = Serialization.ParseElement(reference, refInstance.GetType(), refInstance, false, readerContext);

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

    internal class WriterContext
    {
        private Dictionary<object, string> refs = new Dictionary<object, string>();
        private List<object> refsToWrite = new List<object>();

        public string GetRef(object referenced)
        {
            var refid = refs.TryGetValue(referenced);
            if (refid == null)
            {
                refid = $"ref{refs.Count:D5}";
                refs[referenced] = refid;
                refsToWrite.Add(referenced);
            }

            return refid;
        }

        public bool HasRefsToWrite()
        {
            return refsToWrite.Any();
        }

        public List<object> ConsumeRefsToWrite()
        {
            var result = refsToWrite;
            refsToWrite = new List<object>();
            return result;
        }
    }

    internal class RecorderWriter : Recorder
    {
        private readonly XElement element;
        private readonly HashSet<string> fields = new HashSet<string>();
        private readonly WriterContext context;

        internal RecorderWriter(XElement element, WriterContext context)
        {
            this.element = element;
            this.context = context;
        }

        public override void Record<T>(ref T value, string label)
        {
            if (fields.Contains(label))
            {
                Dbg.Err($"Field '{label}' written multiple times");
                return;
            }

            fields.Add(label);

            element.Add(Serialization.ComposeElement(value, typeof(T), label, context, false));
        }

        public override XElement Xml { get => element; }
        public override Direction Mode { get => Direction.Write; }
    }

    internal class ReaderContext
    {
        public string sourceName;
        public Dictionary<string, object> refs;

        public bool Record { get => refs != null; }

        public ReaderContext(string sourceName, bool withRefs)
        {
            this.sourceName = sourceName;

            if (withRefs)
            {
                refs = new Dictionary<string, object>();
            }
        }
    }

    public class RecorderReader : Recorder
    {
        private readonly XElement element;
        private readonly ReaderContext context;

        public string SourceName { get => context.sourceName; }

        internal RecorderReader(XElement element, ReaderContext context)
        {
            this.element = element;
            this.context = context;
        }

        public override void Record<T>(ref T value, string label)
        {
            var recorded = element.ElementNamed(label);
            if (recorded == null)
            {
                return;
            }

            // Explicit cast here because we want an error if we have the wrong type!
            value = (T)Serialization.ParseElement(recorded, typeof(T), value, false, context);
        }

        public override XElement Xml { get => element; }
        public override Direction Mode { get => Direction.Read; }
    }
}
