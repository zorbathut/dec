namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal abstract class WriterXml : Writer
    {
        // A list of writes that still have to happen. This is used so we don't have to do deep recursive dives and potentially blow our stack.
        private List<Action> pendingWrites = new List<Action>();

        public abstract bool RegisterReference(object referenced, XElement element);

        public void RegisterPendingWrite(Action action)
        {
            pendingWrites.Add(action);
        }

        public void DequeuePendingWrites()
        {
            while (DequeuePendingWrite() is var pending && pending != null)
            {
                pending();
            }
        }

        private Action DequeuePendingWrite()
        {
            if (pendingWrites.Count == 0)
            {
                return null;
            }

            var result = pendingWrites[pendingWrites.Count - 1];
            pendingWrites.RemoveAt(pendingWrites.Count - 1);
            return result;
        }
    }

    internal class WriterXmlCompose : WriterXml
    {
        public override bool AllowReflection { get => true; }

        private XDocument doc;
        private XElement defs;

        public WriterXmlCompose()
        {
            doc = new XDocument();

            defs = new XElement("Defs");
            doc.Add(defs);
        }

        public override bool RegisterReference(object referenced, XElement element)
        {
            // We never register references in Compose mode.
            return false;
        }

        public WriterNode StartDef(Type type, string defName)
        {
            return WriterNodeXml.StartDef(this, defs, type.ComposeDefFormatted(), defName);
        }

        public string Finish()
        {
            DequeuePendingWrites();

            return doc.ToString();
        }
    }

    internal class WriterXmlRecord : WriterXml
    {
        public override bool AllowReflection { get => false; }

        // Maps between object and the in-place element. This does *not* yet have the ref ID tagged, and will have to be extracted into a new Element later.
        private Dictionary<object, XElement> refToElement = new Dictionary<object, XElement>();
        private Dictionary<XElement, object> elementToRef = new Dictionary<XElement, object>();

        // A map from object to the string intended as a reference. This will be filled in only once a second reference to something is created.
        // This is cleared after we resolve references, then re-used for the depth capping code.
        private Dictionary<object, string> refToString = new Dictionary<object, string>();

        // Current reference ID that we're on.
        private int referenceId = 0;

        private XDocument doc;
        private XElement record;
        private XElement refs;
        private XElement rootElement;

        public WriterXmlRecord()
        {
            doc = new XDocument();

            record = new XElement("Record");
            doc.Add(record);

            record.Add(new XElement("recordFormatVersion", 1));

            refs = new XElement("refs");
            record.Add(refs);
        }

        public override bool RegisterReference(object referenced, XElement element)
        {
            if (!refToElement.ContainsKey(referenced))
            {
                // Insert it into our refToElement mapping
                refToElement[referenced] = element;
                elementToRef[element] = referenced;

                // We still need this to be generated, so we'll just let that happen now
                return false;
            }

            var refId = refToString.TryGetValue(referenced);
            if (refId == null)
            {
                // We already had a reference, but we don't have a string ID for it. We need one now though!
                refId = $"ref{referenceId++:D5}";
                refToString[referenced] = refId;
            }

            // Tag the XML element properly
            element.SetAttributeValue("ref", refId);

            // And we're done!
            return true;
        }

        public IEnumerable<KeyValuePair<string, XElement>> StripAndOutputReferences()
        {
            // It is *vitally* important that we do this step *after* all references are generated, not inline as we add references.
            // This is because we have to move all the contents of the XML element, but if we do it during generation, a recursive-reference situation could result in us trying to move the XML element before its contents are fully generated.
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
                result.SetAttributeValue("class", refblock.Key.GetType().ComposeDefFormatted());

                yield return new KeyValuePair<string, XElement>(refblock.Value, result);
            }

            // We're now done processing this segment and can erase it; we don't want to try doing this a second time!
            refToString.Clear();
        }

        public bool ProcessDepthLimitedReferences(XElement node, int depthRemaining)
        {
            if (depthRemaining <= 0 && elementToRef.ContainsKey(node))
            {
                refToString[elementToRef[node]] = $"ref{referenceId++:D5}";
                // We don't continue recursively because then we're threatening a stack overflow; we'll get it on the next pass

                return true;
            }
            else
            {
                bool found = false;
                foreach (var child in node.Elements())
                {
                    found |= ProcessDepthLimitedReferences(child, depthRemaining - 1);
                }

                return found;
            }
        }

        public WriterNodeXml StartData()
        {
            var node = WriterNodeXml.StartData(this, record, "data");
            rootElement = node.GetXElement();
            return node;
        }

        public string Finish()
        {
            // Handle all our pending writes
            DequeuePendingWrites();

            // We now have a giant XML tree, potentially many thousands of nodes deep, where some nodes are references and some *should* be in the reference bank but aren't.
            // We need to do two things:
            // * Make all of our tagged references into actual references in the Refs section
            // * Tag anything deeper than a certain depth as a reference, then move it into the Refs section
            var depthTestsPending = new List<XElement>();
            depthTestsPending.Add(rootElement);

            // This is a loop between "write references" and "tag everything below a certain depth as needing to be turned into a reference".
            // We do this in a loop so we don't have to worry about ironically blowing our stack while making a change required to not blow our stack.
            while (true)
            {
                // Canonical ordering to provide some stability and ease-of-reading.
                foreach (var reference in StripAndOutputReferences().OrderBy(kvp => kvp.Key))
                {
                    refs.Add(reference.Value);
                    depthTestsPending.Add(reference.Value);
                }

                bool found = false;
                for (int i = 0; i < depthTestsPending.Count; ++i)
                {
                    // Magic number should probably be configurable at some point
                    found |= ProcessDepthLimitedReferences(depthTestsPending[i], 20);
                }
                depthTestsPending.Clear();

                if (!found)
                {
                    // No new depth-clobbering references found, just move on
                    break;
                }
            }

            if (refs.IsEmpty)
            {
                // strip out the refs 'cause it looks better that way :V
                refs.Remove();
            }

            return doc.ToString();
        }
    }

    internal sealed class WriterNodeXml : WriterNode
    {
        private WriterXml writer;
        private XElement node;

        public override Writer Writer { get => writer; }

        private WriterNodeXml(WriterXml writer, XElement parent, string label)
        {
            this.writer = writer;

            node = new XElement(label);
            parent.Add(node);
        }

        public static WriterNodeXml StartDef(WriterXmlCompose writer, XElement defRoot, string type, string defName)
        {
            var node = new WriterNodeXml(writer, defRoot, type);
            node.GetXElement().Add(new XAttribute("defName", defName));
            return node;
        }

        public static WriterNodeXml StartData(WriterXmlRecord writer, XElement defRoot, string name)
        {
            return new WriterNodeXml(writer, defRoot, name);
        }

        public override WriterNode CreateChild(string label)
        {
            return new WriterNodeXml(writer, node, label);
        }

        public override void WritePrimitive(object value)
        {
            if (Compat.FloatRoundtripBroken)
            {
                if (value.GetType() == typeof(double))
                {
                    node.Add(new XText(((double)value).ToString("G17")));

                    return;
                }
                else if (value.GetType() == typeof(float))
                {
                    node.Add(new XText(((float)value).ToString("G9")));

                    return;
                }
            }

            node.Add(new XText(value.ToString()));
        }

        public override void WriteEnum(object value)
        {
            node.Add(new XText(value.ToString()));
        }

        public override void WriteString(string value)
        {
            node.Add(new XText(value));
        }

        public override void WriteType(Type value)
        {
            node.Add(new XText(value.ComposeDefFormatted()));
        }

        public override void WriteDef(Def value)
        {
            // Get the def name and be done with it.
            if (value != null)
            {
                var valueDef = value as Def;

                if (valueDef != Database.Get(valueDef.GetType(), valueDef.DefName))
                {
                    Dbg.Err("Referenced def {valueDef} no longer exists in the database; serializing an error value instead");
                    node.Add(new XText($"{valueDef.DefName}_DELETED"));

                    // if you actually have a def named SomePreviouslyExistingDef_DELETED then you need to sort out what you're doing with your life
                }
                else
                {
                    node.Add(new XText(valueDef.DefName));
                }
            }

            // "No data" is defined as null for defs, so we just do that
        }

        public override void TagClass(Type type)
        {
            node.Add(new XAttribute("class", type.ComposeDefFormatted()));
        }

        public override void WriteExplicitNull()
        {
            node.SetAttributeValue("null", "true");
        }

        public override bool WriteReference(object value)
        {
            return writer.RegisterReference(value, node);
        }

        public override void WriteRecord(IRecordable value)
        {
            writer.RegisterPendingWrite(() => value.Record(new RecorderWriter(this)));
        }

        public override void WriteConvertable(Converter converter, object value, Type fieldType)
        {
            writer.RegisterPendingWrite(() => converter.Record(value, fieldType, new RecorderWriter(this)));
        }

        public override XElement GetXElement()
        {
            return node;
        }
    }
}
