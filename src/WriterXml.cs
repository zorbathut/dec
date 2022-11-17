namespace Dec
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal abstract class WriterXml : Writer
    {
        // A list of writes that still have to happen. This is used so we don't have to do deep recursive dives and potentially blow our stack.
        // I think this is only used for WriterXmlRecord, but right now this all goes through WriterNodeXml which is meant to work with both of these.
        // The inheritance tree is kind of messed up right now and should be fixed.
        private WriterUtil.PendingWriteCoordinator pendingWriteCoordinator = new WriterUtil.PendingWriteCoordinator();

        public abstract bool RegisterReference(object referenced, XElement element, Recorder.Context recContext);

        public void RegisterPendingWrite(Action action)
        {
            pendingWriteCoordinator.RegisterPendingWrite(action);
        }

        public void DequeuePendingWrites()
        {
            pendingWriteCoordinator.DequeuePendingWrites();
        }
    }

    internal class WriterXmlCompose : WriterXml
    {
        public override bool AllowReflection { get => true; }

        private XDocument doc;
        private XElement decs;

        public WriterXmlCompose()
        {
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
            return WriterNodeXml.StartDec(this, decs, type.ComposeDecFormatted(), decName);
        }

        public string Finish(bool pretty)
        {
            DequeuePendingWrites();

            return doc.ToString(pretty ? SaveOptions.None : SaveOptions.DisableFormatting);
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

        public override bool RegisterReference(object referenced, XElement element, Recorder.Context recContext)
        {
            if (!refToElement.TryGetValue(referenced, out var xelement))
            {
                if (recContext.Referenceable)
                {
                    // Insert it into our refToElement mapping
                    refToElement[referenced] = element;
                    elementToRef[element] = referenced;
                }
                else
                {
                    // Cannot be referenced, so we insert a fake null entry
                    refToElement[referenced] = null;

                    // Note: It is important not to add an elementToRef entry because this is later used to split long hierarchies
                    // and if you split a long hierarchy around a non-referencable barrier, everything breaks!
                }

                // We still need this to be generated, so we'll just let that happen now
                return false;
            }

            if (xelement == null)
            {
                // This is an unreferencable object! We are in trouble.
                Dbg.Err("Attempt to create a new reference to an unreferenceable object. Will be left with default values.");
                return true;
            }

            // We have a referencable target, but do *we* allow a reference?
            if (!recContext.Referenceable)
            {
                Dbg.Err("Attempt to create a new unreferenceable recording of a referenceable object. Will be left with default values.");
                return true;
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
                result.SetAttributeValue("class", refblock.Key.GetType().ComposeDecFormatted());

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

        public string Finish(bool pretty)
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

            return doc.ToString(pretty ? SaveOptions.None : SaveOptions.DisableFormatting);
        }
    }

    internal sealed class WriterNodeXml : WriterNode
    {
        private WriterXml writer;
        private XElement node;

        // Represents only the *active* depth in the program stack.
        // This is kind of painfully hacky, because when it's created, we don't know if it's going to represent a new stack start.
        // So we just kinda adjust it as we go.
        private int depth;
        private const int MaxRecursionDepth = 100;

        public override bool AllowReflection { get => writer.AllowReflection; }

        private WriterNodeXml(WriterXml writer, XElement parent, string label, int depth, Recorder.Context context) : base(context)
        {
            this.writer = writer;
            this.depth = depth;

            node = new XElement(label);
            parent.Add(node);
        }

        public static WriterNodeXml StartDec(WriterXmlCompose writer, XElement decRoot, string type, string decName)
        {
            var node = new WriterNodeXml(writer, decRoot, type, 0, new Recorder.Context());
            node.GetXElement().Add(new XAttribute("decName", decName));
            return node;
        }

        public static WriterNodeXml StartData(WriterXmlRecord writer, XElement decRoot, string name)
        {
            return new WriterNodeXml(writer, decRoot, name, 0, new Recorder.Context());
        }

        public override WriterNode CreateChild(string label, Recorder.Context context)
        {
            return new WriterNodeXml(writer, node, label, depth + 1, context);
        }

        public override WriterNode CreateMember(System.Reflection.FieldInfo field, Recorder.Context context)
        {
            return new WriterNodeXml(writer, node, field.Name, depth + 1, context);
        }

        public override void WritePrimitive(object value)
        {
            if (value.GetType() == typeof(double))
            {
                if (Compat.FloatRoundtripBroken)
                {
                    node.Add(new XText(((double)value).ToString("G17", System.Globalization.CultureInfo.InvariantCulture)));
                }
                else
                {
                    node.Add(new XText(((double)value).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
            else if (value.GetType() == typeof(float))
            {
                if (Compat.FloatRoundtripBroken)
                {
                    node.Add(new XText(((float)value).ToString("G9", System.Globalization.CultureInfo.InvariantCulture)));
                }
                else
                {
                    node.Add(new XText(((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
            else
            {
                node.Add(new XText(value.ToString()));
            }
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
            node.Add(new XText(value.ComposeDecFormatted()));
        }

        public override void WriteDec(Dec value)
        {
            // Get the dec name and be done with it.
            if (value == null)
            {
                // "No data" is defined as null for decs, so we just do that
            }
            else if (value.DecName == "" || value.DecName == null)
            {
                Dbg.Err($"Attempted to write a Dec that was dynamically created but never registered; this will be left as a null reference. In most cases you shouldn't be dynamically creating Decs anyway, this is likely a malfunctioning deep copy such as a misbehaving ICloneable");
            }
            else if (value != Database.Get(value.GetType(), value.DecName))
            {
                Dbg.Err($"Referenced dec `{value}` does not exist in the database; serializing an error value instead");
                node.Add(new XText($"{value.DecName}_DELETED"));

                // if you actually have a dec named SomePreviouslyExistingDec_DELETED then you need to sort out what you're doing with your life
            }
            else
            {
                node.Add(new XText(value.DecName));
            }
        }

        public override void TagClass(Type type)
        {
            node.Add(new XAttribute("class", type.ComposeDecFormatted()));
        }

        public override void WriteExplicitNull()
        {
            node.SetAttributeValue("null", "true");
        }

        public override bool WriteReference(object value)
        {
            return writer.RegisterReference(value, node, context);
        }

        public override void WriteArray(Array value)
        {
            Type referencedType = value.GetType().GetElementType();

            for (int i = 0; i < value.Length; ++i)
            {
                Serialization.ComposeElement(CreateChild("li", context), value.GetValue(i), referencedType, context);
            }
        }

        public override void WriteList(IList value)
        {
            Type referencedType = value.GetType().GetGenericArguments()[0];

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(CreateChild("li", context), value[i], referencedType, context);
            }
        }

        public override void WriteDictionary(IDictionary value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];
            Type valueType = value.GetType().GetGenericArguments()[1];

            // I really want some way to canonicalize this ordering
            IDictionaryEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                // In theory, some dicts support inline format, not li format. Inline format is cleaner and smaller and we should be using it when possible.
                // In practice, it's hard and I'm lazy and this always works, and we're not providing any guarantees about cleanliness of serialized output.
                // Revisit this later when someone (possibly myself) really wants it improved.
                var li = CreateChild("li", context);

                Serialization.ComposeElement(li.CreateChild("key", context), iterator.Key, keyType, context);
                Serialization.ComposeElement(li.CreateChild("value", context), iterator.Value, valueType, context);
            }
        }

        public override void WriteHashSet(IEnumerable value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];

            // I really want some way to canonicalize this ordering
            IEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                // In theory, some sets support inline format, not li format. Inline format is cleaner and smaller and we should be using it when possible.
                // In practice, it's hard and I'm lazy and this always works, and we're not providing any guarantees about cleanliness of serialized output.
                // Revisit this later when someone (possibly myself) really wants it improved.
                Serialization.ComposeElement(CreateChild("li", context), iterator.Current, keyType, context);
            }
        }

        public override void WriteTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateChild(nameArray != null ? nameArray[i] : "li", context), value.GetType().GetProperty(Util.DefaultTupleNames[i]).GetValue(value), args[i], context);
            }
        }

        public override void WriteValueTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateChild(nameArray != null ? nameArray[i] : "li", context), value.GetType().GetField(Util.DefaultTupleNames[i]).GetValue(value), args[i], context);
            }
        }

        public override void WriteRecord(IRecordable value)
        {
            if (depth < MaxRecursionDepth)
            {
                // This is somewhat faster than a full pending write (5-10% faster in one test case, though with a lot of noise), so we do it whenever we can.
                value.Record(new RecorderWriter(this));
            }
            else
            {
                // Reset depth because this will be run only when the pending writes are ready.
                depth = 0;
                writer.RegisterPendingWrite(() => value.Record(new RecorderWriter(this)));
            }
        }

        public override void WriteConvertible(Converter converter, object value)
        {
            if (depth < MaxRecursionDepth)
            {
                // This is somewhat faster than a full pending write (5-10% faster in one test case, though with a lot of noise), so we do it whenever we can.
                converter.Record(value, value.GetType(), new RecorderWriter(this));
            }
            else
            {
                // Reset depth because this will be run only when the pending writes are ready.
                depth = 0;
                writer.RegisterPendingWrite(() => converter.Record(value, value.GetType(), new RecorderWriter(this)));
            }
        }

        internal XElement GetXElement()
        {
            return node;
        }
    }
}
