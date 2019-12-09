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

            // Write our references!
            if (writerContext.HasReferences())
            {
                // Canonical ordering to provide some stability and ease-of-reading.
                foreach (var reference in writerContext.StripAndOutputReferences().OrderBy(kvp => kvp.Key))
                {
                    refs.Add(reference.Value);
                }
            }
            else
            {
                // strip out the refs 'cause it looks better that way :V
                refs.Remove();
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

            if (refs != null)
            {
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

                    // Create a stub so other things can reference it later
                    readerContext.refs[id] = Activator.CreateInstance(possibleType);
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

                    // The serialization routines don't know how to deal with this, so we'll remove it now
                    reference.Attribute("id").Remove();

                    var refInstance = readerContext.refs[id];

                    // Do our actual parsing
                    var refInstanceOutput = Serialization.ParseElement(reference, refInstance.GetType(), refInstance, readerContext, hasReferenceId: true);

                    if (refInstance != refInstanceOutput)
                    {
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Something really bizarre has happened and we got the wrong object back. Things are probably irrevocably broken. Please report this as a bug in Def.");
                        continue;
                    }
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
        // A map from object to the in-place element. This does *not* yet have the ref ID tagged, and will have to be extracted into a new Element later.
        private Dictionary<object, XElement> refToElement = new Dictionary<object, XElement>();

        // A map from object to the string intended as a reference. This will be filled in only once a second reference to something is created.
        private Dictionary<object, string> refToString = new Dictionary<object, string>();

        public bool RegisterReference(object referenced, XElement element)
        {
            if (!refToElement.ContainsKey(referenced))
            {
                // Insert it into our refToElement mapping
                refToElement[referenced] = element;

                // We still need this to be generated, so we'll just let that happen now
                return false;
            }

            var refId = refToString.TryGetValue(referenced);
            if (refId == null)
            {
                // We already had a reference, but we don't have a string ID for it. We need one now though!
                refId = $"ref{refToString.Count:D5}";
                refToString[referenced] = refId;
            }

            // Tag the XML element properly
            element.SetAttributeValue("ref", refId);

            // And we're done!
            return true;
        }

        public bool HasReferences()
        {
            return refToString.Any();
        }

        public IEnumerable<KeyValuePair<string, XElement>> StripAndOutputReferences()
        {
            // It is *vitally* important that we do this step *after* all references are generated, not inline as we add references.
            // This is because we have to move all the contents of the XML element, but if we do it during generation, a recursive-reference situation could result in us trying to move the contents before the XML element is fully generated.
            // So we do it now, when we know that everything is finished.
            foreach (var refblock in refToString)
            {
                var result = new XElement("Ref");
                result.SetAttributeValue("id", refblock.Value);

                var src = refToElement[refblock.Key];

                // gotta ToArray() because it does not like mutating things while iterating
                // And yes, you have to .Remove() also, otherwise you get copies in both places.
                foreach (var attribute in src.Attributes().ToArray())
                {
                    attribute.Remove();
                    result.Add(attribute);
                }

                foreach (var node in src.Nodes().ToArray())
                {
                    node.Remove();
                    result.Add(node);
                }

                // Patch in the ref link
                src.SetAttributeValue("ref", refblock.Value);

                // We may not have had a class to begin with, but we sure need one now!
                result.SetAttributeValue("class", refblock.Key.GetType().ToStringDefFormatted());

                yield return new KeyValuePair<string, XElement>(refblock.Value, result);
            }
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

            element.Add(Serialization.ComposeElement(value, typeof(T), label, context));
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
            value = (T)Serialization.ParseElement(recorded, typeof(T), value, context);
        }

        public override XElement Xml { get => element; }
        public override Direction Mode { get => Direction.Read; }
    }
}
